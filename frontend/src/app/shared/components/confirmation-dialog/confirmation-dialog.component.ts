import { Component, ElementRef, input, output, signal, viewChild } from '@angular/core'
import { SpinnerComponent } from '@shared/components/spinner/spinner.component'

@Component({
  selector: 'app-confirmation-dialog',
  templateUrl: './confirmation-dialog.component.html',
  styleUrl: './confirmation-dialog.component.css',
  imports: [SpinnerComponent],
})
export class ConfirmationDialogComponent {
  public busy = input(false)
  public confirmed = output<void>()
  public title = signal('Confirm')
  public message = signal('')
  public confirmLabel = signal('Delete')

  private dialogRef = viewChild.required<ElementRef<HTMLDialogElement>>('dialogRef')

  public openDialog(options: { title: string; message: string; confirmLabel?: string }): void {
    this.title.set(options.title)
    this.message.set(options.message)
    this.confirmLabel.set(options.confirmLabel ?? 'Delete')

    const dialog = this.dialogRef().nativeElement
    if (!dialog.open) {
      dialog.showModal()
    }
  }

  public closeDialog(): void {
    this.dialogRef().nativeElement.close()
  }

  public onKeydownEscape(event: Event): void {
    if (this.busy()) {
      event.preventDefault()
      event.stopPropagation()
    }
  }

  public onConfirm(): void {
    this.confirmed.emit()
  }
}
