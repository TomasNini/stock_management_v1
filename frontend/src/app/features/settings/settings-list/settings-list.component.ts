import { Component, inject, signal } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { SpinnerComponent } from '@shared/components/spinner/spinner.component'
import { ServiceHub } from '@shared/services/service-hub.service'

@Component({
  selector: 'app-settings-list',
  templateUrl: './settings-list.component.html',
  styleUrl: './settings-list.component.css',
  imports: [FormsModule, SpinnerComponent],
})
export class SettingsListComponent {
  public serviceHub = inject(ServiceHub)
  public saving = signal(false)
  public data = this.emptyForm()

  constructor() {
    this.syncFromSettings()
  }

  public async onSave(): Promise<void> {
    await this.save(this.settingsToSave())
  }

  public async resetColors(): Promise<void> {
    const defaults = this.emptyForm()
    this.data.menuBackground = defaults.menuBackground
    this.data.menuText = defaults.menuText
    this.data.menuActive = defaults.menuActive
    this.data.button = defaults.button
    this.data.text = defaults.text
    this.data.pageBackground = defaults.pageBackground
    await this.save(this.colorSettingsToSave())
  }

  private async save(settings: SaveSettingsRequestDTO[]): Promise<void> {
    this.saving.set(true)
    try {
      await this.serviceHub.http.post('settings/save-settings', settings)
      await this.serviceHub.settings.load()
      this.syncFromSettings()
    } catch {
      this.saving.set(false)
      return
    }

    this.saving.set(false)
  }

  private syncFromSettings(): void {
    const values = this.serviceHub.settings.values()
    this.data = {
      appName: values['app.name'] ?? 'Stock management',
      currency: values['app.currency'] ?? 'EUR',
      menuBackground: values['color.menu-background'] ?? '#1f2937',
      menuText: values['color.menu-text'] ?? '#f9fafb',
      menuActive: values['color.menu-active'] ?? '#2563eb',
      button: values['color.button'] ?? '#2563eb',
      text: values['color.text'] ?? '#111827',
      pageBackground: values['color.page-background'] ?? '#f3f4f6',
    }
  }

  private settingsToSave(): SaveSettingsRequestDTO[] {
    return [
      { setting: 'app.name', value: this.data.appName },
      { setting: 'app.currency', value: this.data.currency },
      ...this.colorSettingsToSave(),
    ]
  }

  private colorSettingsToSave(): SaveSettingsRequestDTO[] {
    return [
      { setting: 'color.menu-background', value: this.data.menuBackground },
      { setting: 'color.menu-text', value: this.data.menuText },
      { setting: 'color.menu-active', value: this.data.menuActive },
      { setting: 'color.button', value: this.data.button },
      { setting: 'color.text', value: this.data.text },
      { setting: 'color.page-background', value: this.data.pageBackground },
    ]
  }

  private emptyForm(): SettingsForm {
    return {
      appName: 'Stock management',
      currency: 'EUR',
      menuBackground: '#1f2937',
      menuText: '#f9fafb',
      menuActive: '#2563eb',
      button: '#2563eb',
      text: '#111827',
      pageBackground: '#f3f4f6',
    }
  }
}

interface SettingsForm {
  appName: string
  currency: string
  menuBackground: string
  menuText: string
  menuActive: string
  button: string
  text: string
  pageBackground: string
}

interface SaveSettingsRequestDTO {
  setting: string
  value: string
}
