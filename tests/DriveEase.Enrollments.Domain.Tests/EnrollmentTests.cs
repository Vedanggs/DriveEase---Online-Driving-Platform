using Xunit;
using DriveEase.Enrollments.Domain.Aggregates;
using DriveEase.Enrollments.Domain.Events;
using DriveEase.Enrollments.Domain.ValueObjects;
using FluentAssertions;

namespace DriveEase.Enrollments.Domain.Tests;

public sealed class EnrollmentTests
{
    private static readonly Guid StudentId = Guid.NewGuid();
    private static readonly Guid SchoolId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidArgs_SetsStatusPending()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);

        enrollment.Status.Should().Be(EnrollmentStatus.Pending);
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Pending);
        enrollment.Fee.Should().Be(500m);
    }

    [Fact]
    public void Create_WithZeroFee_Throws()
    {
        var act = () => Enrollment.Create(StudentId, SchoolId, 0m);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConfirmPayment_WhenPending_ActivatesEnrollmentAndRaisesEvent()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);

        enrollment.ConfirmPayment();

        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.PaymentStatus.Should().Be(PaymentStatus.Paid);
        enrollment.DomainEvents.Should().ContainSingle(e => e is EnrollmentConfirmedEvent);
    }

    [Fact]
    public void ConfirmPayment_WhenAlreadyPaid_Throws()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);
        enrollment.ConfirmPayment();

        var act = () => enrollment.ConfirmPayment();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FailPayment_WhenPending_SetsFailedAndRaisesEvent()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);

        enrollment.FailPayment("Card declined");

        enrollment.PaymentStatus.Should().Be(PaymentStatus.Failed);
        enrollment.DomainEvents.Should().ContainSingle(e => e is PaymentFailedEvent);
    }

    [Fact]
    public void AssignInstructor_WhenActive_SetsInstructorAndRaisesEvent()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);
        enrollment.ConfirmPayment();
        enrollment.ClearDomainEvents();

        var instructorId = Guid.NewGuid();
        enrollment.AssignInstructor(instructorId);

        enrollment.InstructorId.Should().Be(instructorId);
        enrollment.DomainEvents.Should().ContainSingle(e => e is InstructorAssignedEvent);
    }

    [Fact]
    public void AssignInstructor_WhenNotActive_Throws()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);

        var act = () => enrollment.AssignInstructor(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CanBookLesson_OnlyWhenActiveAndPaid()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);
        enrollment.CanBookLesson().Should().BeFalse();

        enrollment.ConfirmPayment();
        enrollment.CanBookLesson().Should().BeTrue();
    }

    [Fact]
    public void Cancel_AfterCompletion_Throws()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);
        enrollment.ConfirmPayment();
        enrollment.Complete();

        var act = () => enrollment.Cancel("test");
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_WhenPending_RaisesEventAndSetsTimestamp()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);

        enrollment.Cancel("changed my mind");

        enrollment.Status.Should().Be(EnrollmentStatus.Cancelled);
        enrollment.CancelledAt.Should().NotBeNull();
        enrollment.DomainEvents.Should().ContainSingle(e => e is EnrollmentCancelledEvent);
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);
        enrollment.Cancel("first cancel");
        enrollment.ClearDomainEvents();

        var act = () => enrollment.Cancel("second cancel");
        act.Should().Throw<InvalidOperationException>("double-cancel must be rejected");
    }

    [Fact]
    public void Complete_WhenCancelled_Throws()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);
        enrollment.Cancel("gone");

        var act = () => enrollment.Complete();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void FailPayment_WhenAlreadyFailed_Throws()
    {
        var enrollment = Enrollment.Create(StudentId, SchoolId, 500m);
        enrollment.FailPayment("Card declined");
        enrollment.ClearDomainEvents();

        var act = () => enrollment.FailPayment("Card declined again");
        act.Should().Throw<InvalidOperationException>("cannot fail payment twice");
    }

    [Fact]
    public void Create_WithNegativeFee_Throws()
    {
        var act = () => Enrollment.Create(StudentId, SchoolId, -100m);
        act.Should().Throw<InvalidOperationException>();
    }
}
