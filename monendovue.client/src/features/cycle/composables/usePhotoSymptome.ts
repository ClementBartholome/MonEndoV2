import { onBeforeUnmount, ref } from 'vue';
import { preparePhotoForUpload } from '@/shared/utils/safeImageUpload';
import { estImageAcceptee, LIBELLE_TAILLE_MAX_PHOTO, TAILLE_MAX_FICHIER, TAILLE_MAX_PHOTO, tailleLisible } from '../utils/photo';

export type SourcePhoto = 'camera' | 'gallery';

/**
 * Photo choisie pour un symptôme : contrôle du fichier, conversion HEIC et compression, aperçu. Un refus s'affiche
 * dans le formulaire (`message`), sans bloquer le reste de la saisie.
 */
export function usePhotoSymptome() {
    const fichier = ref<File | null>(null);
    const source = ref<SourcePhoto | null>(null);
    const apercu = ref('');
    const preparation = ref(false);
    const message = ref<string | null>(null);

    function liberer() {
        if (apercu.value) URL.revokeObjectURL(apercu.value);
        apercu.value = '';
    }

    function retirer() {
        liberer();
        fichier.value = null;
        source.value = null;
        message.value = null;
    }

    async function choisir(choisi: File | null, depuis: SourcePhoto) {
        if (!choisi) return;
        retirer();
        if (choisi.size === 0) {
            message.value = 'La photo est vide ou incomplète. Réessaie.';
            return;
        }
        if (choisi.size > TAILLE_MAX_FICHIER) {
            message.value = `Le fichier dépasse ${tailleLisible(TAILLE_MAX_FICHIER)}. Choisis une photo plus légère.`;
            return;
        }
        if (!estImageAcceptee(choisi)) {
            message.value = 'Choisis une image (JPG, PNG, WEBP ou HEIC).';
            return;
        }
        preparation.value = true;
        try {
            const prete = await preparePhotoForUpload(choisi, { targetMaxBytes: TAILLE_MAX_PHOTO });
            if (prete.file.size > TAILLE_MAX_PHOTO) {
                message.value = `La photo doit faire moins de ${LIBELLE_TAILLE_MAX_PHOTO} (${tailleLisible(prete.file.size)}).`;
                return;
            }
            fichier.value = prete.file;
            source.value = depuis;
            apercu.value = URL.createObjectURL(prete.file);
        } catch {
            message.value = 'La photo n\'a pas pu être préparée. Essaie avec une autre image.';
        } finally {
            preparation.value = false;
        }
    }

    onBeforeUnmount(liberer);

    return { fichier, source, apercu, preparation, message, choisir, retirer };
}
