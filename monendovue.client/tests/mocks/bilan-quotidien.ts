import type {
  BilanQuotidien,
  BilanQuotidienSaisie,
  CodeEmotion,
} from '../../src/features/bilan-quotidien/types/bilan-quotidien';
import { CARNET_ID } from './session';
import { liste, type FauxServeur } from './faux-serveur';

/** Champs d'un bilan à préciser dans un test : le jour (yyyy-MM-dd) et ce qui compte pour le parcours. */
export type BilanDeTest = Partial<Omit<BilanQuotidien, 'emotions' | 'date'>> & {
  jour: string;
  emotions?: CodeEmotion[];
};

/** Bilan complet au format de l'API, avec des valeurs neutres pour tout ce que le test ne précise pas. */
const versBilan = ({ jour, emotions = ['Calme'], ...champs }: BilanDeTest, id: number): BilanQuotidien => ({
  id,
  carnetSanteId: CARNET_ID,
  date: `${jour}T20:00:00`,
  mood: null,
  emotions: emotions.map((emotion) => ({ emotion })),
  douleurMoyenne: 3,
  fatigue: null,
  stressPro: null,
  stressPerso: null,
  pas: null,
  hydratation: null,
  gluten: false,
  lactose: false,
  grignotage: false,
  commentaire: null,
  selles: null,
  typeBristol: null,
  crampesEstomac: null,
  intensiteCrampes: null,
  ballonnements: null,
  intensiteBallonnements: null,
  ...champs,
});

const jourDe = (bilan: { date: Date | string }) => String(bilan.date).slice(0, 10);

/**
 * Routes simulées du bilan quotidien (`BilanQuotidienController`), avec un état propre au test.
 * Formats reproduits : `HistoriqueBilansViewModel` (listes C# → `{ $values }`, y compris les émotions de chaque bilan),
 * 409 si un bilan existe déjà pour le jour, 201 en création et 204 en modification.
 *
 * Renvoie les bilans du faux serveur, pour vérifier ce que l'interface a enregistré.
 */
export function simulerBilans(serveur: FauxServeur, options: { bilans?: BilanDeTest[]; joursRegles?: string[] } = {}) {
  let prochainId = 1;
  const bilans: BilanQuotidien[] = (options.bilans ?? []).map((b) => versBilan(b, prochainId++));
  const joursRegles = options.joursRegles ?? [];

  const dejaSaisi = (jour: string, saufId?: number) => bilans.some((b) => jourDe(b) === jour && b.id !== saufId);
  const conflit = {
    status: 409,
    body: { message: "Un bilan existe déjà pour ce jour : modifie-le plutôt que d'en créer un second." },
  };

  serveur
    .on('GET', /^BilanQuotidien\/periode$/, ({ url }) => {
      const du = url.searchParams.get('du') ?? '';
      const au = url.searchParams.get('au') ?? '';
      const dansLaPeriode = (jour: string) => jour >= du && jour <= au;
      return {
        body: {
          $id: '1',
          bilans: liste(bilans
            .filter((b) => dansLaPeriode(jourDe(b)))
            .map((b) => ({ ...b, emotions: liste(b.emotions) }))),
          joursRegles: liste(joursRegles.filter(dansLaPeriode)),
        },
      };
    })
    .on('POST', /^BilanQuotidien$/, ({ corps }) => {
      const saisie = corps as BilanQuotidienSaisie;
      if (dejaSaisi(jourDe(saisie))) return conflit;
      const bilan: BilanQuotidien = { ...saisie, id: prochainId++ };
      bilans.push(bilan);
      return { status: 201, body: bilan };
    })
    .on('PUT', /^BilanQuotidien\/(\d+)$/, ({ params: [id], corps }) => {
      const index = bilans.findIndex((b) => b.id === Number(id));
      if (index < 0) return { status: 404 };
      const saisie = corps as BilanQuotidienSaisie;
      if (dejaSaisi(jourDe(saisie), Number(id))) return conflit;
      bilans[index] = { ...saisie, id: Number(id) };
      return { status: 204 };
    });

  return bilans;
}
