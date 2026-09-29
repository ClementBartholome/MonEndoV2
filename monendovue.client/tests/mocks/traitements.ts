import type { PriseSaisie, Traitement, TraitementSaisie } from '../../src/features/medicament/types/traitements';
import { liste, type FauxServeur } from './faux-serveur';

interface PriseNotee extends PriseSaisie {
  id: number;
  traitementId: number;
}

/**
 * Routes simulées de `TraitementsController`, avec un état propre au test. Le planning du jour est volontairement
 * simplifié (le vrai calcul est testé côté serveur, `PlanningTraitementTests`) : un traitement médicamenteux en cours
 * a une prise prévue par horaire, sauf « au besoin ».
 */
export function simulerTraitements(serveur: FauxServeur, initiaux: Partial<Traitement>[] = []) {
  let prochainId = 1;
  const traitements: Traitement[] = initiaux.map((t) => ({
    id: prochainId++,
    nom: 'Diénogest 2 mg',
    type: 'Medicamenteux',
    dose: '1 comprimé',
    frequence: 'ChaqueJour',
    joursSemaine: [],
    intervalleJours: null,
    horaires: ['08:00'],
    dateDebut: '2026-01-10',
    dateFin: null,
    ...t,
  }));
  const prises: PriseNotee[] = [];
  const seances: { traitementId: number; date: string }[] = [];

  const enCours = () => traitements.filter((t) => t.dateFin === null);
  const medicaments = () => enCours().filter((t) => t.type === 'Medicamenteux');
  const derniere = (dates: string[]) => [...dates].sort((a, b) => a.localeCompare(b)).at(-1) ?? null;
  const versTraitement = (id: number, saisie: TraitementSaisie): Traitement =>
    ({ ...saisie, id, horaires: saisie.horaires.map((h) => h.slice(0, 5)) });

  serveur
    .on('GET', /^Traitements\/jour$/, () => ({
      body: {
        prisesPrevues: liste(medicaments().filter((t) => t.frequence !== 'AuBesoin').flatMap((t) => t.horaires.map((heure) => {
          const reponse = prises.find((p) => p.traitementId === t.id && p.heurePrevue === `${heure}:00`);
          return {
            traitementId: t.id, nom: t.nom, dose: t.dose, heurePrevue: heure,
            reponse: reponse ? { priseId: reponse.id, statut: reponse.statut, date: reponse.date } : null,
          };
        })).sort((a, b) => a.heurePrevue.localeCompare(b.heurePrevue))),
        auBesoin: liste(medicaments().filter((t) => t.frequence === 'AuBesoin').map((t) => ({
          id: t.id, nom: t.nom, dose: t.dose,
          dernierePrise: derniere(prises.filter((p) => p.traitementId === t.id).map((p) => p.date)),
        }))),
        soins: liste(enCours().filter((t) => t.type === 'NonMedicamenteux').map((t) => ({
          id: t.id, nom: t.nom,
          derniereSeance: derniere(seances.filter((s) => s.traitementId === t.id).map((s) => s.date)),
        }))),
        enCours: liste(enCours().map((t) => ({ ...t, joursSemaine: liste(t.joursSemaine), horaires: liste(t.horaires) }))),
        termines: liste(traitements.filter((t) => t.dateFin !== null).map((t) => ({ ...t, joursSemaine: liste(t.joursSemaine), horaires: liste(t.horaires) }))),
      },
    }))
    .on('POST', /^Traitements$/, ({ corps }) => {
      const traitement = versTraitement(prochainId++, corps as TraitementSaisie);
      traitements.push(traitement);
      return { body: { id: traitement.id } };
    })
    .on('PUT', /^Traitements\/(\d+)$/, ({ params: [id], corps }) => {
      const index = traitements.findIndex((t) => t.id === Number(id));
      if (index < 0) return { status: 404 };
      traitements[index] = versTraitement(Number(id), corps as TraitementSaisie);
      return { status: 204 };
    })
    .on('POST', /^Traitements\/(\d+)\/arret$/, ({ params: [id], url }) => {
      const traitement = traitements.find((t) => t.id === Number(id));
      if (!traitement) return { status: 404 };
      traitement.dateFin = url.searchParams.get('jour');
      return { status: 204 };
    })
    .on('POST', /^Traitements\/(\d+)\/prises$/, ({ params: [id], corps }) => {
      const saisie = corps as PriseSaisie;
      const index = prises.findIndex((p) => p.traitementId === Number(id) && saisie.heurePrevue !== null && p.heurePrevue === saisie.heurePrevue);
      if (index >= 0) prises.splice(index, 1);
      const prise = { ...saisie, id: prochainId++, traitementId: Number(id) };
      prises.push(prise);
      return { body: { id: prise.id } };
    })
    .on('DELETE', /^Traitements\/prises\/(\d+)$/, ({ params: [id] }) => {
      const index = prises.findIndex((p) => p.id === Number(id));
      if (index < 0) return { status: 404 };
      prises.splice(index, 1);
      return { status: 204 };
    })
    .on('POST', /^Traitements\/(\d+)\/seances$/, ({ params: [id], corps }) => {
      seances.push({ traitementId: Number(id), date: (corps as { date: string }).date });
      return { body: { id: prochainId++ } };
    });

  // Historique d'un mois (planning simplifié comme plus haut : un traitement quotidien, chaque horaire chaque jour).
  serveur
    .on('GET', /^Traitements\/(\d+)\/historique$/, ({ params: [id], url }) => {
      const traitement = traitements.find((t) => t.id === Number(id));
      if (!traitement) return { status: 404 };
      const mois = (url.searchParams.get('mois') ?? '').slice(0, 7);
      const jour = url.searchParams.get('jour') ?? '';
      const moisPrecedent = (() => {
        const d = new Date(`${mois}-01T12:00:00`);
        d.setMonth(d.getMonth() - 1);
        return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
      })();
      const entrees = traitement.type === 'NonMedicamenteux'
        ? seances.filter((s) => s.traitementId === traitement.id).map((s, i) => ({ id: 1000 + i, nature: 'Seance', date: s.date, heurePrevue: null }))
        : prises.filter((p) => p.traitementId === traitement.id)
          .map((p) => ({ id: p.id, nature: p.statut, date: p.date, heurePrevue: p.heurePrevue?.slice(0, 5) ?? null }));
      const duMois = entrees.filter((e) => e.date.startsWith(mois));
      const planifie = traitement.type === 'Medicamenteux' && traitement.frequence !== 'AuBesoin';
      const jours = [...new Set(duMois.map((e) => e.date.slice(0, 10)))].sort((a, b) => b.localeCompare(a));
      return {
        body: {
          traitement: { ...traitement, joursSemaine: liste(traitement.joursSemaine), horaires: liste(traitement.horaires) },
          prevues: planifie && jour.startsWith(mois) ? Number(jour.slice(8, 10)) * traitement.horaires.length : 0,
          faites: duMois.filter((e) => e.nature !== 'Ignore').length,
          ignorees: duMois.filter((e) => e.nature === 'Ignore').length,
          faitesMoisPrecedent: entrees.filter((e) => e.date.startsWith(moisPrecedent) && e.nature !== 'Ignore').length,
          jours: liste(jours.map((j) => ({
            jour: j,
            entrees: liste(duMois.filter((e) => e.date.startsWith(j)).sort((a, b) => b.date.localeCompare(a.date))),
          }))),
        },
      };
    })
    .on('DELETE', /^Traitements\/seances\/(\d+)$/, ({ params: [id] }) => {
      const index = Number(id) - 1000;
      if (index < 0 || index >= seances.length) return { status: 404 };
      seances.splice(index, 1);
      return { status: 204 };
    });

  return { traitements, prises, seances };
}
