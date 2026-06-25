import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SchoolService } from '../../../core/services/school.service';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { SchoolDetail } from '../../../core/models/school.models';
import { EnrollmentDto } from '../../../core/models/enrollment.models';

@Component({
  selector: 'app-school-detail',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './school-detail.html',
  styleUrl: './school-detail.scss'
})
export class SchoolDetailComponent implements OnInit {
  private readonly route             = inject(ActivatedRoute);
  private readonly schoolService     = inject(SchoolService);
  private readonly enrollmentService = inject(EnrollmentService);

  readonly school       = signal<SchoolDetail | null>(null);
  readonly myEnrollment = signal<EnrollmentDto | null>(null);
  readonly loading      = signal(true);
  readonly error        = signal<string | null>(null);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    forkJoin({
      school:     this.schoolService.getById(id),
      enrollment: this.enrollmentService.getMyEnrollment().pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ school, enrollment }) => {
        this.school.set(school);
        this.myEnrollment.set(enrollment);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('School not found.');
        this.loading.set(false);
      }
    });
  }

  get isEnrolledHere(): boolean {
    return this.myEnrollment()?.drivingSchoolId === this.school()?.id;
  }

  get isEnrolledElsewhere(): boolean {
    const e = this.myEnrollment();
    return e !== null && e.drivingSchoolId !== this.school()?.id;
  }
}
