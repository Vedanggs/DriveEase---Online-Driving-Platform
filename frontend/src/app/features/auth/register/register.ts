import { Component, inject, signal, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

function strongPassword(control: AbstractControl): ValidationErrors | null {
  const val: string = control.value ?? '';
  const errors: string[] = [];
  if (val.length < 8)                                                          errors.push('8 characters');
  if ((val.match(/[0-9]/g) ?? []).length < 2)                                  errors.push('2 numbers');
  if (!/[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(val))                    errors.push('1 special character');
  return errors.length ? { strongPassword: errors } : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.scss'
})
export class RegisterComponent implements AfterViewInit, OnDestroy {
  @ViewChild('roadCanvas') private readonly canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);

  readonly loading      = signal(false);
  readonly error        = signal<string | null>(null);
  readonly showPassword = signal(false);

  readonly form = this.fb.group({
    fullName:    ['', [Validators.required, Validators.maxLength(200)]],
    email:       ['', [Validators.required, Validators.email, Validators.maxLength(200)]],
    phoneNumber: ['', [Validators.required, Validators.pattern(/^[0-9]{10}$/)]],
    // Native <input type="date"> gives a YYYY-MM-DD string and makes impossible
    // dates (Feb 31, leap-year Feb 29) unselectable — no day/month/year clamp logic needed.
    dateOfBirth: ['', Validators.required],
    password:    ['', [Validators.required, Validators.maxLength(100), strongPassword]]
  });

  private static readonly MinAgeYears = 18;

  // Latest allowed birthday = today minus 18 years, so the calendar can't offer an under-18 date.
  get maxDob(): string {
    const d = new Date();
    d.setFullYear(d.getFullYear() - RegisterComponent.MinAgeYears);
    return d.toISOString().split('T')[0];
  }

  // Oldest sensible birthday — bounds the calendar's lower end.
  readonly minDob = '1940-01-01';

  private rafId: number | null = null;
  private offset = 0;
  private resizeHandler: (() => void) | null = null;

  ngAfterViewInit() {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    this.startRoad();
  }

  ngOnDestroy() {
    if (this.rafId !== null) cancelAnimationFrame(this.rafId);
    if (this.resizeHandler) window.removeEventListener('resize', this.resizeHandler);
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const v = this.form.value;
    // <input type="date"> already yields a YYYY-MM-DD string — exactly the backend format.
    this.auth.register({
      fullName:    v.fullName!,
      email:       v.email!,
      phoneNumber: v.phoneNumber || null,
      dateOfBirth: v.dateOfBirth!,
      password:    v.password!
    }).subscribe({
      next:  () => this.router.navigate(['/login']),
      error: (err) => {
        const detail = err?.error?.detail ?? 'Registration failed. Please try again.';
        this.error.set(detail);
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
    const W = canvas.width;
    const H = canvas.height;
    ctx.clearRect(0, 0, W, H);

    const vx   = W * 0.5;
    const vy   = H * 0.40;
    const topW = W * 0.07;
    const botW = W * 1.35;

    // Road surface
    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy);
    ctx.lineTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW / 2, H);
    ctx.lineTo(vx - botW / 2, H);
    ctx.closePath();
    ctx.fillStyle = '#181818';
    ctx.fill();

    // Shoulder edge lines
    ctx.lineWidth = 1;
    ctx.strokeStyle = 'rgba(255,255,255,0.12)';
    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy);
    ctx.lineTo(vx - botW * 0.88 / 2, H);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW * 0.88 / 2, H);
    ctx.stroke();

    // Centre dashed lane markings
    const N     = 9;
    const FILL  = 0.42;
    const roadH = H - vy;

    for (let i = -1; i <= N + 1; i++) {
      const t0  = (i + this.offset) / N;
      const t1  = t0 + FILL / N;
      const tc0 = Math.max(0, Math.min(1, t0));
      const tc1 = Math.max(0, Math.min(1, t1));
      if (tc1 <= tc0) continue;

      // t^1.6 power compresses dashes near the horizon (perspective feel)
      const y0 = vy + roadH * Math.pow(tc0, 1.6);
      const y1 = vy + roadH * Math.pow(tc1, 1.6);
      if (y0 > H || y1 < vy) continue;

      const frac0 = (y0 - vy) / roadH;
      const frac1 = (y1 - vy) / roadH;
      const rw0   = topW + (botW - topW) * frac0;
      const rw1   = topW + (botW - topW) * frac1;
      const dw0   = rw0 * 0.035;
      const dw1   = rw1 * 0.035;

      ctx.beginPath();
      ctx.moveTo(vx - dw0 / 2, y0);
      ctx.lineTo(vx + dw0 / 2, y0);
      ctx.lineTo(vx + dw1 / 2, y1);
      ctx.lineTo(vx - dw1 / 2, y1);
      ctx.closePath();
      ctx.fillStyle = 'rgba(245, 197, 24, 0.88)';
      ctx.fill();
    }

    // Horizon glow
    const glow = ctx.createRadialGradient(vx, vy, 0, vx, vy, W * 0.52);
    glow.addColorStop(0, 'rgba(245,197,24,0.055)');
    glow.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = glow;
    ctx.fillRect(0, 0, W, H);
  }
}
