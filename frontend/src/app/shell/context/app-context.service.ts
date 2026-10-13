import { Injectable, signal } from '@angular/core'

export enum ActiveArea {
  None = 'None',
  Products = 'Products',
  Analytics = 'Analytics',
  Settings = 'Settings',
}

@Injectable({ providedIn: 'root' })
export class AppContextService {
  public readonly activeArea = signal(ActiveArea.Products)
}
