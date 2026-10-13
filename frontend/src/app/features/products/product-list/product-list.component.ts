import { DatePipe, formatDate } from '@angular/common'
import { Component, computed, inject, resource, signal, viewChild } from '@angular/core'
import { ConfirmationDialogComponent } from '@shared/components/confirmation-dialog/confirmation-dialog.component'
import { SpinnerComponent } from '@shared/components/spinner/spinner.component'
import { ServiceHub } from '@shared/services/service-hub.service'
import { ProductCreateDialogComponent } from '../product-create/product-create-dialog.component'
import { ProductEditDialogComponent } from '../product-edit/product-edit-dialog.component'

type SortColumn = 'id' | 'name' | 'sku' | 'quantity' | 'unitPrice' | 'updatedAt'
type SortDirection = 'asc' | 'desc'

const NAME_FILTER_DEBOUNCE_MS = 600

@Component({
  selector: 'app-product-list',
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.css',
  imports: [
    ConfirmationDialogComponent,
    DatePipe,
    ProductCreateDialogComponent,
    SpinnerComponent,
    ProductEditDialogComponent,
  ],
})
export class ProductListComponent {
  public serviceHub = inject(ServiceHub)
  public editing = signal<InlineEdit | null>(null)
  public saving = signal(false)
  public deleting = signal(false)
  public sort = signal<{ column: SortColumn; direction: SortDirection }>({
    column: 'id',
    direction: 'asc',
  })
  public nameFilter = signal('')
  public nameFilterOpen = signal(false)
  public stockFilterOpen = signal(false)
  public minStock = signal('')
  public maxStock = signal('')
  public productToDelete = signal<SearchProductsResponseDTO | null>(null)
  
  public products = resource({
    params: () => ({
      name: this.nameQuery(),
      min: this.stockQuery().min,
      max: this.stockQuery().max,
    }),
    loader: async ({ params }) => {
      try {
        const term = params.name.trim()
        if (params.min != null || params.max != null) {
          const items = await this.getStockLevel(params.min, params.max)
          if (!term) {
            return items
          }

          const match = term.toLowerCase()
          return items.filter((product) => product.name.toLowerCase().includes(match))
        }

        if (!term) {
          return await this.getProducts()
        }

        return await this.searchProducts(term)
      } catch {
        return [] as SearchProductsResponseDTO[]
      }
    },
  })

  public sortedProducts = computed(() => {
    const items = this.products.value() ?? []
    const { column, direction } = this.sort()
    const directionFactor = direction === 'asc' ? 1 : -1
    return [...items].sort((left, right) => compareProducts(left, right, column) * directionFactor)
  })

  private confirmDeleteDialog = viewChild.required(ConfirmationDialogComponent)
  private nameQuery = signal('')
  private stockQuery = signal<{ min: number | null; max: number | null }>({ min: null, max: null })
  private nameFilterTimer: ReturnType<typeof setTimeout> | null = null
  private stockFilterTimer: ReturnType<typeof setTimeout> | null = null

  public toggleNameFilter(): void {
    if (this.nameFilterOpen()) {
      this.nameFilterOpen.set(false)
      this.setNameFilter('')
      return
    }

    this.nameFilterOpen.set(true)
  }

  public onNameFilterInput(event: Event): void {
    this.nameFilter.set((event.target as HTMLInputElement).value)
    this.clearNameFilterTimer()
    this.nameFilterTimer = setTimeout(() => {
      this.applyNameQuery(this.nameFilter())
    }, NAME_FILTER_DEBOUNCE_MS)
  }

  public setNameFilter(name: string): void {
    this.nameFilter.set(name)
    this.applyNameQuery(name)
  }

  private applyNameQuery(name: string): void {
    this.clearNameFilterTimer()
    if (this.nameQuery() === name) {
      return
    }

    this.nameQuery.set(name)
  }

  private clearNameFilterTimer(): void {
    if (this.nameFilterTimer == null) {
      return
    }

    clearTimeout(this.nameFilterTimer)
    this.nameFilterTimer = null
  }

  public toggleStockFilter(): void {
    if (this.stockFilterOpen()) {
      this.stockFilterOpen.set(false)
      this.setStockFilter('', '')
      return
    }

    this.stockFilterOpen.set(true)
  }

  public onMinStockInput(event: Event): void {
    this.minStock.set((event.target as HTMLInputElement).value)
    this.scheduleStockQuery()
  }

  public onMaxStockInput(event: Event): void {
    this.maxStock.set((event.target as HTMLInputElement).value)
    this.scheduleStockQuery()
  }

  public setStockFilter(min: string, max: string): void {
    this.minStock.set(min)
    this.maxStock.set(max)
    this.applyStockQuery(min, max)
  }

  private scheduleStockQuery(): void {
    this.clearStockFilterTimer()
    this.stockFilterTimer = setTimeout(() => {
      this.applyStockQuery(this.minStock(), this.maxStock())
    }, NAME_FILTER_DEBOUNCE_MS)
  }

  private applyStockQuery(min: string, max: string): void {
    this.clearStockFilterTimer()
    const next = { min: parseStockBound(min), max: parseStockBound(max) }
    if (next.min != null && next.max != null && next.min > next.max) {
      return
    }

    const current = this.stockQuery()
    if (current.min === next.min && current.max === next.max) {
      return
    }

    this.stockQuery.set(next)
  }

  private clearStockFilterTimer(): void {
    if (this.stockFilterTimer == null) {
      return
    }

    clearTimeout(this.stockFilterTimer)
    this.stockFilterTimer = null
  }

  private getProducts(): Promise<SearchProductsResponseDTO[]> {
    return this.serviceHub.http.get<SearchProductsResponseDTO[]>('products')
  }

  private searchProducts(name: string): Promise<SearchProductsResponseDTO[]> {
    return this.serviceHub.http.get<SearchProductsResponseDTO[]>('products/search', { name })
  }

  private getStockLevel(min: number | null, max: number | null): Promise<SearchProductsResponseDTO[]> {
    return this.serviceHub.http.get<SearchProductsResponseDTO[]>('products/stock-level', {
      min: min ?? 0,
      max: max ?? undefined,
    })
  }

  public sortBy(column: SortColumn): void {
    this.sort.update((current) => {
      if (current.column === column) {
        return {
          column,
          direction: current.direction === 'asc' ? 'desc' : 'asc',
        }
      }

      return { column, direction: 'asc' }
    })
  }

  public beginInlineEdit(product: SearchProductsResponseDTO): void {
    const current = this.editing()
    if (current?.id === product.id) {
      return
    }
    if (current && this.isInlineDirty()) {
      return
    }

    this.editing.set({
      id: product.id,
      originalQuantity: product.quantity,
      quantity: product.quantity,
    })
  }

  public leaveRow(productId: number): void {
    const current = this.editing()
    if (current?.id !== productId || this.isInlineDirty() || this.saving()) {
      return
    }

    this.editing.set(null)
  }

  public cancelInline(): void {
    if (this.saving()) {
      return
    }

    this.editing.set(null)
  }

  public isEditing(productId: number): boolean {
    return this.editing()?.id === productId
  }

  public isInlineDirty(): boolean {
    const current = this.editing()
    if (!current) {
      return false
    }

    return current.quantity !== current.originalQuantity
  }

  public canSaveInline(): boolean {
    const current = this.editing()
    if (!current || this.saving()) {
      return false
    }

    return this.isInlineDirty() && current.quantity != null && current.quantity >= 0 && Number.isInteger(current.quantity)
  }

  public onQuantityInput(event: Event): void {
    const value = (event.target as HTMLInputElement).valueAsNumber
    this.editing.update((current) => {
      if (!current) {
        return current
      }

      return { ...current, quantity: Number.isNaN(value) ? null : value }
    })
  }

  public async saveInline(): Promise<void> {
    const current = this.editing()
    if (!current || !this.canSaveInline()) {
      return
    }

    this.saving.set(true)
    try {
      await this.persistInlineEdit(current)
    } catch {
      this.saving.set(false)
      return
    }

    this.saving.set(false)
    this.reloadProducts()
  }

  public reloadProducts(): void {
    this.editing.set(null)
    this.products.reload()
  }

  public exportProducts(): void {
    const rows = this.sortedProducts()
    const header = ['Id', 'Name', 'Sku', 'Quantity', `Unit price (${this.serviceHub.settings.currency()})`, 'Last updated']
    const cells = [
      header,
      ...rows.map((product) => [
        String(product.id),
        product.name,
        product.sku,
        String(product.quantity),
        String(product.unitPrice),
        formatDate(product.updatedAt, 'dd/MM/yyyy HH:mm', 'en-GB'),
      ]),
    ]
    const xmlRows = cells
      .map((row) => `<Row>${row.map((value) => `<Cell><Data ss:Type="String">${escapeXml(value)}</Data></Cell>`).join('')}</Row>`)
      .join('')
    const xml = `<?xml version="1.0"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" xmlns:ss="urn:schemas-microsoft-com:office:spreadsheet">
  <Worksheet ss:Name="Products">
    <Table>${xmlRows}</Table>
  </Worksheet>
</Workbook>`
    const blob = new Blob([xml], { type: 'application/vnd.ms-excel' })
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = 'products.xls'
    link.click()
    URL.revokeObjectURL(url)
  }

  public openDeleteConfirm(product: SearchProductsResponseDTO): void {
    this.productToDelete.set(product)
    this.confirmDeleteDialog().openDialog({
      title: 'Delete product',
      message: `Delete ${product.name}?`,
    })
  }

  public async confirmDelete(): Promise<void> {
    const product = this.productToDelete()
    if (!product) {
      return
    }

    this.deleting.set(true)
    try {
      await this.serviceHub.http.delete(`products/${product.id}`)
    } catch {
      this.deleting.set(false)
      return
    }

    this.deleting.set(false)
    this.confirmDeleteDialog().closeDialog()
    this.productToDelete.set(null)
    this.reloadProducts()
  }

  private async persistInlineEdit(current: InlineEdit): Promise<void> {
    if (current.quantity != null && current.quantity !== current.originalQuantity) {
      const delta = current.quantity - current.originalQuantity
      if (delta > 0) {
        await this.serviceHub.http.post(`products/${current.id}/add-to-stock/${delta}`)
      } else {
        await this.serviceHub.http.post(`products/${current.id}/decrement-stock/${-delta}`)
      }
    }
  }
}

function parseStockBound(value: string): number | null {
  const trimmed = value.trim()
  if (trimmed === '') {
    return null
  }

  const parsed = Number(trimmed)
  if (!Number.isInteger(parsed) || parsed < 0) {
    return null
  }

  return parsed
}

function compareProducts(left: SearchProductsResponseDTO, right: SearchProductsResponseDTO, column: SortColumn): number {
  switch (column) {
    case 'name':
      return left.name.localeCompare(right.name)
    case 'sku':
      return left.sku.localeCompare(right.sku)
    case 'quantity':
      return left.quantity - right.quantity
    case 'unitPrice':
      return left.unitPrice - right.unitPrice
    case 'updatedAt':
      return left.updatedAt.localeCompare(right.updatedAt)
    default:
      return left.id - right.id
  }
}

function escapeXml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&apos;')
}

interface InlineEdit {
  id: number
  originalQuantity: number
  quantity: number | null
}

interface SearchProductsResponseDTO {
  id: number
  name: string
  sku: string
  description: string | null
  quantity: number
  minQuantity: number
  unitPrice: number
  createdAt: string
  updatedAt: string
}
