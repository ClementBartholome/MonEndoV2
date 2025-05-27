<template>
  <Layout v-if="auth.user"/>
  <transition name="fade" mode="out-in">
    <RouterView/>
  </transition>
  <Toaster/>
</template>

<script setup lang="ts">
import {RouterView} from 'vue-router';
import Toaster from '@/components/ui/toast/Toaster.vue'
import Layout from "@/components/Layout.vue";
import {useAuthStore} from '@/store/auth';
import {onMounted} from 'vue';

const auth = useAuthStore();

onMounted(() => {
  auth.checkAuth();
});

import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${import.meta.env.VITE_API_URL}notificationHub`)
    .build();

connection.on("ReceiveNotification", (message) => {
  console.log("Notification reçue :", message);
  if (message === "Notification à envoyer") {
    sendNotification();
  }
});

connection.start()
    .then(() => console.log('Connected to SignalR hub'))
    .catch(err => console.error('Error connecting to SignalR hub:', err));


import axios from 'axios';

const ONESIGNAL_APP_ID = 'd3434227-a679-4122-b83d-3d1a4e7c1b19';

const sendNotification = async () => {
  try {
    await axios.post('https://api.onesignal.com/notifications', {
      app_id: ONESIGNAL_APP_ID,
      target_channel: 'push',
      included_segments: ['Total Subscriptions'],
      contents: {
        en: "Don't forget to fill in your daily report!",
        fr: "N'oublie pas de remplir ton bilan quotidien !"
      }
    }, {
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Basic ${import.meta.env.VITE_ONESIGNAL_API_KEY}`
      }
    });
  } catch (error) {
    console.error('Error sending notification:', error);
  }
};
</script>

<style>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 1.5s ease;
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