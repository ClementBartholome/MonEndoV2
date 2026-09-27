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
.fade-enter-active,
.fade-leave-active {
  transition: opacity 1s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.page-enter-active, .page-leave-active {
  transition: opacity 0.5s ease, transform 0.5s ease;
}
.page-enter-from, .page-leave-to {
  opacity: 0;
  transform: translateX(10px);
}
</style>