import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';

export const errorInterceptor : HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
        
        if(error.status === 0){
            console.error('Network error occurred');
        }
        else if(error.status === 401){
            console.error('Unauthorized access');
        }
        else{
            console.error(`Backend returned code ${error.status}`);
        }
        return throwError(() => new Error('An error occurred; please try again later.'));
    })
);
};