import { expect, test } from '@playwright/test';
import { enTableau } from '@/shared/utils/json';

test.describe('enTableau', () => {
  test('renvoie un tableau tel quel', () => {
    expect(enTableau([1, 2])).toEqual([1, 2]);
  });

  test('déplie une liste sérialisée avec ReferenceHandler.Preserve', () => {
    expect(enTableau({ $values: ['a', 'b'] })).toEqual(['a', 'b']);
  });

  test('renvoie un tableau vide pour une valeur absente', () => {
    expect(enTableau(null)).toEqual([]);
    expect(enTableau(undefined)).toEqual([]);
    expect(enTableau({} as { $values: number[] })).toEqual([]);
  });
});
