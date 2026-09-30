/** Miroirs de `ActiviteViewModel` et `ActiviteDto` (`ActiviteController`). */

export type NiveauActivite = 'Douce' | 'Moderee' | 'Soutenue';
export type EffetActivite = 'NonRenseigne' | 'Soulagee' | 'Pareille' | 'PlusForte';

export interface Activite {
    id: number;
    type: string;
    /** Date locale sans fuseau (« AAAA-MM-JJTHH:mm:ss »). */
    date: string;
    /** Minutes. */
    duree: number;
    niveau: NiveauActivite;
    effet: EffetActivite;
    commentaire: string | null;
}

export type ActiviteSaisie = Omit<Activite, 'id'>;
