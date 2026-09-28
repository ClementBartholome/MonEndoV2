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
  const derniere = (dates: string[]) => dates.sort().at(-1) ?? null;
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

  return { traitements, prises, seances };
}
