import { apiRequest } from '@/services/api/apiClient'

import type {
  SaveStudyTaskRequest,
  StudyTask,
  StudyTaskStatus,
} from './taskModels'

export interface TaskService {
  getByModule(moduleId: string): Promise<StudyTask[]>

  create(
    moduleId: string,
    request: SaveStudyTaskRequest,
  ): Promise<StudyTask>

  update(
    moduleId: string,
    taskId: string,
    request: SaveStudyTaskRequest,
  ): Promise<StudyTask>

  updateStatus(
    moduleId: string,
    taskId: string,
    status: StudyTaskStatus,
  ): Promise<StudyTask>

  delete(moduleId: string, taskId: string): Promise<void>
}

export class HttpTaskService implements TaskService {
  getByModule(moduleId: string): Promise<StudyTask[]> {
    return apiRequest<StudyTask[]>(
      `${this.getCollectionPath(moduleId)}/`,
    )
  }

  create(
    moduleId: string,
    request: SaveStudyTaskRequest,
  ): Promise<StudyTask> {
    return apiRequest<StudyTask>(
      `${this.getCollectionPath(moduleId)}/`,
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
    )
  }

  update(
    moduleId: string,
    taskId: string,
    request: SaveStudyTaskRequest,
  ): Promise<StudyTask> {
    return apiRequest<StudyTask>(
      this.getTaskPath(moduleId, taskId),
      {
        method: 'PUT',
        body: JSON.stringify(request),
      },
    )
  }

  updateStatus(
    moduleId: string,
    taskId: string,
    status: StudyTaskStatus,
  ): Promise<StudyTask> {
    return apiRequest<StudyTask>(
      `${this.getTaskPath(moduleId, taskId)}/status`,
      {
        method: 'PATCH',
        body: JSON.stringify({ status }),
      },
    )
  }

  delete(moduleId: string, taskId: string): Promise<void> {
    return apiRequest<void>(
      this.getTaskPath(moduleId, taskId),
      {
        method: 'DELETE',
      },
    )
  }

  private getCollectionPath(moduleId: string): string {
    return `/api/modules/${encodeURIComponent(moduleId)}/tasks`
  }

  private getTaskPath(
    moduleId: string,
    taskId: string,
  ): string {
    return `${this.getCollectionPath(moduleId)}/${encodeURIComponent(taskId)}`
  }
}

export const taskService: TaskService =
  new HttpTaskService()
