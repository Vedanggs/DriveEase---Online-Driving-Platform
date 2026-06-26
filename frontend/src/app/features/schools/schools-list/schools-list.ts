import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { SchoolService } from '../../../core/services/school.service';
import { EnrollmentService } from '../../../core/services/enrollment.service';
import { SchoolSummary } from '../../../core/models/school.models';
import { EnrollmentDto } from '../../../core/models/enrollment.models';

@Component({
  selector: 'app-schools-list',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './schools-list.html',
  styleUrl: './schools-list.scss'
})
export class SchoolsListComponent implements OnInit {
  private readonly schoolService      = inject(SchoolService);
  private readonly enrollmentService  = inject(EnrollmentService);

  readonly schools      = signal<SchoolSummary[]>([]);
  readonly myEnrollment = signal<EnrollmentDto | null>(null);
  readonly loading      = signal(true);
  readonly error        = signal<string | null>(null);
  readonly searchQuery  = signal('');

  readonly enrolledSchoolName = computed(() => {
    const e = this.myEnrollment();
    if (!e) return '';
    return this.schools().find(s => s.id === e.drivingSchoolId)?.name ?? 'your school';
  });

  readonly filteredSchools = computed(() => {
    const q = this.searchQuery().trim().toLowerCase();
    if (!q) return this.schools();
    return this.schools().filter(s => s.name.toLowerCase().startsWith(q));
  });

  ngOnInit() {
    forkJoin({
      schools:    this.schoolService.getAll(),
      enrollment: this.enrollmentService.getMyEnrollment().pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ schools, enrollment }) => {
        this.schools.set(schools);
        this.myEnrollment.set(enrollment);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load schools.');
        this.loading.set(false);
      }
    });
  }

  isEnrolledHere(schoolId: string): boolean {
    return this.myEnrollment()?.drivingSchoolId === schoolId;
  }

  isEnrolledElsewhere(schoolId: string): boolean {
    const e = this.myEnrollment();
    return e !== null && e.drivingSchoolId !== schoolId;
  }
}
