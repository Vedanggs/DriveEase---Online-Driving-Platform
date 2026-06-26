import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { SchoolService } from '../../core/services/school.service';
import { EnrollmentDto } from '../../core/models/enrollment.models';
import { SchoolDetail } from '../../core/models/school.models';

@Component({
  selector: 'app-my-enrollment',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './my-enrollment.html',
  styleUrl: './my-enrollment.scss'
})
export class MyEnrollmentComponent implements OnInit {
  private readonly enrollmentService = inject(EnrollmentService);
  private readonly schoolService     = inject(SchoolService);

  readonly enrollment = signal<EnrollmentDto | null>(null);
  readonly school     = signal<SchoolDetail | null>(null);
  readonly loading    = signal(true);
  readonly copied     = signal(false);

  ngOnInit() {
    this.enrollmentService.getMyEnrollment().pipe(
      catchError(() => of(null)),
      switchMap(enrollment => {
        if (!enrollment) return of({ enrollment: null, school: null });
        return this.schoolService.getById(enrollment.drivingSchoolId).pipe(
          catchError(() => of(null)),
          map(school => ({ enrollment, school }))
        );
      })
    ).subscribe(({ enrollment, school }) => {
      this.enrollment.set(enrollment);
      this.school.set(school);
      this.loading.set(false);
      if (enrollment) {
        localStorage.setItem('de_school_id', enrollment.drivingSchoolId);
        localStorage.setItem('de_enrollment_id', enrollment.id);
      }
    });
  }

  copyId() {
    const id = this.enrollment()?.id;
    if (!id) return;
    navigator.clipboard.writeText(id).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    // Backend returns UTC datetimes without 'Z'; appending it tells JS to treat as UTC
    // so toLocaleString correctly converts to the browser's local timezone (e.g. IST)
    const utc = dateStr.endsWith('Z') || dateStr.includes('+') ? dateStr : dateStr + 'Z';
    return new Date(utc).toLocaleString('en-IN', {
      day: 'numeric', month: 'long', year: 'numeric',
      hour: '2-digit', minute: '2-digit', hour12: true
    });
  }
}
