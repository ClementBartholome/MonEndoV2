import type { jsPDF } from 'jspdf';
import type { CellHookData, RowInput, UserOptions } from 'jspdf-autotable';
import { addDays, eachMonthOfInterval, format, getDaysInMonth, parseISO } from 'date-fns';
import { fr } from 'date-fns/locale';
import { presentationEmotion } from '@/features/bilan-quotidien/config/emotions';
import type { CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';
import { libelleCourt } from '@/features/douleurs/utils/douleurs';
import { resumeFrequence } from '@/features/medicament/utils/prises';
import type { BilanDuJour, OptionsPdf, SyntheseRendezVous } from '../types/synthese';

/**
 * PDF « Préparer un rendez-vous » : une première page de synthèse (questions, règles, douleurs par type, traitements,
 * bilans) lisible en deux minutes, puis un tableau jour par jour par mois et les notes des bilans. Uniquement ce qui a été
 * noté : comptes et moyennes, aucune interprétation.
 */

type Rvb = [number, number, number];
type AutoTable = (doc: jsPDF, options: UserOptions) => void;

const TEXTE: Rvb = [51, 39, 42];
const TEXTE_2: Rvb = [89, 74, 78];
const TRAIT: Rvb = [239, 220, 211];
const SURFACE_2: Rvb = [243, 227, 219];
const REGLES: Rvb = [163, 50, 79];
/** Échelle d'intensité de l'application (`--intensite-0` à `--intensite-10`). */
const INTENSITES: Rvb[] = [
    [249, 211, 220], [245, 194, 206], [240, 176, 193], [234, 157, 178], [227, 138, 163], [219, 119, 148],
    [176, 69, 106], [152, 58, 90], [128, 48, 76], [105, 38, 62], [82, 29, 48],
];

const MARGE = 15;
const NIVEAUX_ACTIVITE = ['', 'D', 'M', 'S'];

/**
 * Les polices intégrées au PDF ne connaissent que le latin de base : les caractères typographiques sont remplacés par
 * leur équivalent simple, et le reste (emojis…) est retiré plutôt que d'apparaître brouillé.
 */
export function textePdf(texte: string): string {
    return texte
        .replace(/[‘’]/g, "'")
        .replace(/[“”]/g, '"')
        .replace(/[–—]/g, '-')
        .replace(/…/g, '...')
        .replace(/œ/g, 'oe')
        .replace(/Œ/g, 'OE')
        .replace(/[\u202f\u00a0]/g, ' ')
        .replace(/[^\n\u0020-\u00ff]/g, '')
        .trim();
}

const jourLong = (cle: string) => format(parseISO(cle), 'd MMMM yyyy', { locale: fr });
const jourCourt = (cle: string) => format(parseISO(cle), 'd MMM', { locale: fr });
const nombre = (valeur: number) => valeur.toLocaleString('fr-FR', { maximumFractionDigits: 1 });
const pluriel = (n: number, mot: string) => `${n} ${mot}${n > 1 ? 's' : ''}`;

/** « Du 15 juin au 15 septembre 2026 » */
export function libellePeriode(du: string, au: string): string {
    const debut = du.slice(0, 4) === au.slice(0, 4) ? format(parseISO(du), 'd MMMM', { locale: fr }) : jourLong(du);
    return `Du ${debut} au ${jourLong(au)}`;
}

export function nomDuFichier(synthese: Pick<SyntheseRendezVous, 'du' | 'au'>): string {
    return `monendo-synthese-${synthese.du}-${synthese.au}.pdf`;
}

/** Charge jsPDF à la demande (il pèse lourd et ne sert qu'ici), construit le document et le renvoie. */
export async function creerPdf(synthese: SyntheseRendezVous, options: OptionsPdf): Promise<jsPDF> {
    const [{ jsPDF: Pdf }, { default: autoTable }] = await Promise.all([import('jspdf'), import('jspdf-autotable')]);
    const doc = new Pdf({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    new Redaction(doc, autoTable as AutoTable, synthese, options).rediger();
    return doc;
}

class Redaction {
    private y = MARGE;

    constructor(
        private readonly doc: jsPDF,
        private readonly autoTable: AutoTable,
        private readonly synthese: SyntheseRendezVous,
        private readonly options: OptionsPdf,
    ) {}

    rediger(): void {
        const { rubriques } = this.options;
        this.entete();
        this.questions();
        if (rubriques.cycle) this.regles();
        if (rubriques.douleurs) this.douleurs();
        if (rubriques.cycle) this.symptomes();
        if (rubriques.traitements) this.traitements();
        if (rubriques.bilans) this.bilans();
        if (rubriques.activite) this.activite();
        this.joursParMois();
        if (rubriques.bilans) this.notes();
        this.pieds();
    }

    // --- Première page : synthèse ---

    private entete(): void {
        const { doc, synthese } = this;
        doc.setFont('helvetica', 'bold').setFontSize(18).setTextColor(...TEXTE);
        doc.text('Synthèse de suivi', MARGE, this.y + 5);
        doc.setFont('helvetica', 'normal').setFontSize(12);
        doc.text(libellePeriode(synthese.du, synthese.au), MARGE, this.y + 12);
        doc.setFontSize(9).setTextColor(...TEXTE_2);
        const mention: string[] = doc.splitTextToSize(
            `Notes personnelles saisies dans l'application MonEndo, sans interprétation. Document créé le ${format(this.options.creeLe, 'd MMMM yyyy', { locale: fr })}.`,
            this.largeur() - 2 * MARGE);
        doc.text(mention, MARGE, this.y + 18);
        const bas = this.y + 18 + (mention.length - 1) * 4 + 3;
        doc.setDrawColor(...TRAIT).setLineWidth(0.4).line(MARGE, bas, this.largeur() - MARGE, bas);
        this.y = bas + 7;
    }

    private questions(): void {
        const texte = textePdf(this.options.questions);
        if (!texte) return;
        this.titre('Mes questions');
        const lignes: string[] = this.doc.setFont('helvetica', 'normal').setFontSize(10.5).splitTextToSize(texte, this.largeur() - 2 * MARGE - 8);
        const hauteur = lignes.length * 5 + 6;
        this.placePour(hauteur);
        this.doc.setFillColor(...SURFACE_2).roundedRect(MARGE, this.y, this.largeur() - 2 * MARGE, hauteur, 2, 2, 'F');
        this.doc.setTextColor(...TEXTE).text(lignes, MARGE + 4, this.y + 6.5);
        this.y += hauteur + 6;
    }

    private regles(): void {
        const { regles } = this.synthese;
        this.titre('Règles');
        if (regles.jours.length === 0) {
            this.paragraphe('Aucun jour de règles noté sur la période.');
            return;
        }
        const debuts = regles.debuts.length ? ` Règles commencées le ${regles.debuts.map(jourCourt).join(', ')}.` : '';
        this.paragraphe(`${pluriel(regles.jours.length, 'jour')} de règles notés.${debuts}`);
        if (regles.cycleMoyen !== null) {
            const dureeRegles = regles.reglesMoyenne === null ? '' : `, règles de ${regles.reglesMoyenne} jours en moyenne`;
            this.paragraphe(`Cycle de ${regles.cycleMoyen} jours en moyenne${dureeRegles} (cycles commencés sur la période).`);
        }
    }

    private douleurs(): void {
        const { douleurs } = this.synthese;
        this.titre('Douleurs');
        if (douleurs.jours === 0) {
            this.paragraphe('Aucune douleur notée sur la période.');
            return;
        }
        const pendantRegles = this.options.rubriques.cycle && douleurs.joursDouleurForte > 0
            ? `, dont ${douleurs.joursDouleurFortePendantRegles} pendant les règles`
            : '';
        this.paragraphe(
            `${pluriel(douleurs.jours, 'jour')} avec au moins une douleur notée. ` +
            `Douleur à 6/10 ou plus : ${pluriel(douleurs.joursDouleurForte, 'jour')}${pendantRegles}.`);
        this.tableau({
            head: [['Type de douleur', 'Jours', 'Intensité moyenne', 'Intensité la plus forte', 'Jours pendant les règles']],
            body: douleurs.parType.map((t) => [
                textePdf(libelleCourt(t.type)), t.jours, `${nombre(t.intensiteMoyenne)}/10`, `${t.intensiteMax}/10`, t.joursPendantRegles,
            ]),
        });
    }

    private symptomes(): void {
        const { symptomes } = this.synthese;
        if (symptomes.parType.length === 0) return;
        this.titre('Symptômes');
        this.tableau({
            head: [['Symptôme', 'Jours', 'Intensité moyenne', 'Jours pendant les règles']],
            body: symptomes.parType.map((t) => [textePdf(t.type), t.jours, `${nombre(t.intensiteMoyenne)}/10`, t.joursPendantRegles]),
        });
    }

    private traitements(): void {
        const { traitements } = this.synthese;
        this.titre('Traitements');
        if (traitements.length === 0) {
            this.paragraphe('Aucun traitement suivi sur la période.');
            return;
        }
        this.tableau({
            head: [['Traitement', 'Fréquence', 'Dates', 'Sur la période']],
            body: traitements.map(({ traitement, enCours, prevues, faites, ignorees }) => {
                const soin = traitement.type === 'NonMedicamenteux';
                const dates = `Depuis le ${jourLong(traitement.dateDebut)}` +
                    (traitement.dateFin ? `\njusqu'au ${jourLong(traitement.dateFin)}` : enCours ? '' : '\n(arrêté)');
                let suivi: string;
                if (soin) suivi = pluriel(faites, 'séance');
                else if (prevues > 0) suivi = `${pluriel(faites, 'prise')} notée${faites > 1 ? 's' : ''} sur ${pluriel(prevues, 'prévue')}` + (ignorees ? `\n${pluriel(ignorees, 'prise')} ignorée${ignorees > 1 ? 's' : ''}` : '');
                else suivi = pluriel(faites, 'prise');
                return [
                    textePdf(traitement.nom + (traitement.dose ? `\n${traitement.dose}` : '')),
                    soin ? 'Soin' : textePdf(resumeFrequence(traitement)),
                    dates,
                    suivi,
                ];
            }),
        });
        this.note('Les prises prévues sont calculées d\'après la fréquence et les horaires actuels de chaque traitement.');
    }

    private bilans(): void {
        const { bilans } = this.synthese;
        this.titre('Bilans quotidiens');
        if (bilans.nombre === 0) {
            this.paragraphe('Aucun bilan quotidien rempli sur la période.');
            return;
        }
        const moyennes = [
            bilans.douleurMoyenne === null ? null : `douleur ${nombre(bilans.douleurMoyenne)}/10`,
            bilans.fatigueMoyenne === null ? null : `fatigue ${nombre(bilans.fatigueMoyenne)}/5`,
            bilans.stressMoyen === null ? null : `stress ${nombre(bilans.stressMoyen)}/5`,
        ].filter(Boolean).join(', ');
        this.paragraphe(`${pluriel(bilans.nombre, 'bilan')} rempli${bilans.nombre > 1 ? 's' : ''}. Moyennes des jours renseignés : ${moyennes}.`);
        if (bilans.emotions.length) {
            const emotions = bilans.emotions
                .map((e) => `${presentationEmotion[e.emotion as CodeEmotion]?.libelle ?? e.emotion} (${pluriel(e.jours, 'jour')})`)
                .join(', ');
            this.paragraphe(`Émotions les plus notées : ${emotions}.`);
        }
        if (bilans.joursBallonnements || bilans.joursCrampes) {
            this.paragraphe(`Transit : ballonnements ${pluriel(bilans.joursBallonnements, 'jour')}, crampes d'estomac ${pluriel(bilans.joursCrampes, 'jour')}.`);
        }
    }

    private activite(): void {
        const { activite } = this.synthese;
        this.titre('Activité physique');
        if (activite.seances === 0) {
            this.paragraphe('Aucune séance notée sur la période.');
            return;
        }
        const types = activite.parType.map((t) => `${textePdf(t.type)} (${t.seances})`).join(', ');
        this.paragraphe(`${pluriel(activite.seances, 'séance')}, ${activite.minutes} minutes au total : ${types}.`);
        if (activite.soulagee + activite.pareille + activite.plusForte > 0) {
            this.paragraphe(
                `Effet noté sur la douleur : soulagée après ${pluriel(activite.soulagee, 'séance')}, ` +
                `pareille après ${activite.pareille}, plus forte après ${activite.plusForte}.`);
        }
    }

    // --- Jour par jour : une page en paysage par mois ---

    private joursParMois(): void {
        const { du, au } = this.synthese;
        eachMonthOfInterval({ start: parseISO(du), end: parseISO(au) }).forEach((mois) => this.pageDuMois(mois));
    }

    private pageDuMois(mois: Date): void {
        const { doc, synthese } = this;
        const { rubriques } = this.options;
        const nombreDeJours = getDaysInMonth(mois);
        const cles = Array.from({ length: nombreDeJours }, (_, i) => format(addDays(mois, i), 'yyyy-MM-dd'));
        const cases = (valeur: (cle: string) => string | number) =>
            cles.map((cle) => (cle < synthese.du || cle > synthese.au ? '' : valeur(cle)));
        const dansLeMois = (cle: string) => cle >= cles[0] && cle <= cles[nombreDeJours - 1];
        const lignes: RowInput[] = [];
        /** Index des lignes dont les cases sont des intensités de 0 à 10, colorées sur l'échelle de l'application. */
        const lignesIntensite = new Set<number>();
        let ligneRegles = -1;
        const section = (libelle: string) => lignes.push([{
            content: libelle, colSpan: nombreDeJours + 1,
            styles: { halign: 'left', fontStyle: 'bold', fillColor: SURFACE_2, textColor: TEXTE },
        }]);
        const ligne = (libelle: string, valeurs: (string | number)[], intensite = false) => {
            if (intensite) lignesIntensite.add(lignes.length);
            lignes.push([textePdf(libelle), ...valeurs]);
        };
        /** Une ligne par type présent dans le mois : intensité la plus forte de chaque jour. */
        const parType = (entrees: { jour: string; type: string; intensite: number }[], libelle: (type: string) => string) => {
            const duMois = entrees.filter((e) => dansLeMois(e.jour));
            [...new Set(duMois.map((e) => e.type))].forEach((type) => {
                const plusForte = new Map<string, number>();
                duMois.filter((e) => e.type === type).forEach((e) => plusForte.set(e.jour, Math.max(plusForte.get(e.jour) ?? 0, e.intensite)));
                ligne(libelle(type), cases((cle) => plusForte.get(cle) ?? ''), true);
            });
        };

        if (rubriques.cycle) {
            section('Cycle');
            const regles = new Set(synthese.regles.jours);
            ligneRegles = lignes.length;
            ligne('Règles', cases((cle) => (regles.has(cle) ? 'x' : '')));
            parType(synthese.symptomes.entrees, (type) => type);
        }
        if (rubriques.douleurs && synthese.douleurs.entrees.some((e) => dansLeMois(e.jour))) {
            section('Douleurs (intensité la plus forte du jour)');
            parType(synthese.douleurs.entrees, libelleCourt);
        }
        if (rubriques.bilans) {
            const bilans = new Map(synthese.bilans.jours.map((b) => [b.jour, b]));
            const duBilan = (valeur: (bilan: BilanDuJour) => string | number | null) =>
                cases((cle) => { const bilan = bilans.get(cle); return bilan ? valeur(bilan) ?? '' : ''; });
            section('Bilans quotidiens');
            ligne('Douleur du jour', duBilan((b) => b.douleur), true);
            ligne('Fatigue', duBilan((b) => b.fatigue));
            ligne('Stress', duBilan((b) => (b.stress === null ? null : nombre(b.stress))));
            ligne('Selles (Bristol)', duBilan((b) => (b.selles === null ? null : b.selles ? b.typeBristol ?? 'x' : '0')));
            ligne('Ballonnements', duBilan((b) => (b.ballonnements ? 'x' : null)));
            ligne('Crampes', duBilan((b) => (b.crampes ? 'x' : null)));
        }
        if (rubriques.traitements) {
            const suivis = synthese.traitements.filter((t) => t.jours.some(dansLeMois));
            if (suivis.length) section('Traitements (prise ou séance notée)');
            suivis.forEach((t) => {
                const jours = new Set(t.jours);
                ligne(t.traitement.nom, cases((cle) => (jours.has(cle) ? 'x' : '')));
            });
        }
        if (rubriques.activite && synthese.activite.entrees.some((e) => dansLeMois(e.jour))) {
            const niveaux = new Map<string, number>();
            synthese.activite.entrees.forEach((e) => niveaux.set(e.jour, Math.max(niveaux.get(e.jour) ?? 0, e.niveau)));
            section('Activité physique');
            ligne('Séance', cases((cle) => NIVEAUX_ACTIVITE[niveaux.get(cle) ?? 0] ?? ''));
        }
        if (rubriques.transit && synthese.transit.some((e) => dansLeMois(e.jour))) {
            section('Ancien suivi du transit');
            [...new Set(synthese.transit.filter((e) => dansLeMois(e.jour)).map((e) => e.type))].forEach((type) => {
                const jours = new Set(synthese.transit.filter((e) => e.type === type).map((e) => e.jour));
                ligne(type, cases((cle) => (jours.has(cle) ? 'x' : '')));
            });
        }

        // Un mois sans aucune donnée notée n'a pas de page (pas de tableau vide).
        const aDesDonnees = lignes.some((ligne) => Array.isArray(ligne) && ligne.length > 1 && ligne.slice(1).some((cellule) => cellule !== ''));
        if (!aDesDonnees) return;

        doc.addPage('a4', 'landscape');
        this.y = MARGE;
        doc.setFont('helvetica', 'bold').setFontSize(14).setTextColor(...TEXTE);
        const titre = format(mois, 'MMMM yyyy', { locale: fr });
        doc.text(`Jour par jour · ${titre.charAt(0).toUpperCase()}${titre.slice(1)}`, MARGE, this.y + 4);
        this.y += 9;

        const largeurLibelle = 42;
        const largeurJour = (this.largeur() - 2 * MARGE - largeurLibelle) / nombreDeJours;
        this.autoTable(doc, {
            head: [['', ...cles.map((_, i) => String(i + 1))]],
            body: lignes,
            startY: this.y,
            margin: { left: MARGE, right: MARGE },
            theme: 'grid',
            styles: { font: 'helvetica', fontSize: 7.5, cellPadding: 1, halign: 'center', valign: 'middle', textColor: TEXTE, lineColor: TRAIT, lineWidth: 0.15, overflow: 'ellipsize' },
            headStyles: { fillColor: TEXTE, textColor: [255, 255, 255], fontStyle: 'bold' },
            columnStyles: { 0: { cellWidth: largeurLibelle, halign: 'left' }, ...Object.fromEntries(cles.map((_, i) => [i + 1, { cellWidth: largeurJour }])) },
            didParseCell: (cellule: CellHookData) => {
                if (cellule.section !== 'body' || cellule.column.index === 0 || cellule.cell.raw === '') return;
                if (cellule.row.index === ligneRegles) {
                    cellule.cell.styles.fillColor = REGLES;
                    cellule.cell.styles.textColor = [255, 255, 255];
                } else if (lignesIntensite.has(cellule.row.index)) {
                    const intensite = Math.min(Math.max(Number(cellule.cell.raw), 0), 10);
                    cellule.cell.styles.fillColor = INTENSITES[intensite];
                    if (intensite >= 6) cellule.cell.styles.textColor = [255, 255, 255];
                }
            },
        });
        this.y = this.finDuTableau() + 5;
        doc.setFont('helvetica', 'normal').setFontSize(8).setTextColor(...TEXTE_2);
        doc.text(this.legende(), MARGE, this.y, { maxWidth: this.largeur() - 2 * MARGE });
    }

    private legende(): string {
        const { rubriques } = this.options;
        return [
            'Case vide : rien de noté ce jour-là.',
            rubriques.douleurs || rubriques.cycle || rubriques.bilans ? 'Douleurs et symptômes : de 0 à 10, plus foncé = plus fort.' : '',
            rubriques.bilans ? 'Fatigue et stress : de 0 à 5. Selles : type de Bristol (1 à 7), x = sans précision, 0 = aucune.' : '',
            rubriques.activite ? 'Activité : D douce, M modérée, S soutenue.' : '',
        ].filter(Boolean).join(' ');
    }

    // --- Notes des bilans ---

    private notes(): void {
        const notes = this.synthese.bilans.jours.filter((b) => b.notes && textePdf(b.notes));
        if (notes.length === 0) return;
        this.doc.addPage('a4', 'portrait');
        this.y = MARGE;
        this.titre('Notes des bilans');
        notes.forEach((bilan) => {
            const lignes: string[] = this.doc.setFont('helvetica', 'normal').setFontSize(10).splitTextToSize(textePdf(bilan.notes!), this.largeur() - 2 * MARGE);
            this.placePour(6 + lignes.length * 4.6);
            const jour = format(parseISO(bilan.jour), 'EEEE d MMMM', { locale: fr });
            this.doc.setFont('helvetica', 'bold').setFontSize(10).setTextColor(...TEXTE).text(jour.charAt(0).toUpperCase() + jour.slice(1), MARGE, this.y);
            this.doc.setFont('helvetica', 'normal').setTextColor(...TEXTE_2).text(lignes, MARGE, this.y + 5);
            this.y += 8 + lignes.length * 4.6;
        });
    }

    // --- Outils de mise en page ---

    private largeur(): number {
        return this.doc.internal.pageSize.getWidth();
    }

    private hauteur(): number {
        return this.doc.internal.pageSize.getHeight();
    }

    /** Passe à la page suivante s'il ne reste pas la hauteur demandée. */
    private placePour(hauteur: number): void {
        if (this.y + hauteur <= this.hauteur() - MARGE - 6) return;
        this.doc.addPage('a4', 'portrait');
        this.y = MARGE;
    }

    private titre(libelle: string): void {
        this.placePour(22);
        if (this.y > MARGE + 1) this.y += 3;
        this.doc.setFont('helvetica', 'bold').setFontSize(12.5).setTextColor(...TEXTE).text(libelle, MARGE, this.y + 4);
        this.y += 9;
    }

    private paragraphe(texte: string): void {
        const lignes: string[] = this.doc.setFont('helvetica', 'normal').setFontSize(10.5).splitTextToSize(textePdf(texte), this.largeur() - 2 * MARGE);
        this.placePour(lignes.length * 5);
        this.doc.setTextColor(...TEXTE).text(lignes, MARGE, this.y + 3);
        this.y += lignes.length * 5 + 1.5;
    }

    private note(texte: string): void {
        this.doc.setFont('helvetica', 'italic').setFontSize(8.5).setTextColor(...TEXTE_2).text(texte, MARGE, this.y);
        this.y += 5;
    }

    private tableau(contenu: Pick<UserOptions, 'head' | 'body'>): void {
        this.autoTable(this.doc, {
            ...contenu,
            startY: this.y + 1,
            margin: { left: MARGE, right: MARGE, bottom: MARGE + 6 },
            theme: 'grid',
            styles: { font: 'helvetica', fontSize: 9.5, cellPadding: 1.8, textColor: TEXTE, lineColor: TRAIT, lineWidth: 0.15 },
            headStyles: { fillColor: SURFACE_2, textColor: TEXTE, fontStyle: 'bold' },
        });
        this.y = this.finDuTableau() + 6;
    }

    private finDuTableau(): number {
        return (this.doc as jsPDF & { lastAutoTable: { finalY: number } }).lastAutoTable.finalY;
    }

    private pieds(): void {
        const pages = this.doc.getNumberOfPages();
        const periode = libellePeriode(this.synthese.du, this.synthese.au).toLowerCase();
        for (let page = 1; page <= pages; page++) {
            this.doc.setPage(page);
            this.doc.setFont('helvetica', 'normal').setFontSize(8).setTextColor(...TEXTE_2);
            this.doc.text(`MonEndo · synthèse ${periode}`, MARGE, this.hauteur() - 8);
            this.doc.text(`${page} / ${pages}`, this.largeur() - MARGE, this.hauteur() - 8, { align: 'right' });
        }
    }
}
