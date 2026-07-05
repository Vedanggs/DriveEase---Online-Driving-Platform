import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'schools', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(m => m.LoginComponent)
  },
  {
    path: 'instructor-login',
    loadComponent: () => import('./features/auth/instructor-login/instructor-login').then(m => m.InstructorLoginComponent)
  },
  {
    path: 'instructor-register',
    loadComponent: () => import('./features/auth/instructor-register/instructor-register').then(m => m.InstructorRegisterComponent)
  },
  {
    path: 'instructor/dashboard',
    canActivate: [authGuard],
    data: { role: 'instructor', loginRoute: '/instructor-login' },
    loadComponent: () => import('./features/instructor/dashboard/instructor-dashboard').then(m => m.InstructorDashboardComponent)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then(m => m.RegisterComponent)
  },
  {
    path: 'schools',
    canActivate: [authGuard],
    loadComponent: () => import('./features/schools/schools-list/schools-list').then(m => m.SchoolsListComponent)
  },
  {
    path: 'schools/:id',
    canActivate: [authGuard],
    loadComponent: () => import('./features/schools/school-detail/school-detail').then(m => m.SchoolDetailComponent)
  },
  {
    path: 'schools/:id/enroll',
    canActivate: [authGuard],
    loadComponent: () => import('./features/schools/enroll/enroll').then(m => m.EnrollComponent)
  },
  {
    path: 'my-enrollment',
    canActivate: [authGuard],
    loadComponent: () => import('./features/my-enrollment/my-enrollment').then(m => m.MyEnrollmentComponent)
  },
  {
    path: 'lessons',
    canActivate: [authGuard],
    loadComponent: () => import('./features/lessons/my-lessons/my-lessons').then(m => m.MyLessonsComponent)
  },
  {
    path: 'lessons/book',
    canActivate: [authGuard],
    loadComponent: () => import('./features/lessons/book-lesson/book-lesson').then(m => m.BookLessonComponent)
  },
  { path: '**', redirectTo: 'schools' }
];
