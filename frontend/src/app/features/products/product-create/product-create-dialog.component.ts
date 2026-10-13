import { Component, ElementRef, inject, output, signal, viewChild } from '@angular/core'
import { FormsModule, NgForm } from '@angular/forms'
import { SpinnerComponent } from '@shared/components/spinner/spinner.component'
import { ServiceHub } from '@shared/services/service-hub.service'

@Component({
  selector: 'app-product-create-dialog',
  templateUrl: './product-create-dialog.component.html',
  styleUrl: './product-create-dialog.component.css',
  imports: [FormsModule, SpinnerComponent],
})
export class ProductCreateDialogComponent {
  public serviceHub = inject(ServiceHub)
  public saved = output<void>()
  public submitted = signal(false)
  public saving = signal(false)
  public data = new CreateProductRequestDTO()

  private dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogRef')
  private form = viewChild.required(NgForm)

  public openDialog(): void {
    const dialog = this.dialogRef().nativeElement
    if (!dialog.open) {
      dialog.showModal()
    }

    this.submitted.set(false)
    this.saving.set(false)
    this.data = new CreateProductRequestDTO()
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
      await this.serviceHub.http.post<CreateProductResponseDTO>('products', this.data)
    } catch {
      this.saving.set(false)
      return
    }

    this.saving.set(false)
    this.closeDialog()
    this.saved.emit()
  }
}

class CreateProductRequestDTO {
  name: string
  sku: string
  description: string | null
  quantity: number | null
  minQuantity: number
  unitPrice: number | null

  constructor() {
    this.name = ''
    this.sku = ''
    this.description = null
    this.quantity = null
    this.minQuantity = 0
    this.unitPrice = null
  }
}

interface CreateProductResponseDTO {
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
