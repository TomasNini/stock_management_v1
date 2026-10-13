import { Routes } from '@angular/router'
import { ProductAnalyticsListComponent } from './features/analytics/product-analytics/product-analytics-list.component'
import { ProductListComponent } from './features/products/product-list/product-list.component'
import { SettingsListComponent } from './features/settings/settings-list/settings-list.component'
import { ShellComponent } from './shell/shell.component'

export const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'products' },
      { path: 'products', component: ProductListComponent },
      { path: 'analytics', component: ProductAnalyticsListComponent },
      { path: 'settings', component: SettingsListComponent },
      { path: '**', redirectTo: 'products' },
    ],
  },
]
