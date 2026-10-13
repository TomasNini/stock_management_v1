import { Injectable, signal } from '@angular/core'

export enum ActiveArea {
  Products = 'Products',
  Analytics = 'Analytics',
  Settings = 'Settings',
}

@Injectable({ providedIn: 'root' })
export class AppContextService {
  public readonly activeArea = signal(ActiveArea.Products)
}
