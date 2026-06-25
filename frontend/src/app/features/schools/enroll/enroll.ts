import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { SchoolService } from '../../../core/services/school.service';
import { AuthService } from '../../../core/services/auth.service';
import { SchoolDetail } from '../../../core/models/school.models';

@Component({
  selector: 'app-enroll',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, DecimalPipe],
  templateUrl: './enroll.html',
  styleUrl: './enroll.scss'
})
export class EnrollComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly enrollmentService = inject(EnrollmentService);
  private readonly schoolService = inject(SchoolService);

  readonly school = signal<SchoolDetail | null>(null);
  readonly loading = signal(false);
  readonly pageLoading = signal(true);
  readonly error = signal<string | null>(null);
  readonly success = signal(false);
  readonly enrollmentId = signal<string | null>(null);
  readonly copied = signal(false);

  readonly FIXED_FEE = 4000;

  readonly form = this.fb.group({});

  private schoolId = '';

  ngOnInit() {
    this.schoolId = this.route.snapshot.paramMap.get('id')!;
    this.schoolService.getById(this.schoolId).subscribe({
      next: s => { this.school.set(s); this.pageLoading.set(false); },
      error: () => { this.pageLoading.set(false); }
    });
  }

  submit() {
    if (this.form.invalid) return;

    this.loading.set(true);
    this.error.set(null);

    const studentId = this.auth.studentId()!;

    this.enrollmentService.enroll({ studentId, drivingSchoolId: this.schoolId, fee: this.FIXED_FEE }).subscribe({
      next: enrollment => {
        localStorage.setItem('de_enrollment_id', enrollment.id);
        localStorage.setItem('de_school_id', this.schoolId);
        this.enrollmentId.set(enrollment.id);
        this.success.set(true);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.detail ?? 'Enrollment failed. Please try again.');
        this.loading.set(false);
      }
    });
  }

  copyId() {
    const id = this.enrollmentId();
    if (!id) return;
    navigator.clipboard.writeText(id).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  goToLessons() {
    this.router.navigate(['/lessons']);
  }

  goToMyEnrollment() {
    this.router.navigate(['/my-enrollment']);
  }
}
