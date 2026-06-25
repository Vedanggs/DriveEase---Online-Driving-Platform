export interface BookLessonRequest {
  enrollmentId: string;
  studentId: string;
  instructorId: string;
  scheduledAt: string;
  duration: string;
}

export interface LessonDto {
  id: string;
  enrollmentId: string;
  studentId: string;
  instructorId: string;
  scheduledAt: string;
  duration: string;   // TimeSpan from backend: "01:00:00"
  status: string;
  notes: string | null;
}
