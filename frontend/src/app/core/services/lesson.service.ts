import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BookLessonRequest, LessonDto } from '../models/lesson.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LessonService {
  private readonly http = inject(HttpClient);

  getByStudent(studentId: string) {
    return this.http.get<LessonDto[]>(
      `${environment.apiUrl}/api/v1/lessons/student/${studentId}`
    );
  }

  book(request: BookLessonRequest) {
    return this.http.post<{ id: string }>(`${environment.apiUrl}/api/v1/lessons`, request);
  }

  cancel(lessonId: string) {
    return this.http.post<void>(`${environment.apiUrl}/api/v1/lessons/${lessonId}/cancel`, {});
  }
}
