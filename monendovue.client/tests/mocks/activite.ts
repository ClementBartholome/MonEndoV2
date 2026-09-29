import type { Activite, ActiviteSaisie } from '../../src/features/activite/types/activite';
import { liste, type FauxServeur } from './faux-serveur';

/** Routes simulées de `ActiviteController` (liste sous `$values`), avec un état propre au test. */
export function simulerActivites(serveur: FauxServeur, initiales: Partial<Activite>[] = []) {
  let prochainId = 1;
  const activites: Activite[] = initiales.map((a) => ({
    id: prochainId++, type: 'Marche', date: '2026-09-14T18:00:00', duree: 30, niveau: 'Moderee', effet: 'NonRenseigne', commentaire: null, ...a,
  }));

  serveur
    .on('GET', /^Activite$/, ({ url }) => {
      const mois = (url.searchParams.get('mois') ?? '').slice(0, 7);
      return { body: liste(activites.filter((a) => a.date.startsWith(mois)).sort((a, b) => b.date.localeCompare(a.date))) };
    })
    .on('POST', /^Activite$/, ({ corps }) => {
      const activite = { ...(corps as ActiviteSaisie), id: prochainId++ };
      activites.push(activite);
      return { body: { id: activite.id } };
    })
    .on('PUT', /^Activite\/(\d+)$/, ({ params: [id], corps }) => {
      const index = activites.findIndex((a) => a.id === Number(id));
      if (index < 0) return { status: 404 };
      activites[index] = { ...(corps as ActiviteSaisie), id: Number(id) };
      return { status: 204 };
    })
    .on('DELETE', /^Activite\/(\d+)$/, ({ params: [id] }) => {
      const index = activites.findIndex((a) => a.id === Number(id));
      if (index < 0) return { status: 404 };
      activites.splice(index, 1);
      return { status: 204 };
    });

  return activites;
}
