import type { StudyTaskStatus } from '@/features/tasks/taskModels'

export interface DashboardTask {
  id: string
  moduleId: string
  moduleName: string
  moduleCode: string | null
  title: string
  dueDateUtc: string
  status: StudyTaskStatus
}

export interface DashboardData {
  moduleCount: number
  tasks: DashboardTask[]
}