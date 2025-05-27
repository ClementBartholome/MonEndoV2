<template>
  <div class="flex-column-container">
    <div class="flex items-center w-full gap-4">
      <router-link to="/">
        <Button variant="custom"
                class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
          <i class="material-symbols-outlined ">arrow_back</i>
          <span class="hide-xsm">Revenir en arrière</span>
        </Button>
      </router-link>
    </div>
    <div class="date-picker-container flex flex-col  self-baseline w-full ">
      <h2 class="text-2xl flex gap-2"><i class="material-symbols-outlined text-3xl ">clinical_notes</i>Carnet de suivi
      </h2>
      <div class="flex gap-4">
        <div class="flex flex-col items-start" style="max-width: 46%">
          <label for="month-year">Sélectionner un mois :</label>
          <input class="max-w-40" type="month" id="month-year" v-model="selectedMonthYear"/>
        </div>
        <Button variant="custom" @click="exportToPDF"
                class="flex gap-2 items-center cursor-pointer ml-auto self-end hover:opacity-80 transition-opacity ">
          <i class="material-symbols-outlined">picture_as_pdf</i>
          Exporter en PDF
        </Button>
      </div>
    </div>

    <section class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">

      <table v-if="computedDonneesCarnetSante && !isLoading" id="dataTable" class="responsive-table">
        <thead>
        <tr class="header-row bg-gray-200">
          <th>Date</th>
          <th v-for="day in daysInMonth" :key="day">{{ day }}</th>
        </tr>
        <tr class="highlight-row">
          <td :colspan="daysInMonth.length + 1">Traitements</td>
        </tr>
        </thead>
        <tbody>
        <tr v-for="medicament in computedDonneesCarnetSante.medicaments.$values" :key="medicament.id">
          <td>{{ medicament.nom }}</td>
          <td v-for="day in daysInMonth" :key="day">
            <input type="checkbox" :checked="dataExists('donneesMedicament', medicament.nom, day)"
                   :data-medicament="medicament.nom"
                   :data-day="day" onclick="return false;"/>
          </td>
        </tr>
        <tr class="highlight-row">
          <td :colspan="daysInMonth.length + 1">Règles</td>
        </tr>
        <tr>
          <td>Règles</td>
          <td v-for="day in daysInMonth" :key="day">
            <input type="checkbox" :checked="dataExists('jourRegles', null, day)" :data-day="day" onclick="return false;"/>          </td>
        </tr>
        <tr class="highlight-row">
          <td :colspan="daysInMonth.length + 1">Douleurs</td>
        </tr>
        <tr v-for="typeDouleur in getUniqueTypeData('donneesDouleur', 'typeDouleur')" :key="typeDouleur">
          <td>{{ typeDouleur }}</td>
          <td v-for="day in daysInMonth" :key="day">
            {{
              calculateAverageIntensity(getDailyTypeData('donneesDouleur', 'typeDouleur', typeDouleur, day))
            }}
          </td>
        </tr>
        <tr class="highlight-row">
          <td :colspan="daysInMonth.length + 1">Activité physique</td>
        </tr>
        <tr v-for="typeActivite in getUniqueTypeData('donneesActivitePhysique', 'typeActivite')" :key="typeActivite">
          <td>{{ typeActivite }}</td>
          <td v-for="day in daysInMonth" :key="day">
            {{
              calculateAverageIntensity(getDailyTypeData('donneesActivitePhysique', 'typeActivite', typeActivite, day))
            }}
          </td>
        </tr>
        <tr class="highlight-row">
          <td :colspan="daysInMonth.length + 1">Transit</td>
        </tr>
        <tr v-for="typeTransit in getUniqueTypeData('donneesTransit', 'typeEvenement')" :key="typeTransit">
          <td>{{ typeTransit }}</td>
          <td v-for="day in daysInMonth" :key="day">
            <input type="checkbox" :checked="dataExists('donneesTransit', typeTransit, day)"
                   :data-transit="typeTransit"
                   :data-day="day" onclick="return false;"/>
          </td>
        </tr>
        <tr class="highlight-row">
          <td :colspan="daysInMonth.length + 1">Alimentation</td>
        </tr>
        <tr>
          <td>Lactose</td>
          <td v-for="day in daysInMonth" :key="day">
            <input type="checkbox" :checked="dataExists('bilansQuotidiens', 'lactose', day) && dataIsTrue('bilansQuotidiens', 'lactose', day)" :data-alimentation="day" onclick="return false;"/>
          </td>
        </tr>
        <tr>
          <td>Grignotage</td>
          <td v-for="day in daysInMonth" :key="day">
            <input type="checkbox" :checked="dataExists('bilansQuotidiens', 'grignotage', day) && dataIsTrue('bilansQuotidiens', 'grignotage', day)" :data-alimentation="day" onclick="return false;"/>
          </td>
        </tr>
        <tr>
          <td>Gluten</td>
          <td v-for="day in daysInMonth" :key="day">
            <input type="checkbox" :checked="dataExists('bilansQuotidiens', 'gluten', day) && dataIsTrue('bilansQuotidiens', 'gluten', day)" :data-alimentation="day" onclick="return false;"/>
          </td>
        </tr>
        </tbody>
      </table>
      <Skeleton class="h-[300px] w-full mt-4 rounded-xl" v-else></Skeleton>
    </section>
  </div>
</template>

<script setup lang="ts">
import {ref, onMounted, computed, watch} from 'vue';
import {jsPDF} from 'jspdf';
import autoTable from 'jspdf-autotable';
import {format, parseISO, isValid, startOfMonth, endOfMonth, getDaysInMonth} from 'date-fns';
import {fr} from 'date-fns/locale';
import apiService from '@/services/apiService';
import {Button} from "@/components/ui/button";
import {useAuthStore} from "@/store/auth";
import {Skeleton} from "@/components/ui/skeleton";

const selectedMonthYear = ref(format(new Date(), 'yyyy-MM'));
const carnetSanteId = useAuthStore().user?.carnetSanteId;
const donneesCarnetSante = ref();
const isLoading = ref(true);

const daysInMonth = computed(() => {
  const [year, month] = selectedMonthYear.value.split('-');
  return Array.from({length: getDaysInMonth(new Date(parseInt(year), parseInt(month) - 1))}, (_, i) => i + 1);
});

const computedDonneesCarnetSante = computed(() => donneesCarnetSante.value);

watch(() => selectedMonthYear.value, async () => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  if (carnetSanteId !== undefined) {
    isLoading.value = true;
    donneesCarnetSante.value = await apiService.getDonneesCarnetSanteByMonth(carnetSanteId, month, year);
    setTimeout(() => isLoading.value = false, 500);
  }
});

onMounted(async () => {
  const [year, month] = selectedMonthYear.value.split('-').map(Number);
  if (carnetSanteId !== undefined) {
    donneesCarnetSante.value = await apiService.getDonneesCarnetSanteByMonth(carnetSanteId, month, year);
    isLoading.value = false;
  }
});

const dataExists = (dataType, type, day) => {
  if (!donneesCarnetSante.value || !donneesCarnetSante.value[dataType]) {
    return false;
  }

  const [year, month] = selectedMonthYear.value.split('-');
  const startDate = startOfMonth(new Date(parseInt(year), parseInt(month) - 1));
  const endDate = endOfMonth(new Date(parseInt(year), parseInt(month) - 1));

  const filteredData = donneesCarnetSante.value[dataType].$values.filter(item => {
    if (!item.date) {
      return false;
    }
    const itemDate = parseISO(item.date);
    return isValid(itemDate) && itemDate >= startDate && itemDate <= endDate && (!type || item.typeEvenement === type || item.nomMedicament === type || item[type]);
  });

  return filteredData.some(item => parseISO(item.date).getDate() === day);
};

const dataIsTrue = (dataType, type, day) => {
  if (!donneesCarnetSante.value || !donneesCarnetSante.value[dataType]) {
    return false;
  }
  
  const [year, month] = selectedMonthYear.value.split('-');
  const startDate = startOfMonth(new Date(parseInt(year), parseInt(month) - 1));
  const endDate = endOfMonth(new Date(parseInt(year), parseInt(month) - 1));
  
  const filteredData = donneesCarnetSante.value[dataType].$values.filter(item => {
    if (!item.date) {
      return false;
    }
    const itemDate = parseISO(item.date);
    return isValid(itemDate) && itemDate >= startDate && itemDate <= endDate && (!type || item.typeEvenement === type || item.nomMedicament === type || item[type]);
  });
  
  return filteredData.some(item => parseISO(item.date).getDate() === day && item[type] === true);
}

const getUniqueTypeData = (dataType, type) => {
  return computedDonneesCarnetSante.value[dataType].$values.reduce((acc, item) => {
    if (!acc.includes(item[type])) {
      acc.push(item[type]);
    }
    return acc;
  }, []);
};

const getDailyTypeData = (dataType, typeKey, type, day) => {
  if (!donneesCarnetSante.value || !donneesCarnetSante.value[dataType]) {
    return [];
  }

  const [year, month] = selectedMonthYear.value.split('-');
  const startDate = startOfMonth(new Date(parseInt(year), parseInt(month) - 1));
  const endDate = endOfMonth(new Date(parseInt(year), parseInt(month) - 1));
  
  // Filtre les données pour le type spécifié et la période du mois sélectionné
  const filteredData = donneesCarnetSante.value[dataType].$values.filter(item => {
    if (!item.date) {
      return false;
    }
    const itemDate = parseISO(item.date);
    const isValidDate = isValid(itemDate);
    const isInRange = itemDate >= startDate && itemDate <= endDate;
    const isTypeMatch = item[typeKey] === type;

    return isValidDate && isInRange && isTypeMatch;
  }); 
  
  // Retourne les données filtrées pour le jour spécifié, on calcule ensuite la moyenne du jour avec la fonction calculateAverageIntensity
  return filteredData.filter(item => parseISO(item.date).getDate() === day);
};

const calculateAverageIntensity = (dailyItems) => {
  if (dailyItems.length === 0) return '';
  const averageIntensity = dailyItems.reduce((acc, d) => acc + d.intensite, 0) / dailyItems.length;
  return averageIntensity.toFixed(1).replace(/\.0+$/, '');
};

const exportToPDF = () => {
  // Crée un nouveau document PDF avec une orientation paysage, des unités en mm et un format A4
  const doc = new jsPDF({
    orientation: 'landscape',
    unit: 'mm',
    format: 'a4',
  });

  // Vérifie si les données du carnet de santé sont disponibles
  if (!donneesCarnetSante.value) {
    console.error('No data available');
    return;
  }

  // Récupère l'année et le mois sélectionnés
  const [year, month] = selectedMonthYear.value.split('-');
  const startDate = startOfMonth(new Date(parseInt(year), parseInt(month) - 1));
  const daysInMonth = getDaysInMonth(startDate);

  // Ajoute le titre et la date au document PDF
  doc.text('Carnet de suivi - Coralie Owczaruk', 105, 10, {align: 'center'});
  doc.text(format(startDate, 'MMMM yyyy', {locale: fr}), 10, 30);

  // Calcule les dimensions des colonnes du tableau
  const pageWidth = doc.internal.pageSize.getWidth();
  const margin = 10;
  const availableWidth = pageWidth - 2 * margin;
  const firstColumnWidth = availableWidth * 0.15;
  const otherColumnsWidth = (availableWidth - firstColumnWidth) / daysInMonth;

  // Crée les en-têtes du tableau
  const headers = [['Date', ...Array.from({length: daysInMonth}, (_, i) => (i + 1).toString())]];

  // Crée les lignes du tableau pour les médicaments
  const rows = donneesCarnetSante.value.medicaments.$values.map(medicament => {
    const row = [medicament.nom];
    for (let day = 1; day <= daysInMonth; day++) {
      const checkbox = document.querySelector(`input[type="checkbox"][data-medicament="${medicament.nom}"][data-day="${day}"]`) as HTMLInputElement;
      row.push(checkbox && checkbox.checked ? 'X' : '');
    }
    return row;
  });

  // Ajoute une ligne pour les traitements
  rows.unshift([{
    content: 'Traitements',
    colSpan: daysInMonth + 1,
    styles: {halign: 'left', fillColor: [240, 240, 240], fontStyle: 'bold'}
  }]);
  // Ajoute une ligne pour les jours de règles
  rows.push([{
    content: 'Règles',
    colSpan: daysInMonth + 1,
    styles: {halign: 'left', fillColor: [240, 240, 240], fontStyle: 'bold'}
  }]);

  // Crée une ligne pour chaque jour de règles
  const jourRegles = donneesCarnetSante.value.jourRegles.$values;
  const row = ['Règles'];
  for (let day = 1; day <= daysInMonth; day++) {
    const checkbox = jourRegles.some(jour => new Date(jour.date).getDate() === day);
    row.push(checkbox ? 'X' : '');
  }
  rows.push(row);
  // Ajoute une ligne pour les douleurs
  rows.push([{
    content: 'Douleurs',
    colSpan: daysInMonth + 1,
    styles: {halign: 'left', fillColor: [240, 240, 240], fontStyle: 'bold'}
  }]);

  // Groupe les douleurs par type et crée une ligne pour chaque type de douleur
  const groupedDouleurs = donneesCarnetSante.value.donneesDouleur.$values.reduce((acc, douleur) => {
    if (!acc[douleur.typeDouleur]) {
      acc[douleur.typeDouleur] = [];
    }
    acc[douleur.typeDouleur].push(douleur);
    return acc;
  }, {});

  Object.keys(groupedDouleurs).forEach(typeDouleur => {
    const row = [typeDouleur];
    for (let day = 1; day <= daysInMonth; day++) {
      const dailyDouleurs = groupedDouleurs[typeDouleur].filter(d => new Date(d.date).getDate() === day);
      const averageIntensity = dailyDouleurs.length > 0 ? (dailyDouleurs.reduce((sum, d) => sum + d.intensite, 0) / dailyDouleurs.length).toFixed(1).replace(/\.0+$/, '') : '';
      row.push(averageIntensity);
    }
    rows.push(row);
  });

  // Ajoute une ligne pour les activités physiques
  rows.push([{
    content: 'Activités Physiques',
    colSpan: daysInMonth + 1,
    styles: {halign: 'left', fillColor: [240, 240, 240], fontStyle: 'bold'}
  }]);

  // Groupe les activités physiques par type et crée une ligne pour chaque type d'activité
  const groupedActivites = donneesCarnetSante.value.donneesActivitePhysique.$values.reduce((acc, activite) => {
    if (!acc[activite.typeActivite]) {
      acc[activite.typeActivite] = [];
    }
    acc[activite.typeActivite].push(activite);
    return acc;
  }, {});

  Object.keys(groupedActivites).forEach(typeActivite => {
    const row = [typeActivite];
    for (let day = 1; day <= daysInMonth; day++) {
      const dailyActivites = groupedActivites[typeActivite].filter(a => new Date(a.date).getDate() === day);
      const averageIntensity = dailyActivites.length > 0 ? (dailyActivites.reduce((sum, a) => sum + a.intensite, 0) / dailyActivites.length).toFixed(1).replace(/\.0+$/, '') : '';
      row.push(averageIntensity);
    }
    rows.push(row);
  });

  // Ajoute une ligne pour le transit
  rows.push([{
    content: 'Transit',
    colSpan: daysInMonth + 1,
    styles: {halign: 'left', fillColor: [240, 240, 240], fontStyle: 'bold'}
  }]);

  // Groupe les transits par type et crée une ligne pour chaque type de transit
  const groupedTransits = donneesCarnetSante.value.donneesTransit.$values.reduce((acc, transit) => {
    if (!acc[transit.typeEvenement]) {
      acc[transit.typeEvenement] = [];
    }
    acc[transit.typeEvenement].push(transit);
    return acc;
  }, {});

  Object.keys(groupedTransits).forEach(typeTransit => {
    const row = [typeTransit];
    for (let day = 1; day <= daysInMonth; day++) {
      const checkbox = document.querySelector(`input[type="checkbox"][data-transit="${typeTransit}"][data-day="${day}"]`) as HTMLInputElement;
      row.push(checkbox && checkbox.checked ? 'X' : '');
    }
    rows.push(row);
  });
  
  // Ajoute une ligne pour l'alimentation
  rows.push([{
    content: 'Alimentation',
    colSpan: daysInMonth + 1,
    styles: {halign: 'left', fillColor: [240, 240, 240], fontStyle: 'bold'}
  }]);

  const alimentationTypes = ['lactose', 'grignotage', 'gluten'];
  alimentationTypes.forEach(type => {
    const row = [type.charAt(0).toUpperCase() + type.slice(1)];
    for (let day = 1; day <= daysInMonth; day++) {
      const checkbox = document.querySelector(`input[type="checkbox"][data-alimentation="${day}"]`) as HTMLInputElement;
      row.push(checkbox && checkbox.checked && dataIsTrue('bilansQuotidiens', type, day) ? 'X' : '');    }
    rows.push(row);
  });

  // Génère le tableau dans le document PDF
  autoTable(doc, {
    head: headers,
    body: rows,
    startY: 50,
    styles: {halign: 'left', lineWidth: 0.1, lineColor: [0, 0, 0]},
    headStyles: {fillColor: [211, 211, 211], textColor: [0, 0, 0]},
    alternateRowStyles: {fillColor: [240, 240, 240]},
    columnStyles: {
      0: {cellWidth: firstColumnWidth},
      _: {cellWidth: otherColumnsWidth}
    }
  });

  // Sauvegarde le document PDF avec un nom de fichier formaté
  const formattedMonthYear = format(startDate, 'MM_yyyy');
  doc.save(`carnet_de_suivi_${formattedMonthYear}.pdf`);
};

</script>

<style scoped>
.date-picker-container {
  display: flex;
  gap: 10px;
}

.responsive-table {
  width: 100%;
  border-collapse: collapse;
  overflow-x: auto;
  display: block;
}

table {
  width: 100%;
  border-collapse: collapse;
}

th, td {
  border: 1px solid #ddd;
  padding: 5px;
  text-align: left;
}

.header-row {
  background-color: #e0d4d1;
  font-weight: bold;
}

.highlight-row {
  background-color: #faeee7;
  font-weight: bold;
}

td, th {
  text-align: left !important;
  padding-left: 5px !important;
}
</style>