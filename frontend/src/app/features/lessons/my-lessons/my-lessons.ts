import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, switchMap, map } from 'rxjs/operators';
import { LessonService } from '../../../core/services/lesson.service';
import { AuthService } from '../../../core/services/auth.service';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { SchoolService } from '../../../core/services/school.service';
import { LessonDto } from '../../../core/models/lesson.models';

interface HistoryGroup {
  enrollmentId: string;
  schoolName: string;
  enrolledAt: string;
  lessons: LessonDto[];
}

@Component({
  selector: 'app-my-lessons',
  standalone: true,
  imports: [RouterLink],
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

  // Lessons for the active enrollment only, sorted ascending by date
  readonly currentLessons = computed(() => {
    const id = this.activeEnrollmentId();
    if (!id) return [];
    return [...this.lessons().filter(l => l.enrollmentId === id)]
      .sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime());
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
        lessons: [...lessons].sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime())
      }))
      .sort((a, b) => new Date(b.enrolledAt).getTime() - new Date(a.enrolledAt).getTime());
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
      case 'cancelled':  return 'status-cancelled';
      default: return '';
    }
  }

  ordinal(n: number): string {
    const s = ['th', 'st', 'nd', 'rd'];
    const v = n % 100;
    return n + (s[(v - 20) % 10] ?? s[v] ?? s[0]);
  }

  // Bookings are stored as true UTC (see book-lesson.ts) but the API returns them
  // without a trailing "Z", so treat any string missing an explicit UTC/offset
  // marker as UTC and convert to local — same convention as the instructor dashboard.
  private toUtcIso(iso: string): string {
    return iso.endsWith('Z') || iso.includes('+') ? iso : iso + 'Z';
  }

  formatTime(iso: string): string {
    return new Date(this.toUtcIso(iso)).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
  }

  formatDay(iso: string): string {
    return new Date(this.toUtcIso(iso)).toLocaleDateString('en-IN', { day: 'numeric' });
  }

  formatMonth(iso: string): string {
    return new Date(this.toUtcIso(iso)).toLocaleDateString('en-IN', { month: 'short' });
  }

  isOverdue(lesson: LessonDto): boolean {
    if (lesson.status.toLowerCase() !== 'scheduled') return false;
    return new Date(this.toUtcIso(lesson.scheduledAt)).getTime() < Date.now();
  }

  formatEnrolledAt(iso: string): string {
    if (!iso) return '';
    const utc = iso.endsWith('Z') || iso.includes('+') ? iso : iso + 'Z';
    return new Date(utc).toLocaleDateString('en-IN', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  completedCount(group: HistoryGroup): number {
    return group.lessons.filter(l => l.status.toLowerCase() === 'completed').length;
  }

  isGroupFullyCompleted(group: HistoryGroup): boolean {
    return group.lessons.length > 0 && group.lessons.every(l => l.status.toLowerCase() === 'completed');
  }
}
