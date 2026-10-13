import { Injectable, Signal, signal } from '@angular/core'
import { ActiveArea } from '@shell/context/app-context.service'

export interface MenuItem {
  id: string
  label: string
  path: string
  area: ActiveArea
  visible: Signal<boolean>
  disabled: Signal<boolean>
  pinBottom?: boolean
}

@Injectable({ providedIn: 'root' })
export class MenuService {
  private readonly itemsSignal = signal<MenuItem[]>([])
  public readonly items = this.itemsSignal.asReadonly()

  public addMenuItem(item: MenuItem): void {
    this.itemsSignal.update((items) => {
      if (items.some((existing) => existing.id === item.id)) {
        return items
      }
      return [...items, item]
    })
  }
}
