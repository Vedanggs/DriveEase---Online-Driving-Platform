import { Component, OnInit, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { InstructorService } from '../../../core/services/instructor.service';
import { InstructorNotificationDto } from '../../../core/models/notification.models';
import { InstructorLessonDto } from '../../../core/models/lesson.models';

interface InstructorSession {
  instructorId: string;
  name: string;
  schoolName: string;
}

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './instructor-dashboard.html',
  styleUrl: './instructor-dashboard.scss'
})
export class InstructorDashboardComponent implements OnInit {
  private readonly instructorService: InstructorService;
  private session: InstructorSession | null = null;

  readonly instructorName = signal('');
  readonly schoolName     = signal('');

  readonly notifications  = signal<InstructorNotificationDto[]>([]);
  readonly lessons        = signal<InstructorLessonDto[]>([]);

  readonly completingId   = signal<string | null>(null);
  readonly feedbackLessonId = signal<string | null>(null);
  feedbackText = '';
  readonly submitting     = signal(false);

  readonly unreadCount   = computed(() => this.notifications().filter(n => !n.isRead).length);
  readonly upcomingLessons  = computed(() => this.lessons().filter(l => l.status === 'Scheduled'));
  readonly completedLessons = computed(() => this.lessons().filter(l => l.status === 'Completed'));

  readonly greeting = computed(() => {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  });

  constructor(instructorService: InstructorService) {
    this.instructorService = instructorService;
  }

  ngOnInit() {
    const raw = sessionStorage.getItem('instructor_session');
    if (!raw) { window.location.href = '/instructor-login'; return; }

    this.session = JSON.parse(raw) as InstructorSession;
    this.instructorName.set(this.session.name ?? 'Instructor');
    this.schoolName.set(this.session.schoolName ?? 'Your School');

    this.loadNotifications();
    this.loadLessons();
  }

  private loadNotifications() {
    if (!this.session) return;
    this.instructorService.getNotifications(this.session.instructorId).subscribe({
      next: data => this.notifications.set(data),
      error: () => this.notifications.set([])
    });
  }

  private loadLessons() {
    if (!this.session) return;
    this.instructorService.getUpcomingLessons(this.session.instructorId).subscribe({
      next: data => this.lessons.set(data),
      error: () => this.lessons.set([])
    });
  }

  markRead(id: string) {
    this.notifications.update(list =>
      list.map(n => n.id === id ? { ...n, isRead: true } : n)
    );
  }

  formatScheduledAt(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleString('en-IN', { weekday: 'short', day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
  }

  formatDuration(duration: string): string {
    const parts = duration.split(':');
    const h = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10);
    if (h === 0) return `${m} min`;
    if (m === 0) return `${h} hr`;
    return `${h} hr ${m} min`;
  }

  markComplete(lesson: InstructorLessonDto) {
    this.completingId.set(lesson.id);
    this.instructorService.completeLesson(lesson.id).subscribe({
      next: () => {
        this.lessons.update(list =>
          list.map(l => l.id === lesson.id ? { ...l, status: 'Completed', completedAt: new Date().toISOString() } : l)
        );
        this.completingId.set(null);
      },
      error: () => this.completingId.set(null)
    });
  }

  openFeedback(lessonId: string) {
    this.feedbackLessonId.set(lessonId);
    this.feedbackText = '';
  }

  cancelFeedback() {
    this.feedbackLessonId.set(null);
    this.feedbackText = '';
  }

  submitFeedback(lesson: InstructorLessonDto) {
    if (!this.feedbackText.trim()) return;
    this.submitting.set(true);
    const notes = this.feedbackText.trim();
    this.instructorService.completeLesson(lesson.id, notes).subscribe({
      next: () => {
        this.lessons.update(list =>
          list.map(l => l.id === lesson.id
            ? { ...l, status: 'Completed', notes, completedAt: new Date().toISOString() }
            : l)
        );
        this.feedbackLessonId.set(null);
        this.feedbackText = '';
        this.submitting.set(false);
      },
      error: () => this.submitting.set(false)
    });
  }

  logout() {
    sessionStorage.removeItem('instructor_session');
    window.location.href = '/instructor-login';
  }
}
