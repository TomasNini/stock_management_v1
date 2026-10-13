import { inject, Injectable } from '@angular/core'
import { ApiHttpService } from '@shared/api/http.service'
import { SettingsService } from '@shared/services/settings.service'
import { AppContextService } from '@shell/context/app-context.service'
import { MenuService } from '@shell/menu/menu.service'

@Injectable({ providedIn: 'root' })
export class ServiceHub {
  public readonly http = inject(ApiHttpService)
  public readonly context = inject(AppContextService)
  public readonly menu = inject(MenuService)
  public readonly settings = inject(SettingsService)
}
