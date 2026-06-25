using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveEase.Notifications.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notifications");

            migrationBuilder.CreateTable(
                name: "InstructorNotifications",
                schema: "notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    InstructorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StudentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorNotifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstructorNotifications_InstructorId",
                schema: "notifications",
                table: "InstructorNotifications",
                column: "InstructorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstructorNotifications",
                schema: "notifications");
        }
    }
}
