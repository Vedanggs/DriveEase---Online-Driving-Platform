import { Component, computed, HostListener, inject, signal, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { environment } from '../../../../environments/environment';

interface DemoInstructor {
  name: string;
  email: string;
  license: string;
  password: string;
}

interface DemoSchool {
  name: string;
  instructors: DemoInstructor[];
}

const DEMO_SCHOOLS: DemoSchool[] = [
  { name: 'Mumbai Central Driving Academy',  instructors: [{ name: 'Amit Sharma',    email: 'amit.sharma@mumbaicentral.com',            license: 'MH-01AB1001', password: 'Instructor@123' }] },
  { name: 'Pune Road Masters',               instructors: [{ name: 'Suresh Jadhav',  email: 'suresh.jadhav@puneroadmasters.com',         license: 'MH-12CD2001', password: 'Instructor@123' }] },
  { name: 'Sunrise Driving Academy',         instructors: [{ name: 'Mohan Singh',    email: 'mohan.singh@sunrise.driveease.com',          license: 'MH-02EF3001', password: 'Instructor@123' }] },
  { name: 'Thinkschool Safe Drive Institute',instructors: [{ name: 'Rahul Sharma',   email: 'rahul.sharma@thinkschool.com',               license: 'MH-20GH4001', password: 'Instructor@123' }] },
  { name: 'Mumbai Drive Academy',            instructors: [{ name: 'Suresh Patil',   email: 'suresh.patil@mumbaidriveacademy.com',        license: 'MH-1013',     password: 'Instructor@123' }] },
  { name: 'Nashik Road Pro',                 instructors: [{ name: 'Vikram Nair',    email: 'vikram.nair@nashikroadpro.com',              license: 'MH-1016',     password: 'Instructor@123' }] },
  { name: 'Nagpur Speed School',             instructors: [{ name: 'Deepak Tiwari',  email: 'deepak.tiwari@nagpurspeedschool.com',        license: 'MH-1019',     password: 'Instructor@123' }] },
  { name: 'Aurangabad Motor Training',       instructors: [{ name: 'Mohan Pawar',    email: 'mohan.pawar@aurangabadmotor.com',            license: 'MH-1022',     password: 'Instructor@123' }] },
  { name: 'Kolhapur Drive Centre',           instructors: [{ name: 'Ganesh Bhosale', email: 'ganesh.bhosale@kolhapurdrive.com',           license: 'MH-1025',     password: 'Instructor@123' }] },
  { name: 'Solapur Road Academy',            instructors: [{ name: 'Nilesh Mane',    email: 'nilesh.mane@solapurroadacademy.com',         license: 'MH-1028',     password: 'Instructor@123' }] },
  { name: 'Thane AutoDrive School',          instructors: [{ name: 'Anil Gaikwad',   email: 'anil.gaikwad@thaneautodrive.com',            license: 'MH-1031',     password: 'Instructor@123' }] },
  { name: 'Navi Mumbai Driving Hub',         instructors: [{ name: 'Vinod Kharat',   email: 'vinod.kharat@navimumbaidriving.com',         license: 'MH-1034',     password: 'Instructor@123' }] },
  { name: 'Pimpri-Chinchwad Road School',    instructors: [{ name: 'Santosh Jagtap', email: 'santosh.jagtap@pcmcroadschool.com',          license: 'MH-1037',     password: 'Instructor@123' }] },
  { name: 'Sangli Drive Institute',          instructors: [{ name: 'Pramod Kale',    email: 'pramod.kale@sanglidrive.com',                license: 'MH-1040',     password: 'Instructor@123' }] },
  { name: 'Satara Motor Academy',            instructors: [{ name: 'Mangesh Karale', email: 'mangesh.karale@sataramotor.com',             license: 'MH-1043',     password: 'Instructor@123' }] },
  { name: 'Latur Road Training Centre',      instructors: [{ name: 'Dilip Londhe',   email: 'dilip.londhe@laturroadtraining.com',         license: 'MH-1046',     password: 'Instructor@123' }] },
  { name: 'Jalgaon Drive School',            instructors: [{ name: 'Hemant Patil',   email: 'hemant.patil@jalgaondrive.com',              license: 'MH-1049',     password: 'Instructor@123' }] },
  { name: 'Amravati AutoSkills',             instructors: [{ name: 'Ajay Deshmukh',  email: 'ajay.deshmukh@amravatiautoskills.com',       license: 'MH-1052',     password: 'Instructor@123' }] },
  { name: 'Akola Road Masters',              instructors: [{ name: 'Sunil Wankhade', email: 'sunil.wankhade@akolaroadmasters.com',        license: 'MH-1055',     password: 'Instructor@123' }] },
  { name: 'Ratnagiri Coastal Drive',         instructors: [{ name: 'Ramesh Gavhane', email: 'ramesh.gavhane@ratnagiricoastaldrive.com',   license: 'MH-1058',     password: 'Instructor@123' }] },
];

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss'
})
export class LoginComponent implements AfterViewInit, OnDestroy {
  @ViewChild('roadCanvas') private readonly canvasRef!: ElementRef<HTMLCanvasElement>;

  private readonly fb     = inject(FormBuilder);
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);
  private readonly http   = inject(HttpClient);

  readonly loading = signal(false);
  readonly error   = signal<string | null>(null);
  readonly role    = signal<'student' | 'instructor'>('student');

  readonly form = this.fb.group({
    email:    ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  // ── Demo credentials ───────────────────────────────────────────────────────
  readonly showDemoModal = signal(false);
  readonly demoSearch    = signal('');
  readonly copiedKey     = signal<string | null>(null);
  readonly demoSchools   = DEMO_SCHOOLS;
  readonly demoSeeding   = signal(false);

  readonly filteredDemoSchools = computed(() => {
    const q = this.demoSearch().trim().toLowerCase();
    if (!q) return this.demoSchools;
    return this.demoSchools
      .map(school => ({
        ...school,
        instructors: school.instructors.filter(i =>
          i.name.toLowerCase().includes(q)  ||
          i.email.toLowerCase().includes(q) ||
          school.name.toLowerCase().includes(q)
        )
      }))
      .filter(s => s.instructors.length > 0);
  });

  // ── Road canvas ────────────────────────────────────────────────────────────
  private rafId: number | null = null;
  private offset = 0;
  private resizeHandler: (() => void) | null = null;

  ngAfterViewInit() {
    if (!window.matchMedia('(prefers-reduced-motion: reduce)').matches) this.startRoad();
    this.seedDemoInstructors();
  }

  ngOnDestroy() {
    if (this.rafId !== null) cancelAnimationFrame(this.rafId);
    if (this.resizeHandler) window.removeEventListener('resize', this.resizeHandler);
  }

  @HostListener('keydown.escape')
  onEscape() {
    if (this.showDemoModal()) this.showDemoModal.set(false);
  }

  setRole(r: 'student' | 'instructor') {
    this.role.set(r);
    this.error.set(null);
    this.form.reset();
    if (r === 'instructor') this.seedDemoInstructors();
  }

  private async seedDemoInstructors(): Promise<void> {
    // Phase 1 — instant, no API calls: seed fake IDs so login works immediately
    const profiles: Record<string, any> = JSON.parse(localStorage.getItem('instructor_profiles') ?? '{}');
    let changed = false;
    for (const school of DEMO_SCHOOLS) {
      for (const inst of school.instructors) {
        if (profiles[inst.email]) continue;
        profiles[inst.email] = {
          instructorId: crypto.randomUUID(),
          name: inst.name,
          schoolName: school.name,
          schoolId: crypto.randomUUID(),
          passwordHash: btoa(inst.password)
        };
        changed = true;
      }
    }
    if (changed) localStorage.setItem('instructor_profiles', JSON.stringify(profiles));

    // Phase 2 — background: replace fake IDs with real backend IDs (so lessons show up)
    this.demoSeeding.set(true);
    try {
      const schools = await firstValueFrom(
        this.http.get<{ id: string; name: string }[]>(`${environment.apiUrl}/api/v1/schools`)
      );
      const live = JSON.parse(localStorage.getItem('instructor_profiles') ?? '{}');
      let enriched = false;

      for (const demoSchool of DEMO_SCHOOLS) {
        const backendSchool = schools.find(s => s.name === demoSchool.name);
        if (!backendSchool) continue;
        try {
          const list = await firstValueFrom(
            this.http.get<{ id: string; licenseNumber: string }[]>(
              `${environment.apiUrl}/api/v1/schools/${backendSchool.id}/instructors`
            )
          );
          for (const inst of demoSchool.instructors) {
            const found = list.find(i => i.licenseNumber === inst.license);
            if (found && live[inst.email]) {
              live[inst.email].instructorId = found.id;
              live[inst.email].schoolId     = backendSchool.id;
              enriched = true;
            } else if (!found && live[inst.email]) {
              try {
                const res = await firstValueFrom(
                  this.http.post<{ id: string }>(
                    `${environment.apiUrl}/api/v1/schools/${backendSchool.id}/instructors`,
                    { fullName: inst.name, licenseNumber: inst.license }
                  )
                );
                live[inst.email].instructorId = res.id;
                live[inst.email].schoolId     = backendSchool.id;
                enriched = true;
              } catch { /* duplicate license or error — keep fake ID */ }
            }
          }
        } catch { /* school fetch failed — keep fake IDs */ }
        if (enriched) localStorage.setItem('instructor_profiles', JSON.stringify(live));
      }
    } catch { /* backend unavailable — fake IDs remain, login still works */ }
    finally { this.demoSeeding.set(false); }
  }

  openDemoModal()  { this.showDemoModal.set(true);  this.demoSearch.set(''); }
  closeDemoModal() { this.showDemoModal.set(false); }

  fillStudentDemo() {
    this.form.setValue({ email: 'test@gmail.com', password: 'Test@123' });
  }

  copyToClipboard(text: string, key: string) {
    navigator.clipboard.writeText(text).then(() => {
      this.copiedKey.set(key);
      setTimeout(() => this.copiedKey.set(null), 2000);
    });
  }

  schoolHue(name: string): number {
    let h = 0;
    for (const c of name) h = (h * 31 + c.charCodeAt(0)) & 0xffff;
    return h % 360;
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }

    this.loading.set(true);
    this.error.set(null);

    const { email, password } = this.form.value;

    if (this.role() === 'instructor') {
      const profiles: Record<string, any> =
        JSON.parse(localStorage.getItem('instructor_profiles') ?? '{}');
      const saved = profiles[email!];

      if (!saved) {
        this.error.set('No account found for this email. Please register first.');
        this.loading.set(false);
        return;
      }

      if (saved.passwordHash !== btoa(password!)) {
        this.error.set('Incorrect password. Please try again.');
        this.loading.set(false);
        return;
      }

      const { passwordHash: _pw, ...session } = saved;
      sessionStorage.setItem('instructor_session', JSON.stringify({ ...session, email }));
      this.loading.set(false);
      this.router.navigate(['/instructor/dashboard']);
      return;
    }

    this.auth.login({ email: email!, password: password! }).subscribe({
      next:  () => this.router.navigate(['/schools']),
      error: () => {
        this.error.set('Invalid email or password.');
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
    const isInstructor = this.role() === 'instructor';
    const W = canvas.width, H = canvas.height;
    ctx.clearRect(0, 0, W, H);

    const vx = W * 0.5, vy = H * 0.40;
    const topW = W * 0.07, botW = W * 1.35;

    ctx.beginPath();
    ctx.moveTo(vx - topW / 2, vy); ctx.lineTo(vx + topW / 2, vy);
    ctx.lineTo(vx + botW / 2, H);  ctx.lineTo(vx - botW / 2, H);
    ctx.closePath();
    ctx.fillStyle = isInstructor ? '#0f1a0f' : '#181818';
    ctx.fill();

    ctx.lineWidth = 1;
    ctx.strokeStyle = 'rgba(255,255,255,0.12)';
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
      ctx.fillStyle = isInstructor ? 'rgba(52,211,153,0.90)' : 'rgba(245,197,24,0.88)';
      ctx.fill();
    }

    const color = isInstructor ? '52,211,153' : '245,197,24';
    const glow = ctx.createRadialGradient(vx, vy, 0, vx, vy, W * 0.52);
    glow.addColorStop(0, `rgba(${color},0.06)`);
    glow.addColorStop(1, 'rgba(0,0,0,0)');
    ctx.fillStyle = glow;
    ctx.fillRect(0, 0, W, H);
  }
}
