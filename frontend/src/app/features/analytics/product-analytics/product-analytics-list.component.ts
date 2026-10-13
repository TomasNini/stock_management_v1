import { DatePipe } from '@angular/common'
import { Component, computed, inject, resource, signal } from '@angular/core'
import { SpinnerComponent } from '@shared/components/spinner/spinner.component'
import { ServiceHub } from '@shared/services/service-hub.service'

type Period = '7' | '30' | 'all'

@Component({
  selector: 'app-product-analytics-list',
  templateUrl: './product-analytics-list.component.html',
  styleUrl: './product-analytics-list.component.css',
  imports: [DatePipe, SpinnerComponent],
})
export class ProductAnalyticsListComponent {
  public serviceHub = inject(ServiceHub)
  public period = signal<Period>('30')
  public productId = signal<number | null>(null)
  public movementsPage = signal(1)
  public movementsPageSize = 10

  public products = resource({
    loader: async () => {
      try {
        return await this.serviceHub.http.get<GetProductsItemResponseDTO[]>('products')
      } catch {
        return [] as GetProductsItemResponseDTO[]
      }
    },
  })

  public summary = resource({
    loader: async () => {
      try {
        return await this.serviceHub.http.get<GetSummaryResponseDTO>('analytics/get-summary')
      } catch {
        return null
      }
    },
  })

  public movements = resource({
    params: () => `${this.period()}:${this.productId() ?? 'all'}`,
    loader: async ({ params }) => {
      const [period, productKey] = params.split(':')
      try {
        return await this.serviceHub.http.get<GetMovementAnalyticsResponseDTO>('analytics/get-movement-analytics', {
          period,
          productId: productKey === 'all' ? undefined : Number(productKey),
        })
      } catch {
        return null
      }
    },
  })

  public pagedRecent = computed(() => {
    const items = this.movements.value()?.recent ?? []
    const start = (this.movementsPage() - 1) * this.movementsPageSize
    return items.slice(start, start + this.movementsPageSize)
  })

  public setPeriod(period: Period): void {
    this.movementsPage.set(1)
    this.period.set(period)
  }

  public setProductId(event: Event): void {
    const value = (event.target as HTMLSelectElement).value
    this.movementsPage.set(1)
    this.productId.set(value === '' ? null : Number(value))
  }

  public movementsTotalPages(): number {
    const total = this.movements.value()?.recent.length ?? 0
    return Math.max(1, Math.ceil(total / this.movementsPageSize))
  }

  public goToMovementsPage(page: number): void {
    const next = Math.min(this.movementsTotalPages(), Math.max(1, page))
    this.movementsPage.set(next)
  }

  public stockStatus(product: LowStockProductResponseDTO): string {
    if (product.quantity === 0) {
      return 'Out of stock'
    }

    return 'Low'
  }
}

interface GetProductsItemResponseDTO {
  id: number
  name: string
}

interface GetSummaryResponseDTO {
  totalStockValue: number
  totalUnits: number
  lowStock: LowStockProductResponseDTO[]
}

interface LowStockProductResponseDTO {
  id: number
  name: string
  sku: string
  quantity: number
  minQuantity: number
  unitPrice: number
}

interface GetMovementAnalyticsResponseDTO {
  recent: RecentMovementResponseDTO[]
}

interface RecentMovementResponseDTO {
  createdAt: string
  productName: string
  delta: number
}
