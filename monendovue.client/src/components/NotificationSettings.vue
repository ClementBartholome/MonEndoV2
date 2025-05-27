<template>
  <div class="notification-settings flex gap-2 mb-4 items-center">
    <i class="material-symbols-outlined">notifications</i>
    <h3 class="text-headline text-2xl">Notifications push</h3>
    <Switch :checked="isGranted" @update:checked="toggleNotificationPermission" />
  </div>
  <p v-if="isGranted">Les notifications sont activées</p>
  <p v-else-if="isDenied">Les notifications sont refusées. Veuillez les réinitialiser dans les paramètres du navigateur.</p>
  <p v-else>Les notifications ne sont pas activées.</p>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import {Switch} from '@/components/ui/switch';

const isGranted = ref(getNotificationPermission() === 'granted');
const isDenied = ref(getNotificationPermission() === 'denied');

function toggleNotificationPermission(checked: boolean) {
  if (Notification.permission === 'granted') {
    localStorage.setItem('notification-permission', checked ? 'granted' : 'denied');
  } else if (Notification.permission === 'denied') {
    Notification.requestPermission().then((choice) => {
      if (choice === 'granted') {
        localStorage.setItem('notification-permission', checked ? 'granted' : 'denied');
      } else {
        localStorage.setItem('notification-permission', 'denied');
      }
      updatePermissionState();
    });
  } else if (Notification.permission === 'default') {
    Notification.requestPermission().then((choice) => {
      if (choice === 'granted') {
        localStorage.setItem('notification-permission', checked ? 'granted' : 'denied');
      } else {
        localStorage.setItem('notification-permission', 'denied');
      }
      updatePermissionState();
    });
  }
  updatePermissionState();
}

function getNotificationPermission() {
  if (Notification.permission === 'granted') {
    return localStorage.getItem('notification-permission') || 'granted';
  } else {
    return Notification.permission;
  }
}

function updatePermissionState() {
  isGranted.value = getNotificationPermission() === 'granted';
  isDenied.value = getNotificationPermission() === 'denied';
}
</script>