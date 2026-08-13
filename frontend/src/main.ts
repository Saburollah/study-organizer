import './assets/main.css'
import { i18n } from './i18n'

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
app.use(i18n)

const authStore = useAuthStore(pinia)

configureAccessTokenProvider(
  () => authStore.accessToken,
)

app.mount('#app')
