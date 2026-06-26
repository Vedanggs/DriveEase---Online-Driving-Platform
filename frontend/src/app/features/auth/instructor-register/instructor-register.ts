import { Component, inject, signal, ViewChild, ElementRef, AfterViewInit, OnDestroy, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

// Format: MH-12AB4566  (state 2-alpha)(dash)(2-digit RTO)(2-alpha)(4-digit serial)
const LICENSE_RE = /^[A-Z]{2}-\d{2}[A-Z]{2}\d{4}$/;
function licenseFormat(control: AbstractControl): ValidationErrors | null {
  const val: string = (control.value ?? '').toUpperCase();
  return LICENSE_RE.test(val) ? null : { licenseFormat: true };
}

function strongPassword(control: AbstractControl): ValidationErrors | null {
  const val: string = control.value ?? '';
  const errors: string[] = [];
  if (val.length < 8)                                                        errors.push('8 characters');
  if ((val.match(/[0-9]/g) ?? []).length < 2)                                errors.push('2 numbers');
  if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(val))                  errors.push('1 special character');
  return errors.length ? { strongPassword: errors } : null;
}

interface SchoolOption {
  id: string;
  name: string;
}

@Component({
  selector: 'app-instructor-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './instructor-register.html',
  styleUrl: './instructor-register.scss'
})
export class InstructorRegisterComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('roadCanvas') private readonly canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly fb     = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly http   = inject(HttpClient);

  readonly loading      = signal(false);
  readonly error        = signal<string | null>(null);
  readonly schools      = signal<SchoolOption[]>([]);
  readonly showPassword = signal(false);

  readonly form = this.fb.group({
    fullName:      ['', [Validators.required, Validators.minLength(2)]],
    email:         ['', [Validators.required, Validators.email]],
    licenseNumber: ['', [Validators.required, licenseFormat]],
    schoolId:      ['', Validators.required],
    password:      ['', [Validators.required, Validators.maxLength(100), strongPassword]]
  });

  private rafId: number | null = null;
  private offset = 0;
  private resizeHandler: (() => void) | null = null;

  ngOnInit() {
    this.http.get<SchoolOption[]>(`${environment.apiUrl}/api/v1/schools`).subscribe({
      next: list => this.schools.set(list),
      error: ()   => this.error.set('Could not load schools. Please refresh.')
    });
  }

  ngAfterViewInit() {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    this.startRoad();
  }

  ngOnDestroy() {
    if (this.rafId !== null) cancelAnimationFrame(this.rafId);
    if (this.resizeHandler) window.removeEventListener('resize', this.resizeHandler);
  }

  onLicenseInput(event: Event) {
    const input = event.target as HTMLInputElement;
    const upper = input.value.toUpperCase();
    if (input.value !== upper) {
      const pos = input.selectionStart ?? upper.length;
      input.value = upper;
      input.setSelectionRange(pos, pos);
    }
    this.form.controls.licenseNumber.setValue(upper, { emitEvent: false });
    this.form.controls.licenseNumber.updateValueAndValidity();
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const { fullName, email, licenseNumber, schoolId, password } = this.form.value;
    const school = this.schools().find(s => s.id === schoolId);

    // Guard: email must not already be registered.
    const existing: Record<string, unknown> = JSON.parse(localStorage.getItem('instructor_profiles') ?? '{}');
    if (existing[email!]) {
      this.error.set('An account with this email already exists. Please sign in.');
      this.loading.set(false);
      return;
    }

    this.http.post<{ id: string }>(
      `${environment.apiUrl}/api/v1/schools/${schoolId}/instructors`,
      { fullName, licenseNumber }
    ).subscribe({
      next: ({ id }) => {
        const session = {
          instructorId: id,
          email,
          name: fullName,
          schoolName: school?.name ?? 'Your School',
          schoolId
        };
        sessionStorage.setItem('instructor_session', JSON.stringify(session));
        const profiles: Record<string, { passwordHash: string } & typeof session> =
          JSON.parse(localStorage.getItem('instructor_profiles') ?? '{}');
        profiles[email!] = { ...session, passwordHash: btoa(password!) };
        localStorage.setItem('instructor_profiles', JSON.stringify(profiles));
        this.loading.set(false);
        this.router.navigate(['/instructor/dashboard']);
      },
      error: () => {
        this.error.set('Registration failed. Please try again.');
        this.loading.set(false);
      }
    });
  }

  private startRoad() {
    const canvas = this.canvasRef.nativeElement;
    const ctx    = canvas.getContext('2d')!;

    const resize = () => {
      canvas.width  = canvas.offsetWidth;
      canvas.height = canvas.offsetHeight;
    };
    resize();
    this.resizeHandler = resize;
    window.addEventListener('resize', resize);

    const loop = () => {
      this.offset += 0.014;
      if (this.offset >= 1) this.offset -= 1;
      this.drawRoad(ctx, canvas);
      this.rafId = requestAnimationFrame(loop);
    };
    this.rafId = requestAnimationFrame(loop);
  }

  private drawRoad(ctx: CanvasRenderingContext2D, canvas: HTMLCanvasElement) {
    const W = canvas.width, H = canvas.height;
    ctx.clearRect(0, 0, W, H);

    const vx = W * 0.5, vy = H * 0.40;
    const topW = W * 0.07, botW = W * 1.35;

    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy); ctx.lineTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW / 2, H);  ctx.lineTo(vx - botW / 2, H);
    ctx.closePath();
    ctx.fillStyle = '#0f1a0f'; ctx.fill();

    ctx.lineWidth = 1; ctx.strokeStyle = 'rgba(255,255,255,0.10)';
    ctx.beginPath(); ctx.moveTo(vx - topW / 2, vy); ctx.lineTo(vx - botW * 0.88 / 2, H); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(vx + topW / 2, vy); ctx.lineTo(vx + botW * 0.88 / 2, H); ctx.stroke();

    const N = 9, FILL = 0.42, roadH = H - vy;
    for (let i = -1; i <= N + 1; i++) {
      const t0 = (i + this.offset) / N, t1 = t0 + FILL / N;
      const tc0 = Math.max(0, Math.min(1, t0)), tc1 = Math.max(0, Math.min(1, t1));
      if (tc1 <= tc0) continue;
      const y0 = vy + roadH * Math.pow(tc0, 1.6), y1 = vy + roadH * Math.pow(tc1, 1.6);
      if (y0 > H || y1 < vy) continue;
      const frac0 = (y0 - vy) / roadH, frac1 = (y1 - vy) / roadH;
      const rw0 = topW + (botW - topW) * frac0, rw1 = topW + (botW - topW) * frac1;
      ctx.beginPath();
      ctx.moveTo(vx - rw0 * 0.035 / 2, y0); ctx.lineTo(vx + rw0 * 0.035 / 2, y0);
      ctx.lineTo(vx + rw1 * 0.035 / 2, y1); ctx.lineTo(vx - rw1 * 0.035 / 2, y1);
      ctx.closePath();
      ctx.fillStyle = 'rgba(52, 211, 153, 0.90)'; ctx.fill();
    }

    const glow = ctx.createRadialGradient(vx, vy, 0, vx, vy, W * 0.52);
    glow.addColorStop(0, 'rgba(52,211,153,0.07)'); glow.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = glow; ctx.fillRect(0, 0, W, H);
  }
}
