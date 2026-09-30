import { addDays, differenceInCalendarDays, format } from 'date-fns';
import type { BilanDuJour, EntreeIntensite, SyntheseRendezVous } from '../../src/features/export/types/synthese';
import { liste, type FauxServeur } from './faux-serveur';

/**
 * Synthèse proche du réel pour la période demandée : règles tous les 29 jours (5 jours), douleurs plus fortes autour,
 * un bilan presque chaque jour, deux traitements, un soin, des séances d'activité. Valeurs déterministes (pas de hasard).
 */
export function syntheseRealiste(du: string, au: string): SyntheseRendezVous {
  const debut = new Date(`${du}T12:00:00`);
  const nombre = differenceInCalendarDays(new Date(`${au}T12:00:00`), debut) + 1;
  const regles: string[] = [];
  const debuts: string[] = [];
  const douleurs: EntreeIntensite[] = [];
  const symptomes: EntreeIntensite[] = [];
  const bilans: BilanDuJour[] = [];
  const prises: string[] = [];
  const seances: string[] = [];
  const activites: { jour: string; type: string; niveau: number }[] = [];

  for (let i = 0; i < nombre; i++) {
    const jour = format(addDays(debut, i), 'yyyy-MM-dd');
    const jourDuCycle = (i + 20) % 29;
    const enRegles = jourDuCycle < 5;
    if (enRegles) regles.push(jour);
    if (jourDuCycle === 0) debuts.push(jour);
    if (enRegles || i % 6 === 0) douleurs.push({ jour, type: 'Douleur pelvienne', intensite: enRegles ? 8 - jourDuCycle : 3 + (i % 3) });
    if (i % 9 === 2) douleurs.push({ jour, type: 'Douleur lombaire', intensite: 4 });
    if (i % 11 === 3) symptomes.push({ jour, type: 'Nausée', intensite: 5 });
    if (i % 13 !== 5) {
      bilans.push({
        jour, douleur: enRegles ? 7 - jourDuCycle : 2 + (i % 3), fatigue: i % 8 === 0 ? null : 1 + (i % 4), stress: i % 5 === 0 ? null : 1.5 + (i % 3),
        selles: i % 4 === 0 ? null : true, typeBristol: i % 4 === 0 ? null : 3 + (i % 3), ballonnements: i % 7 === 1, crampes: i % 10 === 4,
        notes: i % 17 === 6 ? 'Nuit courte, douleur au réveil puis mieux dans l’après-midi 🙂.' : null,
      });
    }
    if (i % 12 !== 7) prises.push(jour);
    if (i % 14 === 3) seances.push(jour);
    if (i % 5 === 1) activites.push({ jour, type: i % 2 ? 'Marche' : 'Yoga', niveau: 1 + (i % 3) });
  }

  const moyenne = (valeurs: number[]) => Math.round((valeurs.reduce((s, v) => s + v, 0) / valeurs.length) * 10) / 10;
  const joursForts = new Set(douleurs.filter((d) => d.intensite >= 6).map((d) => d.jour));
  const parType = (type: string) => {
    const entrees = douleurs.filter((d) => d.type === type);
    const jours = [...new Set(entrees.map((d) => d.jour))];
    return {
      type, jours: jours.length, intensiteMoyenne: moyenne(entrees.map((d) => d.intensite)),
      intensiteMax: Math.max(...entrees.map((d) => d.intensite)), joursPendantRegles: jours.filter((j) => regles.includes(j)).length,
    };
  };

  return {
    du, au,
    regles: { jours: regles, debuts, cycleMoyen: debuts.length >= 3 ? 29 : null, reglesMoyenne: debuts.length >= 3 ? 5 : null },
    douleurs: {
      jours: new Set(douleurs.map((d) => d.jour)).size,
      joursDouleurForte: joursForts.size,
      joursDouleurFortePendantRegles: [...joursForts].filter((j) => regles.includes(j)).length,
      parType: [parType('Douleur pelvienne'), parType('Douleur lombaire')],
      entrees: douleurs,
    },
    symptomes: {
      parType: symptomes.length ? [{ type: 'Nausée', jours: symptomes.length, intensiteMoyenne: 5, joursPendantRegles: symptomes.filter((s) => regles.includes(s.jour)).length }] : [],
      entrees: symptomes,
    },
    traitements: [
      {
        traitement: { id: 1, nom: 'Diénogest 2 mg', type: 'Medicamenteux', dose: '1 comprimé', frequence: 'ChaqueJour', joursSemaine: [], intervalleJours: null, horaires: ['08:00'], dateDebut: '2026-02-01', dateFin: null },
        enCours: true, prevues: nombre, faites: prises.length, ignorees: 3, jours: prises,
      },
      {
        traitement: { id: 2, nom: 'Ibuprofène 400 mg', type: 'Medicamenteux', dose: null, frequence: 'AuBesoin', joursSemaine: [], intervalleJours: null, horaires: [], dateDebut: '2025-11-10', dateFin: null },
        enCours: true, prevues: 0, faites: joursForts.size, ignorees: 0, jours: [...joursForts],
      },
      {
        traitement: { id: 3, nom: 'Kiné', type: 'NonMedicamenteux', dose: null, frequence: 'AuBesoin', joursSemaine: [], intervalleJours: null, horaires: [], dateDebut: '2026-01-05', dateFin: null },
        enCours: true, prevues: 0, faites: seances.length, ignorees: 0, jours: seances,
      },
    ],
    bilans: {
      nombre: bilans.length,
      douleurMoyenne: moyenne(bilans.map((b) => b.douleur)),
      fatigueMoyenne: moyenne(bilans.flatMap((b) => (b.fatigue === null ? [] : [b.fatigue]))),
      stressMoyen: moyenne(bilans.flatMap((b) => (b.stress === null ? [] : [b.stress]))),
      joursBallonnements: bilans.filter((b) => b.ballonnements).length,
      joursCrampes: bilans.filter((b) => b.crampes).length,
      emotions: [{ emotion: 'Calme', jours: 31 }, { emotion: 'Anxiete', jours: 18 }, { emotion: 'Decouragement', jours: 9 }],
      jours: bilans,
    },
    activite: {
      seances: activites.length,
      minutes: activites.length * 35,
      soulagee: Math.ceil(activites.length / 2), pareille: Math.floor(activites.length / 3), plusForte: 1,
      parType: ['Marche', 'Yoga'].map((type) => ({ type, seances: activites.filter((a) => a.type === type).length, minutes: activites.filter((a) => a.type === type).length * 35 })),
      entrees: activites,
    },
    transit: [],
  };
}

/** Comme le serveur : chaque liste de la réponse est enveloppée dans `{ $values }`. */
function avecValeurs(valeur: unknown): unknown {
  if (Array.isArray(valeur)) return liste(valeur.map(avecValeurs));
  if (valeur === null || typeof valeur !== 'object') return valeur;
  return Object.fromEntries(Object.entries(valeur).map(([cle, v]) => [cle, avecValeurs(v)]));
}

/** Route simulée de `SyntheseController` : la synthèse de la période demandée. */
export function simulerSynthese(serveur: FauxServeur, synthese: (du: string, au: string) => SyntheseRendezVous = syntheseRealiste) {
  serveur.on('GET', /^Synthese$/, ({ url }) => ({
    body: avecValeurs(synthese(url.searchParams.get('du') ?? '', url.searchParams.get('au') ?? '')),
  }));
}
