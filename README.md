# DriveEase — Driving School Management Platform

---
### Demo Link - https://drive.google.com/file/d/1NHAKi7J1Z70H9OKL0Ih9DGqQoowKFTZv/view?usp=drive_link


## One-Page Design

---

### Bounded Contexts

- **Schools** — manage driving schools, instructors, available slots
- **Students** — student registration and profile management
- **Enrollments** — student enrolls in a school, payment processing *(core)*
- **Lessons** — lesson booking, scheduling, completion
- **Notifications** — email/SMS confirmations, reminders, alerts

---

### Core Aggregate — `Enrollment`

**Fields:**
- `Id` — Guid
- `StudentId` — Guid
- `DrivingSchoolId` — Guid
- `InstructorId` — Guid? *(assigned after payment)*
- `Fee` — decimal
- `PaymentStatus` — Pending → Paid | Failed
- `Status` — Pending → Active → Completed | Cancelled
- `EnrolledAt` — DateTime

**Invariants:**
- Fee must be > 0 at creation
- `ConfirmPayment()` only allowed when PaymentStatus is Pending
- `AssignInstructor()` only allowed when Status is Active
- `CanBookLesson()` returns true only when Active + Paid
- `Cancel()` forbidden once Completed or already Cancelled

**Domain Events raised:**
- `EnrollmentConfirmedEvent` — after payment succeeds
- `PaymentFailedEvent` — after payment fails
- `InstructorAssignedEvent` — after instructor is assigned
- `EnrollmentCancelledEvent` — after cancellation

---

### Async Flows

**Flow 1 — Enrollment Confirmed:**
- Student pays fee
- `FakePaymentGateway` returns success
- `enrollment.ConfirmPayment()` — state saved to DB
- `EnrollmentConfirmedEvent` published to `IEventBus`
- `OnEnrollmentConfirmed` → email: "Welcome, instructor being assigned"
- `OnInstructorAssigned` → email: "Instructor ready, book your first lesson"

**Flow 2 — Lesson Reminder:**
- Student books a lesson slot
- `LessonBookedEvent` published
- `OnLessonBooked` → email: "Lesson confirmed"
- `LessonReminderWorker` runs every hour (BackgroundService)
- Scans lessons where ScheduledAt is within the next 24–25 hours
- Sends reminder email 24 hours before the lesson

**Flow 3 — Incomplete Enrollment Alert + Auto-Cancel:**
- Student starts enrollment but payment fails
- `PaymentFailedEvent` published
- `OnPaymentFailed` → email: "Payment failed, retry within 72h"
- `IncompleteEnrollmentWorker` runs every 24 hours (BackgroundService)
- Scans enrollments where PaymentStatus = Pending and EnrolledAt < now − 72h
- Calls `enrollment.Cancel()` → `EnrollmentCancelledEvent` published
- `OnEnrollmentCancelled` → email: "Enrollment auto-cancelled"

**Flow 4 — Lesson Completed:**
- Instructor marks lesson complete via `POST /lessons/{id}/complete`
- `lesson.Complete(notes)` — state saved
- `LessonCompletedEvent` published
- `OnLessonCompleted` → email: "Rate your experience"

---

## Scaffolded Solution Layout

```
DriveEase/
├── DriveEase.sln                                        19 projects
│
├── src/
│   ├── DriveEase.Shared/
│   │   ├── Domain/
│   │   │   ├── Entity.cs                    value-equality base class
│   │   │   ├── AggregateRoot.cs             domain event list + ClearDomainEvents()
│   │   │   ├── IDomainEvent.cs
│   │   │   └── IIntegrationEvent.cs
│   │   └── Messaging/
│   │       ├── IEventBus.cs
│   │       └── InMemoryEventBus.cs          swap → Azure Service Bus / RabbitMQ in prod
│   │
│   ├── DriveEase.Api/
│   │   ├── Controllers/
│   │   │   ├── EnrollmentsController.cs
│   │   │   ├── SchoolsController.cs
│   │   │   ├── StudentsController.cs
│   │   │   └── LessonsController.cs
│   │   └── Program.cs                       module wiring, MediatR, Swagger
│   │
│   └── Modules/
│       │
│       ├── Enrollments/                     ← core bounded context
│       │   ├── DriveEase.Enrollments.Domain/
│       │   │   ├── Aggregates/
│       │   │   │   └── Enrollment.cs        THE aggregate
│       │   │   ├── ValueObjects/
│       │   │   │   ├── EnrollmentStatus.cs
│       │   │   │   └── PaymentStatus.cs
│       │   │   ├── Events/
│       │   │   │   ├── EnrollmentConfirmedEvent.cs
│       │   │   │   ├── PaymentFailedEvent.cs
│       │   │   │   ├── InstructorAssignedEvent.cs
│       │   │   │   └── EnrollmentCancelledEvent.cs
│       │   │   └── Repositories/
│       │   │       └── IEnrollmentRepository.cs
│       │   ├── DriveEase.Enrollments.Application/
│       │   │   ├── Commands/
│       │   │   │   ├── EnrollStudent/       command + handler
│       │   │   │   ├── ProcessPayment/      command + handler
│       │   │   │   └── AssignInstructor/    command + handler
│       │   │   ├── Queries/GetEnrollment/   query + handler
│       │   │   ├── DTOs/EnrollmentDto.cs
│       │   │   └── Services/
│       │   │       └── IPaymentGateway.cs
│       │   └── DriveEase.Enrollments.Infrastructure/
│       │       ├── Persistence/
│       │       │   ├── EnrollmentsDbContext.cs    EF Core / SQLite, schema="enrollments"
│       │       │   └── EnrollmentRepository.cs
│       │       ├── Workers/
│       │       │   └── IncompleteEnrollmentWorker.cs   Flow 3: daily, auto-cancel @72h
│       │       ├── FakePaymentGateway.cs          always succeeds; one-class swap in prod
│       │       └── EnrollmentsModule.cs           IServiceCollection extension
│       │
│       ├── Schools/
│       │   ├── DriveEase.Schools.Domain/
│       │   │   ├── Entities/
│       │   │   │   ├── DrivingSchool.cs
│       │   │   │   └── Instructor.cs
│       │   │   └── Repositories/
│       │   │       └── IDrivingSchoolRepository.cs
│       │   ├── DriveEase.Schools.Application/
│       │   │   ├── Commands/RegisterSchool/
│       │   │   └── Queries/GetSchool/
│       │   └── DriveEase.Schools.Infrastructure/
│       │       ├── Persistence/
│       │       │   ├── SchoolsDbContext.cs
│       │       │   └── SchoolRepository.cs
│       │       └── SchoolsModule.cs
│       │
│       ├── Students/
│       │   ├── DriveEase.Students.Domain/
│       │   │   ├── Entities/Student.cs      duplicate-email guard on register
│       │   │   └── Repositories/IStudentRepository.cs
│       │   ├── DriveEase.Students.Application/
│       │   │   ├── Commands/RegisterStudent/
│       │   │   └── Queries/GetStudent/
│       │   └── DriveEase.Students.Infrastructure/
│       │       ├── Persistence/
│       │       │   ├── StudentsDbContext.cs
│       │       │   └── StudentRepository.cs
│       │       └── StudentsModule.cs
│       │
│       ├── Lessons/
│       │   ├── DriveEase.Lessons.Domain/
│       │   │   ├── Entities/Lesson.cs
│       │   │   ├── Events/
│       │   │   │   ├── LessonBookedEvent.cs
│       │   │   │   └── LessonCompletedEvent.cs
│       │   │   └── Repositories/ILessonRepository.cs
│       │   ├── DriveEase.Lessons.Application/
│       │   │   ├── Commands/
│       │   │   │   ├── BookLesson/
│       │   │   │   └── CompleteLesson/
│       │   │   └── Queries/GetLesson/
│       │   └── DriveEase.Lessons.Infrastructure/
│       │       ├── Persistence/
│       │       │   ├── LessonsDbContext.cs
│       │       │   ├── LessonRepository.cs
│       │       │   └── UpcomingLessonsQuery.cs
│       │       ├── Workers/
│       │       │   └── LessonReminderWorker.cs    Flow 2: hourly scan, 24h reminder
│       │       └── LessonsModule.cs
│       │
│       └── Notifications/
│           ├── DriveEase.Notifications.Domain/
│           │   └── Entities/NotificationLog.cs
│           ├── DriveEase.Notifications.Application/
│           │   ├── EventHandlers/
│           │   │   ├── OnEnrollmentConfirmed.cs   Flow 1
│           │   │   ├── OnPaymentFailed.cs         Flow 3
│           │   │   ├── OnEnrollmentCancelled.cs   Flow 3
│           │   │   ├── OnInstructorAssigned.cs    Flow 1
│           │   │   ├── OnLessonBooked.cs          Flow 2
│           │   │   └── OnLessonCompleted.cs       Flow 4
│           │   └── Services/INotificationSender.cs
│           └── DriveEase.Notifications.Infrastructure/
│               ├── FakeNotificationSender.cs      logs to console; swap with SMTP/Twilio
│               └── NotificationsModule.cs         wires all 6 handlers to IEventBus
│
└── tests/
    ├── DriveEase.Enrollments.Domain.Tests/
    │   └── EnrollmentTests.cs             9 / 9 passing ✓
    └── DriveEase.Enrollments.Application.Tests/   scaffold ready
```

---

**Solution root:**
- `DriveEase.sln` — 19 projects total

**Shared Kernel (`DriveEase.Shared`):**
- `Entity.cs` — value-equality base class
- `AggregateRoot.cs` — domain event list + `ClearDomainEvents()`
- `IDomainEvent.cs` / `IIntegrationEvent.cs`
- `IEventBus.cs` + `InMemoryEventBus.cs`

**API (`DriveEase.Api`):**
- `EnrollmentsController.cs`
- `SchoolsController.cs`
- `StudentsController.cs`
- `LessonsController.cs`
- `Program.cs` — module wiring, MediatR, Swagger

**Enrollments Module — core:**
- `Enrollment.cs` — the aggregate
- `EnrollmentStatus.cs` / `PaymentStatus.cs` — value objects
- `EnrollmentConfirmedEvent.cs` / `PaymentFailedEvent.cs` / `InstructorAssignedEvent.cs` / `EnrollmentCancelledEvent.cs`
- `IEnrollmentRepository.cs`
- `EnrollStudentCommand` + handler
- `ProcessPaymentCommand` + handler
- `AssignInstructorCommand` + handler
- `GetEnrollmentQuery` + handler
- `IPaymentGateway.cs`
- `EnrollmentsDbContext.cs` — EF Core / SQLite
- `EnrollmentRepository.cs`
- `FakePaymentGateway.cs` — always succeeds, one-class swap in prod
- `IncompleteEnrollmentWorker.cs` — BackgroundService, runs every 24h, auto-cancels at 72h
- `EnrollmentsModule.cs` — DI registration

**Schools Module:**
- `DrivingSchool.cs` / `Instructor.cs` — entities
- `IDrivingSchoolRepository.cs` / `IInstructorRepository.cs`
- `RegisterSchoolCommand` + handler
- `GetSchoolQuery` + handler
- `SchoolsDbContext.cs` / `SchoolRepository.cs` / `InstructorRepository.cs`
- `SchoolsModule.cs` — DI registration

**Students Module:**
- `Student.cs` — entity with duplicate-email guard
- `IStudentRepository.cs`
- `RegisterStudentCommand` + handler
- `GetStudentQuery` + handler
- `StudentsDbContext.cs` / `StudentRepository.cs`
- `StudentsModule.cs` — DI registration

**Lessons Module:**
- `Lesson.cs` — entity
- `LessonBookedEvent.cs` / `LessonCompletedEvent.cs`
- `ILessonRepository.cs`
- `BookLessonCommand` + handler
- `CompleteLessonCommand` + handler
- `GetLessonQuery` + handler
- `LessonsDbContext.cs` / `LessonRepository.cs` / `UpcomingLessonsQuery.cs`
- `LessonReminderWorker.cs` — BackgroundService, runs every 1h, sends 24h reminder
- `LessonsModule.cs` — DI registration

**Notifications Module:**
- `NotificationLog.cs` — entity
- `INotificationSender.cs`
- `OnEnrollmentConfirmed.cs` — Flow 1
- `OnInstructorAssigned.cs` — Flow 1
- `OnLessonBooked.cs` — Flow 2
- `OnPaymentFailed.cs` — Flow 3
- `OnEnrollmentCancelled.cs` — Flow 3
- `OnLessonCompleted.cs` — Flow 4
- `FakeNotificationSender.cs` — logs to console, swap with SMTP/Twilio in prod
- `NotificationsModule.cs` — wires all 6 handlers to `IEventBus`

**Tests:**
- `EnrollmentTests.cs` — 9/9 domain invariant tests passing
- `DriveEase.Enrollments.Application.Tests` — scaffold ready

---

## Key Architecture Decisions

- **Modular monolith** — single deployable unit, modules split to microservices only if load demands it
- **Each module owns its own DbContext** — physical isolation without distributed transactions
- **Modules never call each other directly** — communicate only through `IEventBus` integration events
- **Events published only after state is saved** — no event fires before the DB write succeeds
- **BackgroundService workers** — no external scheduler needed at scaffold stage
- **Fake implementations** — `FakePaymentGateway` and `FakeNotificationSender` are one-class swaps, not config flags
