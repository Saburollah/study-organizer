import {
  createRouter,
  createWebHistory,
  type RouterHistory,
} from 'vue-router'
import LoginView from '@/views/auth/LoginView.vue'

import HomeView from '@/views/HomeView.vue'
import RegisterView from '@/views/auth/RegisterView.vue'
import { useAuthStore } from '@/features/auth/authStore'
import ModulesView from '@/views/modules/ModulesView.vue'
import StudyTasksView from '@/views/tasks/StudyTasksView.vue'
import DashboardView from '@/views/dashboard/DashboardView.vue'
import ProfileView from '@/views/profile/ProfileView.vue'

export function createAppRouter(
  history: RouterHistory = createWebHistory(
    import.meta.env.BASE_URL,
  ),
) {
  const router = createRouter({
    history,
    routes: [
      {
        path: '/',
        name: 'home',
        component: HomeView,
      },
      {
        path: '/login',
        name: 'login',
        component: LoginView,
      },
      {
        path: '/register',
        name: 'register',
        component: RegisterView,
      },
      {
        path: '/dashboard',
        name: 'dashboard',
        component: DashboardView,
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: '/modules',
        name: 'modules',
        component: ModulesView,
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: '/modules/:moduleId/tasks',
        name: 'module-tasks',
        component: StudyTasksView,
        props: true,
        meta: {
          requiresAuth: true,
        },
      },
      {
        path: '/profile',
        name: 'profile',
        component: ProfileView,
        meta: {
          requiresAuth: true,
        },
      },
    ],
  })

  router.beforeEach((to) => {
    const authStore = useAuthStore()

    if (
      to.meta.requiresAuth
      && !authStore.isAuthenticated
    ) {
      return {
        name: 'login',
        query: {
          redirect: to.fullPath,
        },
      }
    }

    if (
      authStore.isAuthenticated
      && (
        to.name === 'login'
        || to.name === 'register'
      )
    ) {
      return {
        name: 'dashboard',
      }
    }

    return true
  })

  return router
}

const router = createAppRouter()

export default router
