import {ref} from 'vue';
import {format} from 'date-fns';
import apiService from '@/shared/services/apiService';

/** Téléchargement de toutes les données de l'utilisatrice (archive ZIP préparée par le serveur). */
export function useExportDonnees() {
    const exportEnCours = ref(false);
    const erreurExport = ref<string | null>(null);

    async function telechargerMesDonnees() {
        if (exportEnCours.value) return;
        exportEnCours.value = true;
        erreurExport.value = null;
        try {
            const archive = await apiService.getExportDonnees();
            enregistrer(archive, `monendo-mes-donnees-${format(new Date(), 'yyyy-MM-dd')}.zip`);
        } catch {
            erreurExport.value = 'L\'export n\'a pas pu être préparé. Réessaie dans quelques minutes.';
        } finally {
            exportEnCours.value = false;
        }
    }

    return {exportEnCours, erreurExport, telechargerMesDonnees};
}

function enregistrer(contenu: Blob, nomFichier: string) {
    const url = URL.createObjectURL(contenu);
    const lien = document.createElement('a');
    lien.href = url;
    lien.download = nomFichier;
    lien.click();
    URL.revokeObjectURL(url);
}
