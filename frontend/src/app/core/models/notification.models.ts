export interface InstructorNotificationDto {
  id: string;
  type: 'enrollment' | 'lesson';
  studentName: string;
  detail: string;
  scheduledAt: string | null;
  createdAt: string;
  isRead: boolean;
}
