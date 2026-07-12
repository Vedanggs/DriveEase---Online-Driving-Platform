import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import jsPDF from 'jspdf';
import { EnrollmentService } from '../../core/services/enrollment.service';
import { SchoolService } from '../../core/services/school.service';
import { LessonService } from '../../core/services/lesson.service';
import { AuthService } from '../../core/services/auth.service';
import { EnrollmentDto } from '../../core/models/enrollment.models';
import { SchoolDetail } from '../../core/models/school.models';
import { LessonDto } from '../../core/models/lesson.models';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge';

@Component({
  selector: 'app-my-enrollment',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent],
  templateUrl: './my-enrollment.html',
  styleUrl: './my-enrollment.scss'
})
export class MyEnrollmentComponent implements OnInit {
  private readonly enrollmentService = inject(EnrollmentService);
  private readonly schoolService     = inject(SchoolService);
  private readonly lessonService     = inject(LessonService);
  private readonly auth              = inject(AuthService);

  readonly enrollment       = signal<EnrollmentDto | null>(null);
  readonly school           = signal<SchoolDetail | null>(null);
  readonly loading          = signal(true);
  readonly copied           = signal(false);
  readonly completedLessons = signal(0);
  readonly totalLessons     = 5;
  readonly lessonDots       = [1, 2, 3, 4, 5];

  ngOnInit() {
    this.enrollmentService.getMyEnrollment().pipe(
      catchError(() => of(null)),
      switchMap(enrollment => {
        if (!enrollment) return of({ enrollment: null, school: null, lessons: [] as LessonDto[] });
        const studentId = this.auth.studentId();
        return forkJoin({
          school:  this.schoolService.getById(enrollment.drivingSchoolId).pipe(catchError(() => of(null))),
          lessons: studentId
            ? this.lessonService.getByStudent(studentId).pipe(catchError(() => of([] as LessonDto[])))
            : of([] as LessonDto[])
        }).pipe(map(({ school, lessons }) => ({ enrollment, school, lessons })));
      })
    ).subscribe(({ enrollment, school, lessons }) => {
      this.enrollment.set(enrollment);
      this.school.set(school);
      const completed = lessons.filter(l =>
        l.status === 'Completed' && l.enrollmentId === enrollment?.id
      ).length;
      this.completedLessons.set(completed);
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

  downloadPdf() {
    const e = this.enrollment();
    if (!e) return;
    const school = this.school();

    const doc = new jsPDF();
    let y = 20;

    doc.setFontSize(18);
    doc.text('DriveEase — Enrollment Summary', 14, y);
    y += 12;

    doc.setFontSize(11);
    const line = (label: string, value: string) => {
      doc.setFont('helvetica', 'bold');
      doc.text(`${label}:`, 14, y);
      doc.setFont('helvetica', 'normal');
      doc.text(value, 60, y);
      y += 8;
    };

    line('School', school?.name ?? '—');
    line('Address', school?.address ?? '—');
    line('Enrollment ID', e.id);
    line('Status', e.status);
    line('Fee Paid', `Rs. ${e.fee.toLocaleString('en-IN')}`);
    line('Payment Status', e.paymentStatus);
    line('Enrolled On', this.formatDate(e.enrolledAt));
    line('Payment Confirmed', this.formatDate(e.paymentConfirmedAt));

    doc.save(`DriveEase-Enrollment-${e.id.slice(0, 8)}.pdf`);
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
