<script setup lang="ts">

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@/components/ui/dropdown-menu";

import {useAuthStore} from "@/store/auth";
import {onMounted} from "vue";
import router from "@/router";

const auth = useAuthStore();

const user = auth.getUser();

onMounted(() => {
  auth.checkAuth();
});

const handleLogout = async () => {
  await auth.logout();
  router.push('/login');
}
</script>

<template>
  <header>
    <nav class="navbar-side md:h-full flex items-center justify-between border-b shadow-lg mb-3">
      <a href="/">
        <img class="logo" src="../images/MonEndo_transparent.png" alt="">
      </a>
      <div class="flex items-center justify-between mb-auto">
        <ul class="flex-grow">
          <li class="flex items-center" :class="{ active: $route.path === '/' }">
            <router-link to="/" class="flex flex-col items-center text-dark w-full">
              <span class="material-symbols-outlined">clinical_notes</span>
              <span>Dashboard</span>
            </router-link>
          </li>
          <li class="flex items-center" :class="{ active: $route.path === '/douleurs' }">
            <router-link to="/douleurs" class="flex flex-col items-center text-dark w-full">
              <span class="material-symbols-outlined">sick</span>
              <span>Douleurs</span>
            </router-link>
          </li>
          <li class="flex items-center" :class="{ active: $route.path === '/activite' }">
            <router-link to="/activite" class="flex flex-col items-center text-dark w-full">
              <span class="material-symbols-outlined">directions_run</span>
              <span>Activité</span>
            </router-link>
          </li>
          <li class="flex items-center" :class="{ active: $route.path === '/medicaments' }">
            <router-link to="/medicaments" class="flex flex-col items-center text-dark w-full">
              <span class="material-symbols-outlined">pill</span>
              <span>Traitements</span>
            </router-link>
          </li>
          <li class="flex items-center" :class="{ active: $route.path === '/cycle' }">
            <router-link to="/cycle" class="flex flex-col items-center text-dark w-full">
              <span class="material-symbols-outlined">menstrual_health</span>
              <span>Cycle</span>
            </router-link>
          </li>
          <li class="flex items-center hidden md:block" :class="{ active: $route.path === '/export' }">
            <router-link to="/export" class="flex flex-col items-center text-dark w-full">
              <span class="material-symbols-outlined">picture_as_pdf</span>
              <span>Export PDF</span>
            </router-link>
          </li>
        </ul>
      </div>
    </nav>

    <div class="user-navbar flex items-center justify-between w-full absolute top-0 right-0 py-5 px-8  gap-4">
      <!--      <a href="#" class="flex items-center text-dark">-->
      <!--        <span class="material-symbols-outlined">notifications</span>-->
      <!--      </a>-->
      <!--      <span class="material-symbols-outlined">account_circle</span>-->
      <a href="/">
        <img class="logo mobile" src="../images/MonEndoTest.png" alt="Logo du site MonEndo">
        <img class="hidden" src="../images/MonEndoIconMobile.jpg" alt="Logo du site MonEndo">
      </a>
      <div class="relative flex justify-center text-left h-6">
        <DropdownMenu>
          <DropdownMenuTrigger>
            <span class="material-symbols-outlined">
            person
            </span> 
            <span class="material-symbols-outlined">arrow_drop_down</span>
          </DropdownMenuTrigger>
          <DropdownMenuContent>
            <!--            <DropdownMenuItem>-->
            <!--              <a href="#">Profil</a>-->
            <!--            </DropdownMenuItem>-->
            <!--            <DropdownMenuItem>-->
            <!--              <a href="#">Paramètres</a>-->
            <!--            </DropdownMenuItem>-->
            <!--            <DropdownMenuSeparator/>-->
            <dropdown-menu-item>
              <router-link to="/parametres">Paramètres</router-link>
            </dropdown-menu-item>
            <DropdownMenuItem>
              <button @click="handleLogout">Déconnexion</button>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  </header>
</template>

<style scoped>

</style>