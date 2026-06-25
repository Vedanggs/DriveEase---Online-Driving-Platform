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
