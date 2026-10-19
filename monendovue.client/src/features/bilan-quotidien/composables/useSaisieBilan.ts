import { computed, ref, type Ref } from 'vue';
import { format } from 'date-fns';
import { toast } from '@/shared/components/ui/toast';
import apiService from '@/shared/services/apiService';
import { estTransitComplet } from '@/features/bilan-quotidien/config/transit';
import { suiteManquante, toutesLesQuestions } from '@/features/bilan-quotidien/config/questions';
import { consommationsAlimentaires } from '@/features/bilan-quotidien/config/saisie';
import { emotionsDuBilan } from '@/features/bilan-quotidien/utils/humeur';
import type { BilanQuotidien, BilanQuotidienSaisie, CategoriesBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { ChampManquant, CorpsBilan, FormulaireBilan } from '@/features/bilan-quotidien/types/saisie-bilan';

export interface UseSaisieBilanOptions {
  carnetSanteId: number;
  /** Jour du bilan (création). */
  date: Date;
  /** Bilan à modifier ; absent = création. */
  bilan?: BilanQuotidien;
  /** Bilan de la veille, pour « Comme hier ». */
  bilanVeille: Ref<BilanQuotidien | undefined>;
  onEnregistre: (bilan: BilanQuotidien) => void;
}

export const corpsDe = (source: Partial<CorpsBilan>): CorpsBilan => ({
  pas: source.pas ?? null,
  hydratation: source.hydratation ?? null,
  gluten: source.gluten ?? false,
  lactose: source.lactose ?? false,
  grignotage: source.grignotage ?? false,
  selles: source.selles ?? null,
  typeBristol: source.typeBristol ?? null,
  crampesEstomac: source.crampesEstomac ?? null,
  intensiteCrampes: source.intensiteCrampes ?? null,
  ballonnements: source.ballonnements ?? null,
  intensiteBallonnements: source.intensiteBallonnements ?? null,
  douleurSelle: source.douleurSelle ?? null,
  intensiteDouleurSelle: source.intensiteDouleurSelle ?? null,
  nausees: source.nausees ?? null,
  sangSelles: source.sangSelles ?? null,
});

/** Réponses des catégories facultatives (urinaire, saignements, nuit et journée, rapports) : null tant qu'elles ne sont pas renseignées. */
export const categoriesDe = (source: Partial<CategoriesBilan>): CategoriesBilan => ({
  douleurUriner: source.douleurUriner ?? null,
  intensiteDouleurUriner: source.intensiteDouleurUriner ?? null,
  enviesUrinaires: source.enviesUrinaires ?? null,
  difficulteVider: source.difficulteVider ?? null,
  sangUrines: source.sangUrines ?? null,
  saignementsHorsRegles: source.saignementsHorsRegles ?? null,
  abondanceSaignementsHorsRegles: source.abondanceSaignementsHorsRegles ?? null,
  nuit: source.nuit ?? null,
  reveilsDouleur: source.reveilsDouleur ?? null,
  limitationJournee: source.limitationJournee ?? null,
  absenceTravail: source.absenceTravail ?? null,
  activiteAnnulee: source.activiteAnnulee ?? null,
  douleurRapport: source.douleurRapport ?? null,
});

export const formulaireVide = (): FormulaireBilan => ({
  douleurMoyenne: null,
  emotions: [],
  fatigue: null,
  stressPro: null,
  stressPerso: null,
  commentaire: '',
  ...corpsDe({}),
  ...categoriesDe({}),
});

export const formulaireDepuisBilan = (bilan: BilanQuotidien): FormulaireBilan => ({
  douleurMoyenne: bilan.douleurMoyenne,
  emotions: emotionsDuBilan(bilan),
  fatigue: bilan.fatigue ?? null,
  stressPro: bilan.stressPro ?? null,
  stressPerso: bilan.stressPerso ?? null,
  commentaire: bilan.commentaire ?? '',
  ...corpsDe(bilan),
  ...categoriesDe(bilan),
});

const transitSuiteRenseignee = (corps: CorpsBilan): boolean => corps.douleurSelle !== null || corps.nausees !== null || corps.sangSelles !== null;

/** Le bloc Corps contient-il au moins une réponse ? (sert à l'ouvrir d'office en modification) */
export const corpsRenseigne = (corps: CorpsBilan): boolean =>
  corps.pas !== null || corps.hydratation !== null || corps.gluten || corps.lactose || corps.grignotage ||
  corps.selles !== null || corps.crampesEstomac !== null || corps.ballonnements !== null || transitSuiteRenseignee(corps);

/** Résumé du bloc Corps replié, ex. « Transit · 5 000 pas · 1,5 L · Gluten ». */
export const resumeCorps = (corps: CorpsBilan): string => {
  const parties = [
    corps.selles !== null || corps.crampesEstomac !== null || corps.ballonnements !== null || transitSuiteRenseignee(corps) ? 'Transit' : null,
    corps.pas !== null ? `${corps.pas.toLocaleString('fr-FR')} pas` : null,
    corps.hydratation !== null ? `${corps.hydratation.toLocaleString('fr-FR')} L` : null,
    ...consommationsAlimentaires.filter((c) => corps[c.cle]).map((c) => c.libelle),
  ].filter((partie): partie is string => partie !== null);
  return parties.length ? parties.join(' · ') : 'Transit, alimentation, pas, hydratation';
};

/** Date envoyée à l'API : midi, heure locale, sans fuseau (le jour ne peut pas glisser en UTC). */
const dateDeSaisie = (date: Date | string): string =>
  typeof date === 'string' ? date : format(date, "yyyy-MM-dd'T'12:00:00");

const messageErreur = (error: unknown): string => {
  const message = (error as { response?: { data?: { message?: unknown } } })?.response?.data?.message;
  return typeof message === 'string' && message.length > 0 ? message : "Impossible d'enregistrer le bilan.";
};

export const useSaisieBilan = (options: UseSaisieBilanOptions) => {
  const estModification = options.bilan !== undefined;
  /** Bilan saisi avant les émotions : son humeur suffit, les émotions deviennent facultatives. */
  const ancienneHumeur = options.bilan?.mood ?? null;

  const initial = options.bilan ? formulaireDepuisBilan(options.bilan) : formulaireVide();
  const formulaire = ref<FormulaireBilan>(structuredClone(initial));
  const instantane = ref(JSON.stringify(initial));
  const enregistrement = ref(false);

  const manquants = computed<ChampManquant[]>(() => {
    const liste: ChampManquant[] = [];
    if (formulaire.value.douleurMoyenne === null) liste.push('douleur');
    if (formulaire.value.emotions.length === 0 && !ancienneHumeur) liste.push('emotions');
    return liste;
  });

  const transitComplet = computed(() => estTransitComplet(formulaire.value));
  /** Catégorie dont une intensité exigée manque encore (urinaire, saignements, nuit et journée…) ; le transit a son propre message. */
  const categorieIncomplete = computed(() => suiteManquante(toutesLesQuestions, formulaire.value));
  const estEnregistrable = computed(() => manquants.value.length === 0 && transitComplet.value && !categorieIncomplete.value && !enregistrement.value);
  const estModifie = computed(() => JSON.stringify(formulaire.value) !== instantane.value);
  const veilleDisponible = computed(() => options.bilanVeille.value !== undefined);

  /** Reprend le bloc Corps de la veille ; jamais la douleur ni les émotions, propres au jour même. */
  const reprendreHier = () => {
    const veille = options.bilanVeille.value;
    if (!veille) return;
    formulaire.value = { ...formulaire.value, ...corpsDe(veille) };
  };

  const versSaisie = (douleur: number): BilanQuotidienSaisie => {
    const { emotions, commentaire, fatigue, stressPro, stressPerso } = formulaire.value;
    return {
      ...corpsDe(formulaire.value),
      ...categoriesDe(formulaire.value),
      fatigue,
      stressPro,
      stressPerso,
      id: options.bilan?.id ?? 0,
      carnetSanteId: options.carnetSanteId,
      date: dateDeSaisie(options.bilan?.date ?? options.date),
      mood: ancienneHumeur,
      emotions: emotions.map((emotion) => ({ emotion })),
      douleurMoyenne: douleur,
      commentaire: commentaire.trim() || null,
    };
  };

  const enregistrer = async (): Promise<boolean> => {
    const douleur = formulaire.value.douleurMoyenne;
    if (!estEnregistrable.value || douleur === null) return false;

    enregistrement.value = true;
    try {
      const saisie = versSaisie(douleur);
      let id = saisie.id;
      if (estModification) {
        await apiService.putBilanQuotidien(saisie);
      } else {
        id = (await apiService.postBilanQuotidien(saisie)).id;
      }
      instantane.value = JSON.stringify(formulaire.value);
      toast({
        title: estModification ? 'Bilan mis à jour' : 'Bilan enregistré',
        variant: 'custom',
      });
      options.onEnregistre({ ...saisie, id });
      return true;
    } catch (error) {
      toast({ title: 'Erreur', description: messageErreur(error), variant: 'destructive' });
      return false;
    } finally {
      enregistrement.value = false;
    }
  };

  return {
    formulaire,
    estModification,
    ancienneHumeur,
    manquants,
    transitComplet,
    categorieIncomplete,
    estEnregistrable,
    estModifie,
    enregistrement,
    veilleDisponible,
    corpsInitialRenseigne: corpsRenseigne(initial),
    reprendreHier,
    enregistrer,
  };
};
