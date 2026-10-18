<script setup lang="ts">
import MotSymbole from '@/shared/components/MotSymbole.vue';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@/shared/components/ui/dropdown-menu";
import NavigationLaterale from "@/shared/components/navigation/NavigationLaterale.vue";
import BarreNavigation from "@/shared/components/navigation/BarreNavigation.vue";
import {useAuthStore} from "@/features/auth/store/auth";
import router from "@/router";

const auth = useAuthStore();

const handleLogout = async () => {
  await auth.logout();
  router.push('/login');
}
</script>

<template>
  <header>
    <NavigationLaterale/>
    <BarreNavigation @deconnexion="handleLogout"/>

    <!-- Bande pleine largeur posée sur la page : elle ne capte que les clics sur le logo et le compte. -->
    <div class="user-navbar pointer-events-none flex items-center justify-between w-full absolute top-0 right-0 py-3 px-4 lg:py-5 lg:px-8 gap-4">
      <router-link to="/" aria-label="Accueil MonEndo" class="pointer-events-auto flex min-h-11 items-center lg:invisible">
        <MotSymbole/>
      </router-link>
      <DropdownMenu>
        <DropdownMenuTrigger aria-label="Mon compte" class="pointer-events-auto flex h-11 w-11 items-center justify-center rounded-full">
          <span class="material-symbols-outlined" aria-hidden="true">person</span>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem as-child>
            <router-link to="/parametres">Paramètres</router-link>
          </DropdownMenuItem>
          <DropdownMenuItem @select="handleLogout">Se déconnecter</DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  </header>
</template>
