<template>
  <div class="stepper-container">
    <div class="stepper-wrapper">
      <div
          v-for="(step, index) in steps"
          :key="index"
          class="stepper-item"
          :class="{
          'active': index + 1 === currentStep,
          'completed': index + 1 < currentStep || completedSteps.includes(index + 1),
          'clickable': index + 1 < currentStep || completedSteps.includes(index + 1)
        }"
          @click="handleStepClick(index + 1)"
      >
        <div class="stepper-circle">
          <i v-if="index + 1 < currentStep || completedSteps.includes(index + 1)"
             class="material-symbols-outlined">check</i>
          <span v-else>{{ index + 1 }}</span>
        </div>
        <div class="stepper-label">{{ step.label }}</div>
        <div v-if="index < steps.length - 1" class="stepper-line"></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface StepperStep {
  label: string;
  icon?: string;
}

const props = defineProps<{
  steps: StepperStep[];
  currentStep: number;
  completedSteps: number[];
}>();

const emit = defineEmits<{
  (e: 'step-click', step: number): void;
}>();

const handleStepClick = (step: number) => {
  if (step < props.currentStep || props.completedSteps.includes(step)) {
    emit('step-click', step);
  }
};
</script>