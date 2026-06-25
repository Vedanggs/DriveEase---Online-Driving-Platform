-- Drop existing tables (in case of partial creation) then recreate clean

-- Schools
IF OBJECT_ID('schools.Schools', 'U') IS NOT NULL DROP TABLE schools.Schools;
IF OBJECT_ID('schools.Instructors', 'U') IS NOT NULL DROP TABLE schools.Instructors;
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'schools') EXEC('DROP SCHEMA schools');

-- Students
IF OBJECT_ID('students.Students', 'U') IS NOT NULL DROP TABLE students.Students;
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'students') EXEC('DROP SCHEMA students');

-- Enrollments
IF OBJECT_ID('enrollments.Enrollments', 'U') IS NOT NULL DROP TABLE enrollments.Enrollments;
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'enrollments') EXEC('DROP SCHEMA enrollments');

-- Lessons
IF OBJECT_ID('lessons.Lessons', 'U') IS NOT NULL DROP TABLE lessons.Lessons;
IF EXISTS (SELECT * FROM sys.schemas WHERE name = 'lessons') EXEC('DROP SCHEMA lessons');

-- ── Create schemas ────────────────────────────────────────────────────────────
EXEC('CREATE SCHEMA schools');
EXEC('CREATE SCHEMA students');
EXEC('CREATE SCHEMA enrollments');
EXEC('CREATE SCHEMA lessons');

-- ── schools.Schools ───────────────────────────────────────────────────────────
CREATE TABLE schools.Schools (
    Id              UNIQUEIDENTIFIER NOT NULL,
    Name            NVARCHAR(200)    NOT NULL,
    Address         NVARCHAR(500)    NOT NULL,
    ContactEmail    NVARCHAR(200)    NOT NULL,
    IsActive        BIT              NOT NULL,
    RegisteredAt    DATETIME2        NOT NULL,
    CONSTRAINT PK_Schools PRIMARY KEY (Id)
);

-- ── schools.Instructors ───────────────────────────────────────────────────────
CREATE TABLE schools.Instructors (
    Id              UNIQUEIDENTIFIER NOT NULL,
    SchoolId        UNIQUEIDENTIFIER NOT NULL,
    FullName        NVARCHAR(200)    NOT NULL,
    LicenseNumber   NVARCHAR(50)     NOT NULL,
    IsAvailable     BIT              NOT NULL,
    CONSTRAINT PK_Instructors PRIMARY KEY (Id)
);
CREATE INDEX IX_Instructors_SchoolId ON schools.Instructors (SchoolId);

-- ── students.Students ─────────────────────────────────────────────────────────
CREATE TABLE students.Students (
    Id              UNIQUEIDENTIFIER NOT NULL,
    FullName        NVARCHAR(200)    NOT NULL,
    Email           NVARCHAR(200)    NOT NULL,
    PhoneNumber     NVARCHAR(30)     NULL,
    DateOfBirth     DATE             NOT NULL,
    RegisteredAt    DATETIME2        NOT NULL,
    PasswordHash    NVARCHAR(500)    NOT NULL,
    CONSTRAINT PK_Students PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX IX_Students_Email ON students.Students (Email);

-- ── enrollments.Enrollments ───────────────────────────────────────────────────
CREATE TABLE enrollments.Enrollments (
    Id                  UNIQUEIDENTIFIER NOT NULL,
    StudentId           UNIQUEIDENTIFIER NOT NULL,
    DrivingSchoolId     UNIQUEIDENTIFIER NOT NULL,
    InstructorId        UNIQUEIDENTIFIER NULL,
    Fee                 DECIMAL(18,2)    NOT NULL,
    PaymentStatus       NVARCHAR(MAX)    NOT NULL,
    Status              NVARCHAR(MAX)    NOT NULL,
    EnrolledAt          DATETIME2        NOT NULL,
    PaymentConfirmedAt  DATETIME2        NULL,
    CancelledAt         DATETIME2        NULL,
    CONSTRAINT PK_Enrollments PRIMARY KEY (Id)
);
CREATE INDEX IX_Enrollments_StudentId ON enrollments.Enrollments (StudentId);
CREATE INDEX IX_Enrollments_StudentId_Status ON enrollments.Enrollments (StudentId, Status);

-- ── lessons.Lessons ───────────────────────────────────────────────────────────
CREATE TABLE lessons.Lessons (
    Id              UNIQUEIDENTIFIER NOT NULL,
    EnrollmentId    UNIQUEIDENTIFIER NOT NULL,
    StudentId       UNIQUEIDENTIFIER NOT NULL,
    InstructorId    UNIQUEIDENTIFIER NOT NULL,
    ScheduledAt     DATETIME2        NOT NULL,
    Duration        FLOAT            NOT NULL,
    Status          NVARCHAR(MAX)    NOT NULL,
    Notes           NVARCHAR(MAX)    NULL,
    CompletedAt     DATETIME2        NULL,
    CONSTRAINT PK_Lessons PRIMARY KEY (Id)
);
CREATE INDEX IX_Lessons_StudentId_ScheduledAt ON lessons.Lessons (StudentId, ScheduledAt);
CREATE INDEX IX_Lessons_EnrollmentId ON lessons.Lessons (EnrollmentId);

PRINT 'All schemas and tables created successfully.';
