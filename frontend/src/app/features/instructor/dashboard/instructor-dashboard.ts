import { Component, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { InstructorService } from '../../../core/services/instructor.service';
import { AuthService } from '../../../core/services/auth.service';
import { InstructorNotificationDto } from '../../../core/models/notification.models';
import { InstructorLessonDto } from '../../../core/models/lesson.models';
import { toUtcDate } from '../../../core/utils/date.utils';

interface InstructorSession {
  instructorId: string;
  name: string;
  schoolName: string;
}

interface FeedbackGroup {
  enrollmentId: string;
  studentId: string;
  studentName: string;
  lessons: InstructorLessonDto[];
}

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './instructor-dashboard.html',
  styleUrl: './instructor-dashboard.scss'
})
export class InstructorDashboardComponent implements OnInit, OnDestroy {
  private readonly instructorService: InstructorService;
  private readonly authService = inject(AuthService);
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private readonly localReadIds = new Set<string>();

  readonly instructorName = signal('');
  readonly schoolName     = signal('');

  readonly notifications  = signal<InstructorNotificationDto[]>([]);
  readonly lessons        = signal<InstructorLessonDto[]>([]);

  readonly activeTab = signal<'overview' | 'upcoming' | 'notifications' | 'history'>('overview');

  readonly feedbackLessonId = signal<string | null>(null);
  feedbackText = '';
  readonly submitting = signal(false);

  static readonly MaxFeedbackLength = 100;

  // Feedback must be real words: non-empty, within the length cap, and containing
  // at least one letter — so "12345" or "@#$%" alone can't be submitted.
  get feedbackValid(): boolean {
    const t = this.feedbackText.trim();
    return t.length > 0
        && t.length <= InstructorDashboardComponent.MaxFeedbackLength
        && /[a-zA-Z]/.test(t);
  }

  // A short reason to show the instructor when the feedback isn't valid yet.
  get feedbackError(): string | null {
    const t = this.feedbackText.trim();
    if (t.length === 0) return null;                       // empty → no error, just disabled
    if (t.length > InstructorDashboardComponent.MaxFeedbackLength) return 'Feedback must be 100 characters or fewer.';
    if (!/[a-zA-Z]/.test(t)) return 'Feedback must include words, not just numbers or symbols.';
    return null;
  }

  readonly maxFeedbackLength = InstructorDashboardComponent.MaxFeedbackLength;

  readonly isAvailable          = signal(true);
  readonly togglingAvailability = signal(false);

  readonly unreadCount      = computed(() => this.notifications().filter(n => !n.isRead).length);
  readonly upcomingLessons  = computed(() => this.lessons().filter(l => l.status === 'Scheduled'));
  readonly completedLessons = computed(() => this.lessons().filter(l => l.status === 'Completed'));

  readonly feedbackGroups = computed<FeedbackGroup[]>(() => {
    // Group by enrollment, not student — a student who re-enrolls starts a fresh
    // 5-lesson package, so each enrollment must be its own card. Grouping by student
    // would merge both packages and show a misleading "6 / 5".
    const map = new Map<string, FeedbackGroup>();
    for (const l of this.completedLessons()) {
      if (!map.has(l.enrollmentId)) {
        map.set(l.enrollmentId, {
          enrollmentId: l.enrollmentId,
          studentId: l.studentId,
          studentName: l.studentName || 'Student',
          lessons: []
        });
      }
      map.get(l.enrollmentId)!.lessons.push(l);
    }
    for (const g of map.values()) {
      g.lessons.sort((a, b) => toUtcDate(a.scheduledAt).getTime() - toUtcDate(b.scheduledAt).getTime());
    }
    return Array.from(map.values());
  });

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
    const user = this.authService.user();
    if (!user || user.role !== 'instructor') { window.location.href = '/instructor-login'; return; }

    this.instructorName.set(user.fullName ?? 'Instructor');
    this.schoolName.set(user.schoolName ?? 'Your School');

    this.loadNotifications();
    this.loadLessons();
    this.loadMyProfile();

    // Poll for new notifications every 30 seconds
    this.pollTimer = setInterval(() => this.loadNotifications(), 30_000);
  }

  ngOnDestroy() {
    if (this.pollTimer !== null) clearInterval(this.pollTimer);
  }

  private loadNotifications() {
    const user = this.authService.user();
    if (!user || !user.instructorId) return;
    this.instructorService.getNotifications(user.instructorId).subscribe({
      next: data => this.notifications.set(
        data.map(n => ({ ...n, isRead: n.isRead || this.localReadIds.has(n.id) }))
      ),
      error: () => {}
    });
  }

  private loadLessons() {
    const user = this.authService.user();
    if (!user || !user.instructorId) return;
    this.instructorService.getUpcomingLessons(user.instructorId).subscribe({
      next: data => this.lessons.set(data),
      error: () => this.lessons.set([])
    });
  }

  private loadMyProfile() {
    this.instructorService.getMyProfile().subscribe({
      next: profile => this.isAvailable.set(profile.isAvailable),
      error: () => {}
    });
  }

  toggleAvailability() {
    if (this.togglingAvailability()) return;
    const next = !this.isAvailable();
    this.isAvailable.set(next);
    this.togglingAvailability.set(true);
    this.instructorService.setAvailability(next).subscribe({
      next: () => this.togglingAvailability.set(false),
      error: () => {
        this.isAvailable.set(!next);
        this.togglingAvailability.set(false);
      }
    });
  }

  markRead(id: string) {
    if (this.localReadIds.has(id)) return;
    this.localReadIds.add(id);
    this.notifications.update(list =>
      list.map(n => n.id === id ? { ...n, isRead: true } : n)
    );
    this.instructorService.markNotificationRead(id).subscribe({ error: () => {} });
  }

  formatScheduledAt(iso: string | null): string {
    if (!iso) return '';
    return toUtcDate(iso).toLocaleString('en-IN', {
      weekday: 'short', day: '2-digit', month: 'short',
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }

  formatDuration(duration: string): string {
    const parts = duration.split(':');
    const h = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10);
    if (h === 0) return `${m} min`;
    if (m === 0) return `${h} hr`;
    return `${h} hr ${m} min`;
  }

  formatNotifTime(iso: string): string {
    const d = toUtcDate(iso);
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffMin = Math.floor(diffMs / 60_000);
    if (diffMin < 1) return 'Just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    const diffHrs = Math.floor(diffMin / 60);
    if (diffHrs < 24) return `Today at ${d.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true })}`;
    const yesterday = new Date(now);
    yesterday.setDate(yesterday.getDate() - 1);
    if (d.toDateString() === yesterday.toDateString())
      return `Yesterday at ${d.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true })}`;
    return d.toLocaleString('en-IN', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit', hour12: true });
  }

  /** Returns how many lessons (scheduled or completed) exist for a given enrollmentId */
  lessonCountForEnrollment(enrollmentId: string): number {
    return this.lessons().filter(l => l.enrollmentId === enrollmentId && l.status !== 'Cancelled').length;
  }

  /** Returns how many lessons are completed for a given enrollmentId */
  completedCountForEnrollment(enrollmentId: string): number {
    return this.lessons().filter(l => l.enrollmentId === enrollmentId && l.status === 'Completed').length;
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
    if (!this.feedbackValid) return;
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

  get todayDate(): string {
    return new Date().toLocaleDateString('en-IN', {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric'
    });
  }

  avatarInitial(name: string): string {
    return name.charAt(0).toUpperCase();
  }

  logout() {
    this.authService.logout();
    window.location.href = '/instructor-login';
  }
}
