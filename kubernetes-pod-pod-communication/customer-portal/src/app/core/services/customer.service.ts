import { Injectable, inject } from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Customer } from '../models/customer';
import { PagedResult } from '../models/paged-result';

@Injectable({
  providedIn: 'root',
})
export class CustomerService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/customers`;

  getCustomers(pageNumber: number, pageSize: number, search?: string): Observable<PagedResult<Customer>> {
    let params = new HttpParams()
      .set('page', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if(search) {
      params = params.set('search', search);
    }

    return this.http.get<PagedResult<Customer>>(this.baseUrl, { params });
  }
}
