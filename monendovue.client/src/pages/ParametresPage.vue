<template>
  <div class="flex-column-container">
    <BackButton/>
    <h2 class="text-2xl mr-auto ml-4">Paramètres</h2>
    <section
        class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div>
      </div>
      <NotificationSettings/>
    </section>
    <section
        class="container !mt-0  mx-auto py-8 w-full bg-clearer rounded-3xl shadow-xl ml-auto">
      <div class=" flex gap-2 mb-4 items-center">
        <i class="material-symbols-outlined">person</i>
        <h3 class="text-headline text-2xl">Compte utilisateur</h3>
      </div>
      <hr class="mb-4 border-gray-300">      
      <ChangePassword/>
    </section>
  </div>
</template>

<script setup lang="ts">

import BackButton from "@/components/BackButton.vue";
import NotificationSettings from "@/components/NotificationSettings.vue";
import ChangePassword from "@/components/ChangePassword.vue";

</script>

<style scoped>

</style>