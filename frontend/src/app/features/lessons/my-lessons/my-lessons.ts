import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { LessonService } from '../../../core/services/lesson.service';
import { AuthService } from '../../../core/services/auth.service';
import { LessonDto } from '../../../core/models/lesson.models';

@Component({
  selector: 'app-my-lessons',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './my-lessons.html',
  styleUrl: './my-lessons.scss'
})
export class MyLessonsComponent implements OnInit {
  private readonly lessonService = inject(LessonService);
  private readonly auth = inject(AuthService);

  readonly lessons = signal<LessonDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly hasEnrollment = signal(!!localStorage.getItem('de_enrollment_id'));

  ngOnInit() {
    const studentId = this.auth.studentId();
    if (!studentId) return;

    this.lessonService.getByStudent(studentId).subscribe({
      next: data => { this.lessons.set(data); this.loading.set(false); },
      error: () => { this.error.set('Failed to load lessons.'); this.loading.set(false); }
    });
  }

  formatDuration(duration: string): string {
    if (!duration) return '—';
    const parts = duration.split(':').map(Number);
    const h = parts[0], m = parts[1] ?? 0;
    if (h === 0) return `${m} min`;
    if (m === 0) return `${h} hr`;
    return `${h} hr ${m} min`;
  }

  statusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'scheduled': return 'status-scheduled';
      case 'completed': return 'status-completed';
      case 'cancelled': return 'status-cancelled';
      default: return '';
    }
  }

  ordinal(n: number): string {
    const s = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return n + (s[(v - 20) % 10] ?? s[v] ?? s[0]);
  }
}
