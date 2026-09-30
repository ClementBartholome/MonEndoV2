<template>
  <DocumentLegal titre="Mentions légales">
    <section>
      <h2>Éditeur</h2>
      <p>
        MonEndo (monendoapp.fr) est un projet personnel et non commercial édité par {{ EDITEUR.nom }}, également
        directeur de la publication.
        Contact : <a :href="`mailto:${EDITEUR.email}`">{{ EDITEUR.email }}</a>.
      </p>
    </section>

    <section>
      <h2>Hébergement</h2>
      <p>
        Serveur et base de données : OVH SAS, 2 rue Kellermann, 59100 Roubaix, France.
      </p>
      <p>
        Photos de suivi : Microsoft Ireland Operations Limited (Microsoft Azure), One Microsoft Place, South County Business
        Park, Leopardstown, Dublin 18, Irlande.
      </p>
    </section>

    <section>
      <h2>Données personnelles</h2>
      <p>
        L'utilisation de tes données est décrite dans la
        <router-link to="/confidentialite">politique de confidentialité</router-link>.
      </p>
    </section>

    <section>
      <h2>Avertissement</h2>
      <p>
        MonEndo est un outil de suivi personnel. Il ne pose aucun diagnostic et ne remplace pas l'avis d'un professionnel
        de santé.
      </p>
    </section>

    <section>
      <h2>Crédits</h2>
      <p>Police Poppins (licence SIL Open Font License) et icônes Material Symbols de Google (licence Apache 2.0).</p>
    </section>
  </DocumentLegal>
</template>

<script setup lang="ts">
import DocumentLegal from '../components/DocumentLegal.vue';
import {EDITEUR} from '../config/editeur';
</script>
