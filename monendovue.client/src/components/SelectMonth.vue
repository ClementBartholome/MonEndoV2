<template>
  <div class="flex gap-4">
    <div class="flex items-start">
      <input class="w-full" type="month" id="month-year" :value="modelValue" @input="updateValue(($event.target as HTMLInputElement).value)"/>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, defineProps, defineEmits } from 'vue';

const props = defineProps({
  modelValue: {
    type: String,
    required: true
  }
});

const emits = defineEmits(['update:modelValue']);

const updateValue = (value: string) => {
  emits('update:modelValue', value);
};
</script>