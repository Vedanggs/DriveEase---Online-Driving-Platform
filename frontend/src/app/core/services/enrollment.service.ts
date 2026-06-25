import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { EnrollRequest, EnrollmentDto } from '../models/enrollment.models';
import { environment } from '../../../environments/environment';
import { switchMap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class EnrollmentService {
  private readonly http = inject(HttpClient);

  enroll(request: EnrollRequest) {
    return this.http
      .post<{ id: string }>(`${environment.apiUrl}/api/v1/enrollments`, request)
      .pipe(
        switchMap(res =>
          this.processPayment(res.id).pipe(
            switchMap(() => this.getById(res.id))
          )
        )
      );
  }

  getMyEnrollment() {
    return this.http.get<EnrollmentDto | null>(`${environment.apiUrl}/api/v1/enrollments/me`);
  }

  getById(id: string) {
    return this.http.get<EnrollmentDto>(`${environment.apiUrl}/api/v1/enrollments/${id}`);
  }

  processPayment(enrollmentId: string) {
    return this.http.post<{ success: boolean }>(
      `${environment.apiUrl}/api/v1/enrollments/${enrollmentId}/payment`,
      {}
    );
  }
}
