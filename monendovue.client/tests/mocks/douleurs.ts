import type { DonneesDouleur } from '../../src/features/douleurs/types/donnees-douleur';
import { CARNET_ID } from './session';
import { liste, type FauxServeur } from './faux-serveur';

/**
 * Routes simulées des douleurs (`DonneesDouleursController`, tableau JSON) et des jours de règles du mois
 * (`JourRegleController`, liste sous `$values`), avec un état propre au test.
 */
export function simulerDouleurs(serveur: FauxServeur, initiales: Partial<DonneesDouleur>[] = [], joursDeRegles: string[] = []) {
  let prochainId = 1;
  const douleurs: DonneesDouleur[] = initiales.map((d) => ({
    id: prochainId++,
    carnetSanteId: CARNET_ID,
    typeDouleur: 'Douleur pelvienne',
    intensite: 5,
    date: '2026-09-14T09:00:00',
    commentaire: null,
    ...d,
  }));
  const duMois = (date: string, mois: string, annee: string) =>
    Number(date.slice(5, 7)) === Number(mois) && Number(date.slice(0, 4)) === Number(annee);

  serveur
    .on('GET', /^DonneesDouleurs\/\d+\/(\d+)\/(\d+)$/, ({ params: [mois, annee] }) => ({
      body: douleurs.filter((d) => duMois(d.date, mois, annee)),
    }))
    .on('GET', /^JourRegle\/ByMonth\/\d+\/(\d+)\/(\d+)\/?$/, ({ params: [mois, annee] }) => ({
      body: liste(joursDeRegles.filter((j) => duMois(j, mois, annee)).map((date, index) => ({ id: index + 1, date: `${date}T00:00:00` }))),
    }))
    .on('POST', /^DonneesDouleurs$/, ({ corps }) => {
      const douleur = { ...corps, id: prochainId++ } as DonneesDouleur;
      douleurs.push(douleur);
      return { status: 201, body: douleur };
    })
    .on('PUT', /^DonneesDouleurs\/(\d+)$/, ({ params: [id], corps }) => {
      const index = douleurs.findIndex((d) => d.id === Number(id));
      if (index < 0) return { status: 404 };
      douleurs[index] = { ...douleurs[index], ...corps, id: Number(id) };
      return { status: 204 };
    })
    .on('DELETE', /^DonneesDouleurs\/(\d+)$/, ({ params: [id] }) => {
      const index = douleurs.findIndex((d) => d.id === Number(id));
      if (index < 0) return { status: 404 };
      douleurs.splice(index, 1);
      return { status: 204 };
    });

  return douleurs;
}
