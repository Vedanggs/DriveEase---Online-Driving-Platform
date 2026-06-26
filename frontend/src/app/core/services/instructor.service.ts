import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { InstructorNotificationDto } from '../models/notification.models';
import { InstructorLessonDto } from '../models/lesson.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class InstructorService {
  private readonly http = inject(HttpClient);

  getNotifications(instructorId: string) {
    return this.http.get<InstructorNotificationDto[]>(
      `${environment.apiUrl}/api/v1/notifications/instructor/${instructorId}`
    );
  }

  getUpcomingLessons(instructorId: string) {
    return this.http.get<InstructorLessonDto[]>(
      `${environment.apiUrl}/api/v1/lessons/instructor/${instructorId}`
    );
  }

  completeLesson(lessonId: string, notes?: string) {
    return this.http.post(
      `${environment.apiUrl}/api/v1/lessons/${lessonId}/complete`,
      notes ? { notes } : {}
    );
  }

  markNotificationRead(notificationId: string) {
    return this.http.patch<void>(
      `${environment.apiUrl}/api/v1/notifications/${notificationId}/read`,
      {}
    );
  }
}
