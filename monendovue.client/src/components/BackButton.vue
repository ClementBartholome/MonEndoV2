<template>
  <div class="flex justify-between items-center w-full gap-4">
    <router-link to="/">
      <Button variant="custom"
              class="flex gap-2 items-center cursor-pointer hover:opacity-80 transition-opacity">
        <i class="material-symbols-outlined ">arrow_back</i>
        <span class="hide-xsm">Revenir en arrière</span>
      </Button>
    </router-link>
  </div>
</template>

<script setup lang="ts">
import {Button} from '@/components/ui/button'
</script>