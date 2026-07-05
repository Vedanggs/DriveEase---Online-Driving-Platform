using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveEase.Schools.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorEmailAndPasswordHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "schools",
                table: "Instructors",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                schema: "schools",
                table: "Instructors",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                schema: "schools",
                table: "Instructors");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                schema: "schools",
                table: "Instructors");
        }
    }
}
