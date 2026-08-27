using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyOrganizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCourseCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "metadata_purged_at",
                table: "external_learning_contents",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "metadata_purged_at",
                table: "external_learning_contents");
        }
    }
}
