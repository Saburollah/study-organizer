import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import { useAuthStore } from './features/auth/authStore'
import router from './router'
import {
  configureAccessTokenProvider,
} from './services/api/apiClient'

const app = createApp(App)
const pinia = createPinia()

app.use(pinia)
app.use(router)

const authStore = useAuthStore(pinia)

configureAccessTokenProvider(
  () => authStore.accessToken,
)

app.mount('#app')
