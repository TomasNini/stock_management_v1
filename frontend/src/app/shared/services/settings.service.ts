import { computed, inject, Injectable, signal } from '@angular/core'
import { ApiHttpService } from '@shared/api/http.service'

const THEME_KEYS: Record<string, string> = {
  'color.menu-background': '--color-menu-background',
  'color.menu-text': '--color-menu-text',
  'color.menu-active': '--color-menu-active',
  'color.button': '--color-button',
  'color.text': '--color-text',
  'color.page-background': '--color-page-background',
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  private readonly http = inject(ApiHttpService)
  public readonly values = signal<Record<string, string>>({})
  public readonly appName = computed(() => this.values()['app.name'] ?? 'Stock management')
  public readonly currency = computed(() => this.values()['app.currency'] ?? 'EUR')

  public async load(): Promise<void> {
    const items = await this.http.get<GetSettingsResponseDTO[]>('settings/get-settings')
    const map: Record<string, string> = {}
    for (const item of items) {
      map[item.setting] = item.value
    }

    this.values.set(map)
    this.apply(map)
  }

  public apply(map: Record<string, string>): void {
    const root = document.documentElement
    for (const [setting, cssVar] of Object.entries(THEME_KEYS)) {
      const value = map[setting]
      if (value) {
        root.style.setProperty(cssVar, value)
      }
    }

    document.title = map['app.name'] ?? 'Stock management'
  }
}

interface GetSettingsResponseDTO {
  setting: string
  value: string
}
