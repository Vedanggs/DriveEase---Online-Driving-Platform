export interface InstructorNotificationDto {
  id: string;
  type: 'enrollment' | 'lesson';
  studentName: string;
  detail: string;
  createdAt: string;
  isRead: boolean;
}
