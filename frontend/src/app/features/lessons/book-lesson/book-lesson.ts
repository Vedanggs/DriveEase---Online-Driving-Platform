import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { LessonService } from '../../../core/services/lesson.service';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

interface InstructorOption {
  id: string;
  fullName: string;
  licenseNumber: string;
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

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal(false);
  readonly instructors = signal<InstructorOption[]>([]);
  readonly selectedDate = signal<string>('');

  readonly enrollmentId = localStorage.getItem('de_enrollment_id') ?? '';
  readonly schoolId     = localStorage.getItem('de_school_id') ?? '';
  readonly hasEnrollment = !!this.enrollmentId;

  readonly form = this.fb.group({
    instructorId: ['', Validators.required],
    date: ['', Validators.required],
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
    if (!date || date !== this.localDateStr()) return this.allTimeSlots;
    const currentHour = new Date().getHours();
    return this.allTimeSlots.filter(slot => parseInt(slot, 10) > currentHour);
  });

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
    d.setFullYear(d.getFullYear() + 1);
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }

  ngOnInit() {
    this.form.controls.date.valueChanges.subscribe(date => {
      this.selectedDate.set(date ?? '');
      const current = this.form.controls.timeSlot.value;
      if (current && !this.availableTimeSlots().includes(current)) {
        this.form.controls.timeSlot.setValue('');
      }
    });

    if (!this.schoolId) return;
    this.http.get<InstructorOption[]>(
      `${environment.apiUrl}/api/v1/schools/${this.schoolId}/instructors`
    ).subscribe({
      next: list => this.instructors.set(list),
      error: () => this.instructors.set([])
    });
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const v = this.form.value;
    const scheduledAt = `${v.date!}T${v.timeSlot!}:00`;

    this.lessonService.book({
      enrollmentId: this.enrollmentId,
      studentId: this.auth.studentId()!,
      studentName: this.auth.fullName() ?? 'Student',
      instructorId: v.instructorId!,
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
