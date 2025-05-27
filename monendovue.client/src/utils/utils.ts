import {format} from 'date-fns';
import apiService from '@/services/apiService';

export const fetchEntries = async (carnetSanteId: number, entryType: string) => {
    const data = await apiService.getDonneesCarnetSante(carnetSanteId);
    return data[entryType].$values.map((d: any) => {
        const date = new Date(d.date);
        const hours = date.getHours().toString().padStart(2, '0');
        const minutes = date.getMinutes().toString().padStart(2, '0');
        const time = `${hours}h${minutes}`;
        return {
            id: d.id,
            type: d.typeDouleur || d.typeActivite,
            date: format(date, 'dd-MM-yyyy').replace(/-/g, '/'),
            time: time,
            intensite: d.intensite,
            duree: d.duree ? `${d.duree}min` : undefined,
            commentaire: d.commentaire ? d.commentaire : 'Pas de commentaire'
        };
    });
};

