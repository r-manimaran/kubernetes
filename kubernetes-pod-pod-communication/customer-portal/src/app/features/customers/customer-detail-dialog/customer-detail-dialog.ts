import { Component, inject } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Customer } from '../../../core/models/customer';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-customer-detail-dialog',
  imports: [DatePipe, MatDialogModule, MatButtonModule, MatDividerModule],
  templateUrl: './customer-detail-dialog.html',
  styleUrl: './customer-detail-dialog.scss',
})
export class CustomerDetailDialog {
  dialogRef = inject(MatDialogRef<CustomerDetailDialog>);
  customer: Customer = inject(MAT_DIALOG_DATA);
}
