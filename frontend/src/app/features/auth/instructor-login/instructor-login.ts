import { Component, inject, signal, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-instructor-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './instructor-login.html',
  styleUrl: './instructor-login.scss'
})
export class InstructorLoginComponent implements AfterViewInit, OnDestroy {
  @ViewChild('roadCanvas') private readonly canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly fb     = inject(FormBuilder);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);

  readonly form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

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

    const { email } = this.form.value;

    // Happy path stub — real instructor auth wired up in Day 30
    setTimeout(() => {
      sessionStorage.setItem('instructor_session', JSON.stringify({
        email,
        name: email!.split('@')[0],
        schoolName: 'Pune Road Masters'
      }));
      this.router.navigate(['/instructor/dashboard']);
    }, 800);
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

    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy);
    ctx.lineTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW / 2, H);
    ctx.lineTo(vx - botW / 2, H);
    ctx.closePath();
    ctx.fillStyle = '#0f1a0f';
    ctx.fill();

    ctx.lineWidth = 1;
    ctx.strokeStyle = 'rgba(255,255,255,0.10)';
    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy);
    ctx.lineTo(vx - botW * 0.88 / 2, H);
    ctx.stroke();
    ctx.beginPath();
    ctx.moveTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW * 0.88 / 2, H);
    ctx.stroke();

    const N     = 9;
    const FILL  = 0.42;
    const roadH = H - vy;

    for (let i = -1; i <= N + 1; i++) {
      const t0  = (i + this.offset) / N;
      const t1  = t0 + FILL / N;
      const tc0 = Math.max(0, Math.min(1, t0));
      const tc1 = Math.max(0, Math.min(1, t1));
      if (tc1 <= tc0) continue;

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
      ctx.fillStyle = 'rgba(52, 211, 153, 0.90)';
      ctx.fill();
    }

    const glow = ctx.createRadialGradient(vx, vy, 0, vx, vy, W * 0.52);
    glow.addColorStop(0, 'rgba(52,211,153,0.07)');
    glow.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = glow;
    ctx.fillRect(0, 0, W, H);
  }
}
