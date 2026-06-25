import { Component, OnInit, signal } from '@angular/core';

interface Notification {
  id: string;
  type: 'enrollment' | 'lesson';
  studentName: string;
  detail: string;
  time: string;
  read: boolean;
}

@Component({
  selector: 'app-instructor-dashboard',
  standalone: true,
  imports: [],
  templateUrl: './instructor-dashboard.html',
  styleUrl: './instructor-dashboard.scss'
})
export class InstructorDashboardComponent implements OnInit {

  readonly instructorName = signal('');
  readonly schoolName     = signal('');

  readonly notifications = signal<Notification[]>([
    {
      id: '1',
      type: 'enrollment',
      studentName: 'Vedang Shinde',
      detail: 'enrolled at Pune Road Masters',
      time: 'Today, 10:32 AM',
      read: false
    },
    {
      id: '2',
      type: 'lesson',
      studentName: 'Vedang Shinde',
      detail: 'booked a lesson for tomorrow at 9:00 AM',
      time: 'Today, 11:05 AM',
      read: false
    }
  ]);

  ngOnInit() {
    const raw = sessionStorage.getItem('instructor_session');
    if (!raw) { window.location.href = '/instructor-login'; return; }

    const session = JSON.parse(raw);
    this.instructorName.set(session.name ?? 'Instructor');
    this.schoolName.set(session.schoolName ?? 'Your School');
  }

  markRead(id: string) {
    this.notifications.update(list =>
      list.map(n => n.id === id ? { ...n, read: true } : n)
    );
  }

  get unreadCount(): number {
    return this.notifications().filter(n => !n.read).length;
  }

  logout() {
    sessionStorage.removeItem('instructor_session');
    window.location.href = '/instructor-login';
  }
}
