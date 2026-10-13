import { Component, computed, inject, OnInit } from '@angular/core'
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router'
import { ServiceHub } from '@shared/services/service-hub.service'
import { ActiveArea } from '@shell/context/app-context.service'

@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.css',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
})
export class ShellComponent implements OnInit {
  public serviceHub = inject(ServiceHub)

  public async ngOnInit(): Promise<void> {
    this.serviceHub.menu.addMenuItem({
      id: 'menu-item-products',
      label: 'Products',
      path: '/products',
      area: ActiveArea.Products,
      visible: computed(() => true),
      disabled: computed(() => false),
    })
    this.serviceHub.menu.addMenuItem({
      id: 'menu-item-analytics',
      label: 'Analytics',
      path: '/analytics',
      area: ActiveArea.Analytics,
      visible: computed(() => true),
      disabled: computed(() => false),
    })
    this.serviceHub.menu.addMenuItem({
      id: 'menu-item-settings',
      label: 'Settings',
      path: '/settings',
      area: ActiveArea.Settings,
      pinBottom: true,
      visible: computed(() => true),
      disabled: computed(() => false),
    })

    try {
      await this.serviceHub.settings.load()
    } catch {
      return
    }
  }
}
