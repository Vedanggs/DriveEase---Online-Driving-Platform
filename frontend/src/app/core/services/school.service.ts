import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SchoolDetail, SchoolSummary } from '../models/school.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SchoolService {
  private readonly http = inject(HttpClient);

  getAll() {
    return this.http.get<SchoolSummary[]>(`${environment.apiUrl}/api/v1/schools`);
  }

  getById(id: string) {
    return this.http.get<SchoolDetail>(`${environment.apiUrl}/api/v1/schools/${id}`);
  }

  getInstructors(schoolId: string) {
    return this.http.get<{ id: string; fullName: string; licenseNumber: string }[]>(
      `${environment.apiUrl}/api/v1/schools/${schoolId}/instructors`
    );
  }
}
