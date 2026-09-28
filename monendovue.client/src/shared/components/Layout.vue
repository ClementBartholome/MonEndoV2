<script setup lang="ts">
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

    <div class="user-navbar flex items-center justify-between w-full absolute top-0 right-0 py-3 px-4 lg:py-5 lg:px-8 gap-4">
      <router-link to="/" aria-label="Accueil MonEndo" class="lg:invisible">
        <img class="h-14 w-14 object-contain" src="@/images/MonEndo_transparent.png" alt="">
      </router-link>
      <DropdownMenu>
        <DropdownMenuTrigger aria-label="Mon compte" class="flex h-11 w-11 items-center justify-center rounded-full">
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
