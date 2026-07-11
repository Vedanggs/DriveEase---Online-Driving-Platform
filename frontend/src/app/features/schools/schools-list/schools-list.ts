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

  readonly currentPage = signal(1);
  readonly pageSize = 6;

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredSchools().length / this.pageSize))
  );

  readonly paginatedSchools = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredSchools().slice(start, start + this.pageSize);
  });

  ngOnInit() {
    forkJoin({
      schools:    this.schoolService.getAll(),
      enrollment: this.enrollmentService.getMyEnrollment().pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ schools, enrollment }) => {
        const enrolledId = enrollment?.drivingSchoolId;
        this.schools.set(schools.sort((a, b) =>
          ((b.id === enrolledId ? 1 : 0) - (a.id === enrolledId ? 1 : 0)) ||
          a.name.localeCompare(b.name)
        ));
        this.myEnrollment.set(enrollment);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load schools.');
        this.loading.set(false);
      }
    });
  }

  onSearchChange(value: string) {
    this.searchQuery.set(value);
    this.currentPage.set(1);
  }

  nextPage() {
    this.currentPage.update(p => Math.min(p + 1, this.totalPages()));
  }

  prevPage() {
    this.currentPage.update(p => Math.max(p - 1, 1));
  }

  isEnrolledHere(schoolId: string): boolean {
    return this.myEnrollment()?.drivingSchoolId === schoolId;
  }

  isEnrolledElsewhere(schoolId: string): boolean {
    const e = this.myEnrollment();
    return e !== null && e.drivingSchoolId !== schoolId;
  }

  avatarHue(name: string): number {
    let h = 0;
    for (const c of name) h = (h * 31 + c.charCodeAt(0)) & 0xffff;
    return h % 360;
  }

  schoolRating(name: string): string {
    let h = 0;
    for (const c of name) h = (h * 31 + c.charCodeAt(0)) & 0xffff;
    return (4.1 + ((h % 10) * 0.09)).toFixed(1);
  }

  schoolReviewCount(name: string): number {
    let h = 0;
    for (const c of name) h = (h * 31 + c.charCodeAt(0)) & 0xffff;
    return 12 + (h % 50);
  }
}
