using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyOrganizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "external_courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    external_course_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    active_scan_run_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_successful_scan_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_courses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "course_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_course_subscriptions_external_courses_external_course_id",
                        column: x => x.external_course_id,
                        principalTable: "external_courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_subscriptions_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_contents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_content_id = table.Column<string>(type: "text", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    source_url = table.Column<string>(type: "text", nullable: false),
                    structured_due_date_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    processing_state = table.Column<int>(type: "integer", nullable: false),
                    review_reason = table.Column<int>(type: "integer", nullable: false),
                    visibility = table.Column<int>(type: "integer", nullable: false),
                    last_seen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_contents", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_contents_external_courses_external_course_id",
                        column: x => x.external_course_id,
                        principalTable: "external_courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "scan_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_runs", x => x.id);
                    table.ForeignKey(
                        name: "FK_scan_runs_external_courses_external_course_id",
                        column: x => x.external_course_id,
                        principalTable: "external_courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "external_task_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_task_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_task_links_course_subscriptions_course_subscriptio~",
                        column: x => x.course_subscription_id,
                        principalTable: "course_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_task_links_external_contents_external_content_id",
                        column: x => x.external_content_id,
                        principalTable: "external_contents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_task_links_tasks_task_id",
                        column: x => x.task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_subscriptions_external_course_id",
                table: "course_subscriptions",
                column: "external_course_id");

            migrationBuilder.CreateIndex(
                name: "IX_course_subscriptions_module_id",
                table: "course_subscriptions",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "ix_course_subscriptions_owner_id_external_course_id",
                table: "course_subscriptions",
                columns: new[] { "owner_id", "external_course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_contents_external_course_id_provider_content_id",
                table: "external_contents",
                columns: new[] { "external_course_id", "provider_content_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_courses_provider_key_external_course_id",
                table: "external_courses",
                columns: new[] { "provider_key", "external_course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_external_task_links_course_subscription_id_external_content_id",
                table: "external_task_links",
                columns: new[] { "course_subscription_id", "external_content_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_task_links_external_content_id",
                table: "external_task_links",
                column: "external_content_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_task_links_task_id",
                table: "external_task_links",
                column: "task_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scan_runs_external_course_id",
                table: "scan_runs",
                column: "external_course_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "external_task_links");

            migrationBuilder.DropTable(
                name: "scan_runs");

            migrationBuilder.DropTable(
                name: "course_subscriptions");

            migrationBuilder.DropTable(
                name: "external_contents");

            migrationBuilder.DropTable(
                name: "external_courses");
        }
    }
}
