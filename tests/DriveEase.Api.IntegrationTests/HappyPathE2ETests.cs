using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace DriveEase.Api.IntegrationTests;

/// <summary>
/// Full happy-path E2E:
///   Register → Login → Enroll → Pay → Assign Instructor → Book Lesson → Complete Lesson
///
/// Uses the real auth flow (register + login via AuthController) so JWT issuance is tested end-to-end.
/// </summary>
[Collection("IntegrationTests")]
public sealed class HappyPathE2ETests(DriveEaseWebApplicationFactory factory, ITestOutputHelper output)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _anon = factory.CreateClient();

    [Fact]
    public async Task StudentRegistration_Enrollment_Payment_LessonBooking_Completion_Flow()
    {
        // Unique credentials per run — DB persists for the factory lifetime
        var email    = $"{Guid.NewGuid():N}@e2e.test";
        var password = "E2eP@ssw0rd!";
        var fullName = "E2E TestStudent";

        // ── Step 1: Register student (public endpoint) ────────────────────────
        var registerId = await PostJsonAsync<IdResponse>(_anon,
            "/api/v1/auth/register",
            new { FullName = fullName, Email = email, Password = password, DateOfBirth = "2000-01-01" });

        registerId.Should().NotBeNull();
        output.WriteLine($"Registered student {registerId!.Id}");

        // ── Step 2: Login to get JWT ──────────────────────────────────────────
        var login = await PostJsonAsync<LoginResponse>(_anon,
            "/api/v1/auth/login",
            new { Email = email, Password = password });

        login.Should().NotBeNull();
        login!.AccessToken.Should().NotBeNullOrEmpty();
        output.WriteLine($"JWT issued for student {login.StudentId}");

        var authClient = factory.CreateClient();
        authClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        // ── Step 3: Get schools, pick the first one ───────────────────────────
        var schools = await GetJsonAsync<JsonElement[]>(authClient, "/api/v1/schools");
        schools.Should().NotBeNullOrEmpty();

        var schoolId = schools![0].GetProperty("id").GetString()!;
        output.WriteLine($"Enrolling in school {schoolId}");

        // ── Step 4: Enroll ────────────────────────────────────────────────────
        var enrollment = await PostJsonAsync<IdResponse>(authClient,
            "/api/v1/enrollments",
            new { StudentId = login.StudentId, DrivingSchoolId = schoolId, Fee = 500 });

        enrollment.Should().NotBeNull();
        var enrollmentId = enrollment!.Id;
        output.WriteLine($"Enrollment created {enrollmentId}");

        // ── Step 5: Process payment ───────────────────────────────────────────
        var paymentResp = await authClient.PostAsync($"/api/v1/enrollments/{enrollmentId}/payment", null);
        paymentResp.StatusCode.Should().Be(HttpStatusCode.OK);
        output.WriteLine("Payment processed");

        // ── Step 6: Get instructors for the school ────────────────────────────
        var instructors = await GetJsonAsync<JsonElement[]>(_anon, $"/api/v1/schools/{schoolId}/instructors");
        instructors.Should().NotBeNullOrEmpty();
        var instructorId   = instructors![0].GetProperty("id").GetString()!;
        var instructorName = instructors![0].GetProperty("fullName").GetString()!;
        output.WriteLine($"Assigned instructor {instructorId} ({instructorName})");

        // ── Step 7: Assign instructor to enrollment ───────────────────────────
        var assignResp = await PostJsonAsync<object?>(authClient,
            $"/api/v1/enrollments/{enrollmentId}/instructor",
            new { InstructorId = instructorId });
        // 204 No Content expected
        output.WriteLine("Instructor assigned");

        // ── Step 8: Book a lesson ─────────────────────────────────────────────
        // Jittered rather than a fixed +48h offset — a hardcoded offset can collide with
        // another lesson at the same instructor+time (seen intermittently in this suite),
        // since HasConflictAsync checks a multi-hour window around the requested slot.
        var scheduledAt = DateTime.UtcNow.AddHours(48).AddMinutes(Random.Shared.Next(1, 500)).ToString("o");
        var lesson = await PostJsonAsync<IdResponse>(authClient,
            "/api/v1/lessons",
            new
            {
                EnrollmentId   = enrollmentId,
                StudentId      = login.StudentId,
                StudentName    = fullName,
                InstructorId   = instructorId,
                InstructorName = instructorName,
                ScheduledAt    = scheduledAt,
                Duration       = "01:00:00",
            });

        lesson.Should().NotBeNull();
        var lessonId = lesson!.Id;
        output.WriteLine($"Lesson booked {lessonId}");

        // ── Step 9: Complete the lesson (requires the assigned instructor's token) ──
        var instructorClient = factory.CreateInstructorClient(Guid.Parse(instructorId));
        var completeResp = await PostJsonAsync<object?>(instructorClient,
            $"/api/v1/lessons/{lessonId}/complete",
            new { Notes = "Excellent drive — no incidents." });

        output.WriteLine("Lesson completed");

        // Verify the lesson is now Complete
        var lessonDto = await GetJsonAsync<JsonElement>(authClient, $"/api/v1/lessons/{lessonId}");
        lessonDto.GetProperty("status").GetString().Should().Be("Completed");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static async Task<T?> PostJsonAsync<T>(HttpClient client, string url, object body)
    {
        var json     = JsonSerializer.Serialize(body, JsonOpts);
        var content  = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        return string.IsNullOrWhiteSpace(raw) ? default : JsonSerializer.Deserialize<T>(raw, JsonOpts);
    }

    private static async Task<T?> GetJsonAsync<T>(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(raw, JsonOpts);
    }

    private sealed record IdResponse(Guid Id);
    private sealed record LoginResponse(string AccessToken, string RefreshToken, Guid StudentId, string FullName, string Email);
}
