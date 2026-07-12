import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { LessonService } from '../../../core/services/lesson.service';
import { AuthService } from '../../../core/services/auth.service';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { environment } from '../../../../environments/environment';

interface InstructorOption {
  id: string;
  fullName: string;
  licenseNumber: string;
}

const MAX_LESSONS = 5;
const MAX_PENDING_LESSONS = 2;
const BOOKING_WINDOW_DAYS = 10;

// Native <input type="date"> has no way to grey out individual days of the
// week — min/max only bound the whole range. Sundays are blocked here instead,
// surfaced as a field error like any other validation failure.
function notSundayValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  const [year, month, day] = control.value.split('-').map(Number);
  const isSunday = new Date(year, month - 1, day).getDay() === 0;
  return isSunday ? { sunday: true } : null;
}

@Component({
  selector: 'app-book-lesson',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './book-lesson.html',
  styleUrl: './book-lesson.scss'
})
export class BookLessonComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly lessonService = inject(LessonService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly enrollmentService = inject(EnrollmentService);

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal(false);
  readonly instructors = signal<InstructorOption[]>([]);
  readonly selectedDate = signal<string>('');
  readonly bookedSlots = signal<{ scheduledAt: string; duration: string }[]>([]);

  readonly bookedCount = signal(0);
  readonly scheduledCount = signal(0);
  readonly remainingLessons = computed(() => Math.max(0, MAX_LESSONS - this.bookedCount()));
  readonly atLimit = computed(() => this.remainingLessons() === 0);
  readonly atPendingLimit = computed(() => this.scheduledCount() >= MAX_PENDING_LESSONS);
  readonly maxLessons = MAX_LESSONS;
  readonly maxPendingLessons = MAX_PENDING_LESSONS;

  // Resolved from API on init; not from potentially stale localStorage
  private _enrollmentId = '';
  private _schoolId = '';
  readonly enrollmentIdLoaded = signal(false);

  get enrollmentId() { return this._enrollmentId; }
  get schoolId()     { return this._schoolId; }
  get hasEnrollment() { return !!this._enrollmentId; }

  readonly form = this.fb.group({
    instructorId: ['', Validators.required],
    date: ['', [Validators.required, notSundayValidator]],
    timeSlot: ['', Validators.required],
    durationHours: ['01:00:00', Validators.required]
  });

  private readonly allTimeSlots = [
    '09:00', '10:00', '11:00', '12:00', '13:00',
    '14:00', '15:00', '16:00', '17:00', '18:00',
    '19:00', '20:00', '21:00'
  ];

  readonly availableTimeSlots = computed(() => {
    const date = this.selectedDate();
    let slots = this.allTimeSlots;
    if (date && date === this.localDateStr()) {
      const now = new Date()
      let currentHour = now.getHours();
      if(now.getMinutes() > 35) currentHour ++; // Round up to the next hour if minutes > 35
      slots = slots.filter(slot => parseInt(slot, 10) > currentHour);
    }
    return slots;
  });

  isSlotTaken(slot: string): boolean {
    const date = this.selectedDate();
    if (!date) return false;
    const durationVal = this.form.value.durationHours ?? '01:00:00';
    const durationHours = parseInt(durationVal.split(':')[0], 10);
    const newStart = new Date(`${date}T${slot}:00`).getTime();
    const newEnd = newStart + durationHours * 3600_000;
    return this.bookedSlots().some(b => {
      const bStart = new Date(b.scheduledAt.endsWith('Z') ? b.scheduledAt : b.scheduledAt + 'Z').getTime();
      const bDurHours = parseInt(b.duration.split(':')[0], 10);
      const bEnd = bStart + bDurHours * 3600_000;
      return newStart < bEnd && newEnd > bStart;
    });
  }

  readonly durationOptions = [
    { value: '01:00:00', label: '1 hour' },
    { value: '02:00:00', label: '2 hours' }
  ];

  private localDateStr(): string {
    const d = new Date();
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  get minDate(): string {
    return this.localDateStr();
  }

  get maxDate(): string {
    const d = new Date();
    d.setDate(d.getDate() + BOOKING_WINDOW_DAYS);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  private loadBookedSlots() {
    const instructorId = this.form.value.instructorId;
    const date = this.form.value.date;
    if (!instructorId || !date) { this.bookedSlots.set([]); return; }
    this.http.get<{ scheduledAt: string; duration: string }[]>(
      `${environment.apiUrl}/api/v1/lessons/instructor/${instructorId}/booked-slots?date=${date}`
    ).subscribe({
      next: slots => {
        this.bookedSlots.set(slots);
        const current = this.form.controls.timeSlot.value;
        if (current && this.isSlotTaken(current)) this.form.controls.timeSlot.setValue('');
      },
      error: () => this.bookedSlots.set([])
    });
  }

  ngOnInit() {
    this.form.controls.date.valueChanges.subscribe(date => {
      this.selectedDate.set(date ?? '');
      const current = this.form.controls.timeSlot.value;
      if (current && !this.availableTimeSlots().includes(current)) {
        this.form.controls.timeSlot.setValue('');
      }
      this.loadBookedSlots();
    });

    this.form.controls.instructorId.valueChanges.subscribe(() => this.loadBookedSlots());
    this.form.controls.durationHours.valueChanges.subscribe(() => {
      const current = this.form.controls.timeSlot.value;
      if (current && this.isSlotTaken(current)) this.form.controls.timeSlot.setValue('');
    });

    // Always load enrollment fresh from the API so we never book against a stale/old enrollment
    this.enrollmentService.getMyEnrollment().subscribe({
      next: enrollment => {
        this.error.set(null);
        if (!enrollment) { this.enrollmentIdLoaded.set(true); return; }
        this._enrollmentId = enrollment.id;
        this._schoolId = enrollment.drivingSchoolId;
        localStorage.setItem('de_enrollment_id', enrollment.id);
        localStorage.setItem('de_school_id', enrollment.drivingSchoolId);

        // Load instructors in parallel — doesn't block form display
        this.http.get<InstructorOption[]>(
          `${environment.apiUrl}/api/v1/schools/${this._schoolId}/instructors`
        ).subscribe({
          next: list => this.instructors.set(list),
          error: () => this.instructors.set([])
        });

        // Fetch authoritative count BEFORE showing the form to eliminate the race condition
        // where the user clicks "Book lesson" while bookedCount is still 0 (its initial value)
        // but the backend already sees 5 lessons.
        this.http.get<{ count: number; scheduledCount: number }>(
          `${environment.apiUrl}/api/v1/lessons/enrollment/${this._enrollmentId}/count`
        ).subscribe({
          next: res => {
            this.bookedCount.set(res.count);
            this.scheduledCount.set(res.scheduledCount ?? 0);
            this.enrollmentIdLoaded.set(true);
          },
          error: () => this.enrollmentIdLoaded.set(true)
        });
      },
      error: () => this.enrollmentIdLoaded.set(true)
    });
  }

  submit() {
    if (this.atLimit() || this.atPendingLimit()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const v = this.form.value;
    // Build as a real Date (parsed in the browser's local timezone per the
    // ECMAScript date-time string format), then convert to a true UTC ISO
    // string. Sending the naive "YYYY-MM-DDTHH:mm:00" string with no offset
    // stored an ambiguous wall-clock value that display code later
    // mis-interpreted as UTC, shifting every rendered time by the local
    // timezone offset (e.g. a 4 PM booking showing as 9:30 PM for IST).
    const scheduledAt = new Date(`${v.date!}T${v.timeSlot!}:00`).toISOString();

    const instructorName = this.instructors().find(i => i.id === v.instructorId!)?.fullName ?? '';

    this.lessonService.book({
      enrollmentId: this.enrollmentId,
      studentId: this.auth.studentId()!,
      studentName: this.auth.fullName() ?? 'Student',
      instructorId: v.instructorId!,
      instructorName,
      scheduledAt,
      duration: v.durationHours!
    }).subscribe({
      next: () => {
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/lessons']), 2000);
      },
      error: (err) => {
        this.error.set(err?.error?.detail ?? 'Booking failed. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
