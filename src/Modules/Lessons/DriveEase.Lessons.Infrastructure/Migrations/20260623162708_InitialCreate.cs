using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveEase.Lessons.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "lessons");

            migrationBuilder.CreateTable(
                name: "Lessons",
                schema: "lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentName = table.Column<string>(type: "TEXT", nullable: false),
                    InstructorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<double>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lessons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "lessons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Error = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_EnrollmentId",
                schema: "lessons",
                table: "Lessons",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_StudentId_ScheduledAt",
                schema: "lessons",
                table: "Lessons",
                columns: new[] { "StudentId", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAt",
                schema: "lessons",
                table: "OutboxMessages",
                column: "ProcessedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lessons",
                schema: "lessons");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "lessons");
        }
    }
}
