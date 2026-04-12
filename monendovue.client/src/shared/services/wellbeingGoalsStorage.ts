export interface WellbeingGoals {
  hydrationLitersGoal: number;
  stepsGoal: number;
  stressMaxGoal: number;
  fatigueMaxGoal: number;
  painMaxGoal: number;
}

const STORAGE_KEY = 'monendo.wellbeing-goals';

export const DEFAULT_WELLBEING_GOALS: WellbeingGoals = {
  hydrationLitersGoal: 1.5,
  stepsGoal: 10000,
  stressMaxGoal: 3,
  fatigueMaxGoal: 3,
  painMaxGoal: 5,
};

export const getWellbeingGoals = (): WellbeingGoals => {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return DEFAULT_WELLBEING_GOALS;

  try {
    const parsed = JSON.parse(raw) as Partial<WellbeingGoals>;
    return {
      hydrationLitersGoal: Number(parsed.hydrationLitersGoal ?? DEFAULT_WELLBEING_GOALS.hydrationLitersGoal),
      stepsGoal: Number(parsed.stepsGoal ?? DEFAULT_WELLBEING_GOALS.stepsGoal),
      stressMaxGoal: Number(parsed.stressMaxGoal ?? DEFAULT_WELLBEING_GOALS.stressMaxGoal),
      fatigueMaxGoal: Number(parsed.fatigueMaxGoal ?? DEFAULT_WELLBEING_GOALS.fatigueMaxGoal),
      painMaxGoal: Number(parsed.painMaxGoal ?? DEFAULT_WELLBEING_GOALS.painMaxGoal),
    };
  } catch {
    return DEFAULT_WELLBEING_GOALS;
  }
};

export const saveWellbeingGoals = (goals: WellbeingGoals): void => {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(goals));
};

