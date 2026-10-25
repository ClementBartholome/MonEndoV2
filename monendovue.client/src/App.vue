<template>
  <Layout v-if="auth.user && !route.meta.sansNavigation"/>
  <transition name="fade" mode="out-in">
    <RouterView/>
  </transition>
  <Toaster/>
</template>

<script setup lang="ts">
import {RouterView, useRoute} from 'vue-router';
import Toaster from '@/shared/components/ui/toast/Toaster.vue'
import Layout from "@/shared/components/Layout.vue";
import {useAuthStore} from '@/features/auth/store/auth';
import {onMounted} from 'vue';

const auth = useAuthStore();
const route = useRoute();

onMounted(() => {
  auth.checkAuth();
});
</script>

<style>
/*
 * Changement de page : un fondu bref, seulement pour éviter un saut brutal. On change de page des dizaines de fois par jour :
 * l'ancien fondu (1 s à la sortie puis 1 s à l'entrée) faisait paraître l'application lente.
 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 120ms ease-out;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>