import type { DonneesTransit } from '../../src/features/transit/types/donnees-transit';
import { liste, type FauxServeur } from './faux-serveur';

/** Routes simulées de l'ancien suivi du transit (`DonneesTransitController`, liste sous `$values`), état propre au test. */
export function simulerTransit(serveur: FauxServeur, initiales: Partial<DonneesTransit>[] = []) {
  let prochainId = 1;
  const entrees: DonneesTransit[] = initiales.map((e) => ({
    id: prochainId++, date: '2026-09-10T08:40:00', typeEvenement: 'Ballonnements', intensite: 'Légère', saignement: false, douleur: false, commentaires: null, ...e,
  }));

  serveur
    .on('GET', /^DonneesTransit\/\d+\/(\d+)\/(\d+)$/, ({ params: [mois, annee] }) => ({
      body: liste(entrees.filter((e) => Number(e.date.slice(5, 7)) === Number(mois) && Number(e.date.slice(0, 4)) === Number(annee))),
    }))
    .on('DELETE', /^DonneesTransit\/(\d+)$/, ({ params: [id] }) => {
      const index = entrees.findIndex((e) => e.id === Number(id));
      if (index < 0) return { status: 404 };
      entrees.splice(index, 1);
      return { status: 204 };
    });

  return entrees;
}
