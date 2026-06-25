using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DriveEase.Enrollments.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxRetryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DeadLettered",
                schema: "enrollments",
                table: "OutboxMessages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                schema: "enrollments",
                table: "OutboxMessages",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeadLettered",
                schema: "enrollments",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                schema: "enrollments",
                table: "OutboxMessages");
        }
    }
}
