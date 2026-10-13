import { Component, ElementRef, inject, output, signal, viewChild } from '@angular/core'
import { FormsModule, NgForm } from '@angular/forms'
import { SpinnerComponent } from '@shared/components/spinner/spinner.component'
import { ServiceHub } from '@shared/services/service-hub.service'

@Component({
  selector: 'app-product-edit-dialog',
  templateUrl: './product-edit-dialog.component.html',
  styleUrl: './product-edit-dialog.component.css',
  imports: [FormsModule, SpinnerComponent],
})
export class ProductEditDialogComponent {
  public serviceHub = inject(ServiceHub)
  public saved = output<void>()
  public submitted = signal(false)
  public saving = signal(false)
  public deleting = signal(false)
  public data = new EditProductRequestDTO()

  private dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogRef')
  private form = viewChild.required(NgForm)

  public async openDialog(id: number): Promise<void> {
    const dialog = this.dialogRef().nativeElement
    if (!dialog.open) {
      dialog.showModal()
    }

    this.submitted.set(false)
    this.saving.set(false)
    const product = await this.serviceHub.http.get<GetProductResponseDTO>(`products/${id}`)
    this.data = new EditProductRequestDTO(product)
    this.form().resetForm(this.data)
  }

  public closeDialog(): void {
    if (this.saving()) {
      return
    }

    this.dialogRef().nativeElement.close()
  }

  public onKeydownEscape(event: Event): void {
    if (this.saving()) {
      event.preventDefault()
      event.stopPropagation()
    }
  }

  public async onSave(): Promise<void> {
    this.submitted.set(true)
    if (this.form().invalid) {
      return
    }

    this.saving.set(true)
    try {
      await this.serviceHub.http.put(`products/${this.data.id}`, this.data)
    } catch {
      this.saving.set(false)
      return
    }

    this.saving.set(false)
    this.closeDialog()
    this.saved.emit()
  }

  public async onDelete(): Promise<void> {
    this.deleting.set(true)
    try {
      await this.serviceHub.http.delete(`products/${this.data.id}`)
    } catch {
      this.deleting.set(false)
      return
    }
    this.deleting.set(false)
    this.closeDialog()
    this.saved.emit()
  }
}

class EditProductRequestDTO {
  id: number
  name: string
  sku: string
  description: string | null
  quantity: number | null
  minQuantity: number
  unitPrice: number | null

  constructor(product?: GetProductResponseDTO) {
    this.id = product?.id ?? 0
    this.name = product?.name ?? ''
    this.sku = product?.sku ?? ''
    this.description = product?.description ?? null
    this.quantity = product?.quantity ?? null
    this.minQuantity = product?.minQuantity ?? 0
    this.unitPrice = product?.unitPrice ?? null
  }
}

interface GetProductResponseDTO {
  id: number
  name: string
  sku: string
  description: string | null
  quantity: number
  minQuantity: number
  unitPrice: number
}
