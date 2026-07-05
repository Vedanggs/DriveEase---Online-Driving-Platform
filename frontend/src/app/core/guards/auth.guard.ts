import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

type AuthRole = 'student' | 'instructor';

export const authGuard: CanActivateFn = route => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const user = auth.user();
  const requiredRole = route.data?.['role'] as AuthRole | undefined;
  const loginRoute = route.data?.['loginRoute'] as string | undefined;

  if (!user) {
    return router.createUrlTree([loginRoute ?? '/login']);
  }

  if (!requiredRole || user.role === requiredRole) {
    return true;
  }

  return router.createUrlTree([requiredRole === 'instructor' ? '/instructor-login' : '/login']);
};
