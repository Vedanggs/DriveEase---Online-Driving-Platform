using DriveEase.Enrollments.Application.Queries.GetEnrollment;
using DriveEase.Enrollments.Domain.Aggregates;
using DriveEase.Enrollments.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DriveEase.Enrollments.Application.Tests;

public sealed class GetEnrollmentQueryHandlerTests
{
    private readonly IEnrollmentRepository _repo = Substitute.For<IEnrollmentRepository>();
    private readonly GetEnrollmentHandler _sut;

    public GetEnrollmentQueryHandlerTests() => _sut = new GetEnrollmentHandler(_repo);

    [Fact]
    public async Task Handle_EnrollmentNotFound_ReturnsNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((Enrollment?)null);

        var result = await _sut.Handle(new GetEnrollmentQuery(Guid.NewGuid()), default);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EnrollmentExists_ReturnsDtoWithMatchingFields()
    {
        var studentId = Guid.NewGuid();
        var schoolId  = Guid.NewGuid();
        var enrollment = Enrollment.Create(studentId, schoolId, 750m);

        _repo.GetByIdAsync(enrollment.Id, Arg.Any<CancellationToken>())
             .Returns(enrollment);

        var result = await _sut.Handle(new GetEnrollmentQuery(enrollment.Id), default);

        result.Should().NotBeNull();
        result!.StudentId.Should().Be(studentId);
        result.DrivingSchoolId.Should().Be(schoolId);
        result.Fee.Should().Be(750m);
        result.Status.Should().Be("Pending");
        result.PaymentStatus.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_AfterPaymentConfirmed_DtoReflectsActiveStatus()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 500m);
        enrollment.ConfirmPayment();

        _repo.GetByIdAsync(enrollment.Id, Arg.Any<CancellationToken>())
             .Returns(enrollment);

        var result = await _sut.Handle(new GetEnrollmentQuery(enrollment.Id), default);

        result!.Status.Should().Be("Active");
        result.PaymentStatus.Should().Be("Paid");
        result.PaymentConfirmedAt.Should().NotBeNull();
    }
}
