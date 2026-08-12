import { apiRequest } from '@/services/api/apiClient'

import type {
  SaveModuleRequest,
  StudyModule,
} from './moduleModels'

export interface ModuleService {
  getAll(): Promise<StudyModule[]>

  create(
    request: SaveModuleRequest,
  ): Promise<StudyModule>

  update(
    moduleId: string,
    request: SaveModuleRequest,
  ): Promise<StudyModule>

  delete(moduleId: string): Promise<void>
}

export class HttpModuleService
implements ModuleService {
  getAll(): Promise<StudyModule[]> {
    return apiRequest<StudyModule[]>(
      '/api/modules/',
    )
  }

  create(
    request: SaveModuleRequest,
  ): Promise<StudyModule> {
    return apiRequest<StudyModule>(
      '/api/modules/',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
    )
  }

  update(
    moduleId: string,
    request: SaveModuleRequest,
  ): Promise<StudyModule> {
    return apiRequest<StudyModule>(
      `/api/modules/${encodeURIComponent(moduleId)}`,
      {
        method: 'PUT',
        body: JSON.stringify(request),
      },
    )
  }

  delete(moduleId: string): Promise<void> {
    return apiRequest<void>(
      `/api/modules/${encodeURIComponent(moduleId)}`,
      {
        method: 'DELETE',
      },
    )
  }
}

export const moduleService: ModuleService =
  new HttpModuleService()