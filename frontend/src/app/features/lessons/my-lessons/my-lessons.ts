import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, switchMap, map } from 'rxjs/operators';
import { LessonService } from '../../../core/services/lesson.service';
import { AuthService } from '../../../core/services/auth.service';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { SchoolService } from '../../../core/services/school.service';
import { LessonDto } from '../../../core/models/lesson.models';
import { StatusBadgeComponent } from '../../../shared/status-badge/status-badge';
import { toUtcDate } from '../../../core/utils/date.utils';

interface HistoryGroup {
  enrollmentId: string;
  schoolName: string;
  enrolledAt: string;
  lessons: LessonDto[];
}

@Component({
  selector: 'app-my-lessons',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent],
  templateUrl: './my-lessons.html',
  styleUrl: './my-lessons.scss'
})
export class MyLessonsComponent implements OnInit {
  private readonly lessonService    = inject(LessonService);
  private readonly auth             = inject(AuthService);
  private readonly enrollmentService = inject(EnrollmentService);
  private readonly schoolService    = inject(SchoolService);

  readonly lessons           = signal<LessonDto[]>([]);
  readonly activeEnrollmentId = signal<string | null>(null);
  readonly schoolNameMap     = signal<Map<string, string>>(new Map());
  readonly enrolledAtMap     = signal<Map<string, string>>(new Map());
  readonly loading           = signal(true);
  readonly error             = signal<string | null>(null);
  readonly activeTab         = signal<'current' | 'history'>('current');
  readonly cancellingId      = signal<string | null>(null);
  readonly cancelError       = signal<string | null>(null);
  readonly confirmingLessonId = signal<string | null>(null);
  readonly statusFilter      = signal<'all' | 'scheduled' | 'completed' | 'cancelled'>('all');

  // Lessons for the active enrollment only, sorted ascending by date
  readonly currentLessons = computed(() => {
    const id = this.activeEnrollmentId();
    if (!id) return [];
    return [...this.lessons().filter(l => l.enrollmentId === id)]
      .sort((a, b) => toUtcDate(a.scheduledAt).getTime() - toUtcDate(b.scheduledAt).getTime());
  });

  readonly statusCounts = computed(() => {
    const lessons = this.currentLessons();
    return {
      all:       lessons.length,
      scheduled: lessons.filter(l => l.status.toLowerCase() === 'scheduled').length,
      completed: lessons.filter(l => l.status.toLowerCase() === 'completed').length,
      cancelled: lessons.filter(l => l.status.toLowerCase() === 'cancelled').length
    };
  });

  readonly filteredCurrentLessons = computed(() => {
    const filter = this.statusFilter();
    if (filter === 'all') return this.currentLessons();
    return this.currentLessons().filter(l => l.status.toLowerCase() === filter);
  });

  // Past lessons grouped by completed enrollment, most recent first
  readonly historyGroups = computed<HistoryGroup[]>(() => {
    const activeId = this.activeEnrollmentId();
    const past = this.lessons().filter(l => l.enrollmentId !== activeId);
    const map = new Map<string, LessonDto[]>();
    for (const l of past) {
      if (!map.has(l.enrollmentId)) map.set(l.enrollmentId, []);
      map.get(l.enrollmentId)!.push(l);
    }
    return Array.from(map.entries())
      .map(([enrollmentId, lessons]) => ({
        enrollmentId,
        schoolName: this.schoolNameMap().get(enrollmentId) ?? 'Previous School',
        enrolledAt: this.enrolledAtMap().get(enrollmentId) ?? '',
        lessons: [...lessons].sort((a, b) => toUtcDate(a.scheduledAt).getTime() - toUtcDate(b.scheduledAt).getTime())
      }))
      .sort((a, b) => toUtcDate(b.enrolledAt).getTime() - toUtcDate(a.enrolledAt).getTime());
  });

  readonly hasActiveEnrollment = computed(() => !!this.activeEnrollmentId());

  ngOnInit() {
    const studentId = this.auth.studentId();
    if (!studentId) return;

    forkJoin({
      lessons:          this.lessonService.getByStudent(studentId),
      activeEnrollment: this.enrollmentService.getMyEnrollment().pipe(catchError(() => of(null))),
      schools:          this.schoolService.getAll().pipe(catchError(() => of([])))
    }).pipe(
      switchMap(({ lessons, activeEnrollment, schools }) => {
        const activeId = activeEnrollment?.id ?? null;
        const oldIds = [...new Set(lessons.filter(l => l.enrollmentId !== activeId).map(l => l.enrollmentId))];

        if (oldIds.length === 0) {
          return of({ lessons, activeId, schools, oldEnrollments: [] as any[] });
        }

        return forkJoin(
          oldIds.map(id => this.enrollmentService.getById(id).pipe(catchError(() => of(null))))
        ).pipe(
          map(oldEnrollments => ({ lessons, activeId, schools, oldEnrollments: oldEnrollments.filter(Boolean) }))
        );
      })
    ).pipe(
      switchMap(({ lessons, activeId, schools, oldEnrollments }) => {
        // Collect all unique school IDs so we can resolve missing instructor names
        const schoolIds = new Set<string>();
        if (activeId) {
          const activeSid = localStorage.getItem('de_school_id');
          if (activeSid) schoolIds.add(activeSid);
        }
        for (const e of oldEnrollments) schoolIds.add(e.drivingSchoolId);

        const hasGaps = lessons.some(l => !l.instructorName && l.instructorId);
        if (!hasGaps || schoolIds.size === 0) {
          return of({ lessons, activeId, schools, oldEnrollments, instructorMap: new Map<string, string>() });
        }

        return forkJoin(
          [...schoolIds].map(sid =>
            this.schoolService.getInstructors(sid).pipe(catchError(() => of([])))
          )
        ).pipe(
          map(results => {
            const instructorMap = new Map<string, string>();
            results.flat().forEach(i => instructorMap.set(i.id, i.fullName));
            return { lessons, activeId, schools, oldEnrollments, instructorMap };
          })
        );
      })
    ).subscribe({
      next: ({ lessons, activeId, schools, oldEnrollments, instructorMap }) => {
        const nameMap = new Map<string, string>();
        const dateMap = new Map<string, string>();
        for (const e of oldEnrollments) {
          const school = schools.find((s: any) => s.id === e.drivingSchoolId);
          nameMap.set(e.id, school?.name ?? 'Previous School');
          dateMap.set(e.id, e.enrolledAt);
        }
        // Patch instructor names for lessons booked before the InstructorName feature
        const patched = lessons.map(l =>
          (!l.instructorName && l.instructorId && instructorMap.has(l.instructorId))
            ? { ...l, instructorName: instructorMap.get(l.instructorId)! }
            : l
        );
        this.lessons.set(patched);
        this.activeEnrollmentId.set(activeId);
        this.schoolNameMap.set(nameMap);
        this.enrolledAtMap.set(dateMap);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load lessons.');
        this.loading.set(false);
      }
    });
  }

  requestCancel(lessonId: string) {
    if (this.cancellingId()) return;
    this.confirmingLessonId.set(lessonId);
  }

  dismissCancelConfirm() {
    this.confirmingLessonId.set(null);
  }

  confirmCancel() {
    const lessonId = this.confirmingLessonId();
    if (!lessonId) return;
    this.confirmingLessonId.set(null);
    this.cancellingId.set(lessonId);
    this.cancelError.set(null);
    this.lessonService.cancel(lessonId).subscribe({
      next: () => {
        this.lessons.update(list =>
          list.map(l => l.id === lessonId ? { ...l, status: 'Cancelled' } : l));
        this.cancellingId.set(null);
      },
      error: (err) => {
        this.cancelError.set(err?.error?.detail ?? 'Failed to cancel the lesson. Please try again.');
        this.cancellingId.set(null);
      }
    });
  }

  setStatusFilter(filter: 'all' | 'scheduled' | 'completed' | 'cancelled') {
    this.statusFilter.set(filter);
  }

  formatTimeRange(lesson: LessonDto): string {
    const start = toUtcDate(lesson.scheduledAt);
    const parts = lesson.duration.split(':').map(Number);
    const end = new Date(start.getTime() + (parts[0] * 60 + (parts[1] ?? 0)) * 60000);
    const fmt = (d: Date) => d.toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
    return `${fmt(start)} – ${fmt(end)}`;
  }

  formatDuration(duration: string): string {
    if (!duration) return '—';
    const parts = duration.split(':').map(Number);
    const h = parts[0], m = parts[1] ?? 0;
    if (h === 0) return `${m} min`;
    if (m === 0) return `${h} hr`;
    return `${h} hr ${m} min`;
  }

  ordinal(n: number): string {
    const s = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return n + (s[(v - 20) % 10] ?? s[v] ?? s[0]);
  }

  formatTime(iso: string): string {
    return toUtcDate(iso).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  formatDay(iso: string): string {
    return toUtcDate(iso).toLocaleDateString('en-IN', { day: 'numeric' });
  }

  formatMonth(iso: string): string {
    return toUtcDate(iso).toLocaleDateString('en-IN', { month: 'short' });
  }

  isOverdue(lesson: LessonDto): boolean {
    if (lesson.status.toLowerCase() !== 'scheduled') return false;
    return toUtcDate(lesson.scheduledAt).getTime() < Date.now();
  }

  formatEnrolledAt(iso: string): string {
    if (!iso) return '';
    return toUtcDate(iso).toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  completedCount(group: HistoryGroup): number {
    return group.lessons.filter(l => l.status.toLowerCase() === 'completed').length;
  }

  isGroupFullyCompleted(group: HistoryGroup): boolean {
    return group.lessons.length > 0 && group.lessons.every(l => l.status.toLowerCase() === 'completed');
  }
}
