import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path:'customers',
        loadComponent: () => import('./features/customers/customer-list/customer-list').then(m => m.CustomerList)
    },
    {path:'', redirectTo:'customers', pathMatch:'full'}
];
