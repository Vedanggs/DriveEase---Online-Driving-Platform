export interface BookLessonRequest {
  enrollmentId: string;
  studentId: string;
  studentName: string;
  instructorId: string;
  instructorName: string;
  scheduledAt: string;
  duration: string;
}

export interface LessonDto {
  id: string;
  enrollmentId: string;
  studentId: string;
  instructorId: string;
  instructorName: string | null;
  scheduledAt: string;
  duration: string;   // TimeSpan from backend: "01:00:00"
  status: string;
  notes: string | null;
}

export interface InstructorLessonDto {
  id: string;
  enrollmentId: string;
  studentId: string;
  studentName: string;
  scheduledAt: string;
  duration: string;
  status: string;
  notes: string | null;
  completedAt: string | null;
}
