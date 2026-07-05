import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { AuthResponse, InstructorAuthResponse, LoginRequest, RegisterRequest } from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _user = signal<AuthResponse | null>(this.loadFromStorage());

  readonly user = this._user.asReadonly();
  readonly isLoggedIn = computed(() => this._user() !== null);
  readonly studentId = computed(() => this._user()?.studentId ?? null);
  readonly fullName = computed(() => this._user()?.fullName ?? null);

  login(request: LoginRequest) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/v1/auth/login`, request)
      .pipe(tap(res => this.persist({ ...res, role: 'student' })));
  }

  instructorLogin(request: { email: string; password: string }) {
    return this.http
      .post<InstructorAuthResponse>(`${environment.apiUrl}/api/v1/auth/instructor-login`, request)
      .pipe(tap(res => this.persist({ ...res, role: 'instructor' })));
  }

  register(request: RegisterRequest) {
    return this.http.post<{ id: string }>(`${environment.apiUrl}/api/v1/auth/register`, request);
  }

  logout() {
    this._user.set(null);
    localStorage.removeItem('de_auth');
  }

  getToken(): string | null {
    return this._user()?.accessToken ?? null;
  }

  private persist(res: AuthResponse) {
    this._user.set(res);
    localStorage.setItem('de_auth', JSON.stringify(res));
  }

  private loadFromStorage(): AuthResponse | null {
    try {
      const raw = localStorage.getItem('de_auth');
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }
}
