import { Component, input } from '@angular/core';

type StatusVariant = 'primary' | 'success' | 'danger' | 'warning' | 'neutral';

// Maps raw backend status strings (lesson statuses: Scheduled/Completed/Cancelled,
// enrollment statuses: Pending/Active/Completed/Cancelled, plus the client-computed
// "Overdue") to a shared color language, case-insensitively.
const VARIANT_MAP: Record<string, StatusVariant> = {
  scheduled: 'primary',
  active:    'success',
  completed: 'success',
  cancelled: 'danger',
  overdue:   'warning',
  pending:   'warning',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.scss'
})
export class StatusBadgeComponent {
  readonly status  = input.required<string>();
  readonly showDot = input(false);

  variantClass(): string {
    const variant = VARIANT_MAP[this.status()?.toLowerCase()] ?? 'neutral';
    return `status-badge--${variant}`;
  }
}
