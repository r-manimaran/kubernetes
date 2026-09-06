import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { CustomerService } from '../../../core/services/customer.service';
import { Customer } from '../../../core/models/customer';
import { PagedResult } from '../../../core/models/paged-result';
import { CustomerDetailDialog } from '../customer-detail-dialog/customer-detail-dialog';

@Component({
  selector: 'app-customer-list',
  imports: [
    DatePipe,
    MatTableModule, MatPaginatorModule, MatSortModule,
    MatProgressSpinnerModule, MatCardModule, MatIconModule,
    MatButtonModule
  ],
  templateUrl: './customer-list.html',
  styleUrl: './customer-list.scss',
})
export class CustomerList implements OnInit {

  private customerService = inject(CustomerService);
  private dialog = inject(MatDialog);

  displayedColumns: string[] = ['id', 'name', 'email', 'phone', 'city', 'state', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<Customer>([]);

  loading = signal(false);
  error = signal<string | null>(null);

  pageNumber = signal(1);
  pageSize = signal(10);
  totalCount = signal(0);

  totalPages = computed(() => Math.ceil(this.totalCount() / this.pageSize()) || 1);


  ngOnInit(): void {    
    this.loadCustomers();
  }

  loadCustomers():void {
    this.loading.set(true);
    this.error.set(null);

    this.customerService.getCustomers(this.pageNumber(), this.pageSize())
      .subscribe({
        next: (result: PagedResult<Customer>) => {
          this.dataSource.data = result.data;
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load customers. Please try again later.');
          this.loading.set(false);
        }
      });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;      
    }
    this.pageNumber.set(page);
    this.loadCustomers();
  }
  
  onPageChange(event: PageEvent): void{
    this.pageNumber.set(event.pageIndex + 1);
    this.pageSize.set(event.pageSize);
    this.loadCustomers();
  }
  openDetail(customer: Customer): void {
    this.dialog.open(CustomerDetailDialog, {
      width: '480px',
      data: customer
    });
  }

  // nextPage():void { this.goToPage(this.pageNumber() + 1);  }
  // prevPage():void { this.goToPage(this.pageNumber() - 1);  }
}
