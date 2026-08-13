import { moduleService } from '@/features/modules/moduleService'
import { taskService } from '@/features/tasks/taskService'

import type {
  DashboardData,
  DashboardTask,
} from './dashboardModels'

export interface DashboardService {
  getDashboard(): Promise<DashboardData>
}

export class ApiDashboardService
implements DashboardService {
  async getDashboard(): Promise<DashboardData> {
    const modules = await moduleService.getAll()

    const tasksByModule = await Promise.all(
      modules.map(async (module) => {
        const tasks = await taskService.getByModule(module.id)

        return tasks.map<DashboardTask>((task) => ({
          id: task.id,
          moduleId: module.id,
          moduleName: module.name,
          moduleCode: module.code,
          title: task.title,
          dueDateUtc: task.dueDateUtc,
          status: task.status,
        }))
      }),
    )

    return {
      moduleCount: modules.length,
      tasks: tasksByModule.flat(),
    }
  }
}

export const dashboardService: DashboardService =
  new ApiDashboardService()