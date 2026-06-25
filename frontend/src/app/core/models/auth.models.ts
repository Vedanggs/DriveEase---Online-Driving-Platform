export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string | null;
  dateOfBirth: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  studentId: string;
  fullName: string;
  email: string;
}

export interface InstructorLoginRequest {
  email: string;
  password: string;
  schoolId: string;
}

export interface InstructorRegisterRequest {
  schoolId: string;
  fullName: string;
  email: string;
  licenseNumber: string;
  password: string;
}

export interface InstructorAuthResponse {
  accessToken: string;
  instructorId: string;
  schoolId: string;
  fullName: string;
  email: string;
  schoolName: string;
}

export interface InstructorNotification {
  id: string;
  type: 'enrollment' | 'lesson';
  studentName: string;
  detail: string;
  occurredAt: string;
}
