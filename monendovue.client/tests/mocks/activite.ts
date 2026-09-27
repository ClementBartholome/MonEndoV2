import type { DonneesActivitePhysique } from '../../src/features/activite/types/donnees-activite-physique';
import { CARNET_ID } from './session';
import type { FauxServeur } from './faux-serveur';

/**
 * Routes simulées de l'activité physique (`DonneesActivitePhysiqueController`), avec un état propre au test :
 * une séance ajoutée puis relue ou supprimée se comporte comme avec la vraie API.
 */
export function simulerActivite(serveur: FauxServeur, initiales: Partial<DonneesActivitePhysique>[] = []) {
  let prochainId = 1;
  const seances: DonneesActivitePhysique[] = initiales.map((s) => ({
    id: prochainId++,
    carnetSanteId: CARNET_ID,
    typeActivite: 'Marche',
    date: '2026-09-10T08:00:00',
    duree: 30,
    intensite: 3,
    ...s,
  }));

  serveur
    .on('GET', /^DonneesActivitePhysique\/\d+\/(\d+)\/(\d+)$/, ({ params: [mois, annee] }) => ({
      // Le contrôleur renvoie un tableau (ToArrayAsync) : simple tableau JSON, sans $values.
      body: seances.filter((s) => {
        const date = new Date(s.date);
        return date.getMonth() + 1 === Number(mois) && date.getFullYear() === Number(annee);
      }),
    }))
    .on('POST', /^DonneesActivitePhysique$/, ({ corps }) => {
      const seance = { ...corps, id: prochainId++ } as DonneesActivitePhysique;
      seances.push(seance);
      return { status: 201, body: seance };
    })
    .on('PUT', /^DonneesActivitePhysique\/(\d+)$/, ({ params: [id], corps }) => {
      const index = seances.findIndex((s) => s.id === Number(id));
      if (index < 0) return { status: 404 };
      seances[index] = { ...seances[index], ...corps, id: Number(id) };
      return { status: 204 };
    })
    .on('DELETE', /^DonneesActivitePhysique\/(\d+)$/, ({ params: [id] }) => {
      const index = seances.findIndex((s) => s.id === Number(id));
      if (index < 0) return { status: 404 };
      seances.splice(index, 1);
      return { status: 204 };
    });

  return seances;
}
