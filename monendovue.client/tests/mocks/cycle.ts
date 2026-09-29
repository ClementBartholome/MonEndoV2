import type { CycleDuMois } from '../../src/features/cycle/types/cycle';
import type { SymptomeCycle } from '../../src/features/cycle/types/symptome-cycle';
import { CARNET_ID } from './session';
import { liste, type FauxServeur } from './faux-serveur';

/**
 * Routes simulées de `CycleController`, avec les jours de règles comme état du test. Le cycle en cours et l'historique
 * sont fournis tels quels (leur calcul est testé côté serveur, `HistoriqueCyclesTests`) ; seuls les jours changent.
 */
export function simulerRegles(serveur: FauxServeur, initiaux: string[] = [], vue: Partial<Omit<CycleDuMois, 'joursDeRegles'>> = {}) {
  const jours = new Set(initiaux);
  serveur
    .on('GET', /^Cycle$/, ({ url }) => {
      const mois = (url.searchParams.get('mois') ?? '').slice(0, 7);
      return {
        body: {
          enCours: null,
          dureeMoyenne: null,
          ...vue,
          cycles: liste(vue.cycles ?? []),
          joursDeRegles: liste([...jours].filter((j) => j.startsWith(mois)).sort((a, b) => a.localeCompare(b))),
        },
      };
    })
    .on('PUT', /^Cycle\/regles\/([\d-]+)$/, ({ params: [jour] }) => {
      jours.add(jour);
      return { status: 204 };
    })
    .on('DELETE', /^Cycle\/regles\/([\d-]+)$/, ({ params: [jour] }) => {
      jours.delete(jour);
      return { status: 204 };
    });
  return jours;
}

/** Champs d'un formulaire multipart reçu (les valeurs texte suffisent aux vérifications). */
export function champs(corps: string): Record<string, string> {
  const resultat: Record<string, string> = {};
  for (const [, nom, valeur] of corps.matchAll(/name="([^"]+)"\r\n\r\n([^\r]*)\r\n/g)) resultat[nom] = valeur;
  return resultat;
}

/**
 * Routes simulées de `SymptomesCycleController` (tableau JSON par mois ; POST et PUT en multipart), avec un état propre
 * au test. Sert aussi à l'onglet Acné, qui lit les mêmes routes.
 */
export function simulerSymptomes(serveur: FauxServeur, initiaux: Partial<SymptomeCycle>[] = []) {
  let prochainId = 1;
  const symptomes: SymptomeCycle[] = initiaux.map((s) => ({
    id: prochainId++, carnetSanteId: CARNET_ID, typeSymptome: 'Fatigue', date: '2026-09-14T09:00:00', intensite: 5, commentaire: null, photoUrl: null, ...s,
  }));
  const depuis = (corps: string, id: number): SymptomeCycle => {
    const recu = champs(corps);
    return {
      id, carnetSanteId: Number(recu.carnetSanteId), typeSymptome: recu.typeSymptome, date: recu.date,
      intensite: Number(recu.intensite), commentaire: recu.commentaire || null, photoUrl: null,
    };
  };

  serveur
    .on('GET', /^SymptomesCycle\/\d+\/(\d+)\/(\d+)$/, ({ params: [mois, annee] }) => ({
      body: symptomes.filter((s) => Number(s.date.slice(5, 7)) === Number(mois) && Number(s.date.slice(0, 4)) === Number(annee)),
    }))
    .on('POST', /^SymptomesCycle$/, ({ corps }) => {
      const symptome = depuis(corps, prochainId++);
      symptomes.push(symptome);
      return { status: 201, body: symptome };
    })
    .on('PUT', /^SymptomesCycle\/(\d+)$/, ({ params: [id], corps }) => {
      const index = symptomes.findIndex((s) => s.id === Number(id));
      if (index < 0) return { status: 404 };
      symptomes[index] = depuis(corps, Number(id));
      return { status: 204 };
    })
    .on('DELETE', /^SymptomesCycle\/(\d+)$/, ({ params: [id] }) => {
      const index = symptomes.findIndex((s) => s.id === Number(id));
      if (index < 0) return { status: 404 };
      symptomes.splice(index, 1);
      return { status: 204 };
    });
  return symptomes;
}
