using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyOrganizer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseImportSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "due_date",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "external_courses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_instance = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    external_course_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    inactive_since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_courses", x => x.id);
                    table.CheckConstraint("ck_external_courses_state", "\"state\" IN ('Inactive', 'Active')");
                });

            migrationBuilder.CreateTable(
                name: "course_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_subscriptions", x => x.id);
                    table.UniqueConstraint("AK_course_subscriptions_id_external_course_id", x => new { x.id, x.external_course_id });
                    table.CheckConstraint("ck_course_subscriptions_state", "\"state\" IN ('Pending', 'Active', 'Ended')");
                    table.ForeignKey(
                        name: "FK_course_subscriptions_AspNetUsers_owner_id",
                        column: x => x.owner_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_subscriptions_external_courses_external_course_id",
                        column: x => x.external_course_id,
                        principalTable: "external_courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_subscriptions_modules_study_module_id",
                        column: x => x.study_module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "external_learning_contents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_content_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    media_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source_reference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    availability = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    signature_version = table.Column<int>(type: "integer", nullable: false),
                    signature_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_learning_contents", x => x.id);
                    table.UniqueConstraint("AK_external_learning_contents_id_external_course_id", x => new { x.id, x.external_course_id });
                    table.CheckConstraint("ck_external_learning_contents_availability", "\"availability\" IN ('Available', 'Unavailable')");
                    table.CheckConstraint("ck_external_learning_contents_type", "\"type\" IN ('File', 'Link', 'Activity')");
                    table.ForeignKey(
                        name: "FK_external_learning_contents_external_courses_external_course~",
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
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activation_subscription_id = table.Column<Guid>(type: "uuid", nullable: true),
                    error_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    new_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    unchanged_count = table.Column<int>(type: "integer", nullable: false),
                    unavailable_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_scan_runs", x => x.id);
                    table.UniqueConstraint("AK_scan_runs_id_external_course_id", x => new { x.id, x.external_course_id });
                    table.CheckConstraint("ck_scan_runs_counts_non_negative", "\"new_count\" >= 0 AND \"updated_count\" >= 0 AND \"unchanged_count\" >= 0 AND \"unavailable_count\" >= 0");
                    table.CheckConstraint("ck_scan_runs_status", "\"status\" IN ('Running', 'Succeeded', 'Failed', 'Cancelled', 'Expired')");
                    table.ForeignKey(
                        name: "FK_scan_runs_course_subscriptions_activation_subscription_id_e~",
                        columns: x => new { x.activation_subscription_id, x.external_course_id },
                        principalTable: "course_subscriptions",
                        principalColumns: new[] { "id", "external_course_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_scan_runs_external_courses_external_course_id",
                        column: x => x.external_course_id,
                        principalTable: "external_courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "subscription_content_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    course_subscription_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    study_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    confirmed_signature_version = table.Column<int>(type: "integer", nullable: false),
                    confirmed_signature_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_subscription_content_states", x => x.id);
                    table.CheckConstraint("ck_subscription_content_states_status", "\"status\" IN ('Imported', 'Dismissed')");
                    table.CheckConstraint("ck_subscription_content_states_task", "(\"status\" = 'Imported' AND \"study_task_id\" IS NOT NULL) OR (\"status\" = 'Dismissed' AND \"study_task_id\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_subscription_content_states_course_subscriptions_course_sub~",
                        columns: x => new { x.course_subscription_id, x.external_course_id },
                        principalTable: "course_subscriptions",
                        principalColumns: new[] { "id", "external_course_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscription_content_states_external_learning_contents_exte~",
                        columns: x => new { x.external_learning_content_id, x.external_course_id },
                        principalTable: "external_learning_contents",
                        principalColumns: new[] { "id", "external_course_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_subscription_content_states_tasks_study_task_id",
                        column: x => x.study_task_id,
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "course_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scan_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_current = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_snapshots", x => x.id);
                    table.UniqueConstraint("AK_course_snapshots_id_external_course_id", x => new { x.id, x.external_course_id });
                    table.ForeignKey(
                        name: "FK_course_snapshots_external_courses_external_course_id",
                        column: x => x.external_course_id,
                        principalTable: "external_courses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_course_snapshots_scan_runs_scan_run_id_external_course_id",
                        columns: x => new { x.scan_run_id, x.external_course_id },
                        principalTable: "scan_runs",
                        principalColumns: new[] { "id", "external_course_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "source_updates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_content_state_id = table.Column<Guid>(type: "uuid", nullable: false),
                    detected_signature_version = table.Column<int>(type: "integer", nullable: false),
                    detected_signature_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    detected_by_scan_run_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_updates", x => x.id);
                    table.ForeignKey(
                        name: "FK_source_updates_scan_runs_detected_by_scan_run_id",
                        column: x => x.detected_by_scan_run_id,
                        principalTable: "scan_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_source_updates_subscription_content_states_subscription_con~",
                        column: x => x.subscription_content_state_id,
                        principalTable: "subscription_content_states",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course_snapshot_items",
                columns: table => new
                {
                    course_snapshot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_course_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_content_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    media_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    source_reference = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    signature_version = table.Column<int>(type: "integer", nullable: false),
                    signature_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_snapshot_items", x => new { x.course_snapshot_id, x.external_learning_content_id });
                    table.ForeignKey(
                        name: "FK_course_snapshot_items_course_snapshots_course_snapshot_id_e~",
                        columns: x => new { x.course_snapshot_id, x.external_course_id },
                        principalTable: "course_snapshots",
                        principalColumns: new[] { "id", "external_course_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_snapshot_items_external_learning_contents_external_l~",
                        columns: x => new { x.external_learning_content_id, x.external_course_id },
                        principalTable: "external_learning_contents",
                        principalColumns: new[] { "id", "external_course_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_course_snapshot_items_course_snapshot_id_external_course_id",
                table: "course_snapshot_items",
                columns: new[] { "course_snapshot_id", "external_course_id" });

            migrationBuilder.CreateIndex(
                name: "IX_course_snapshot_items_external_learning_content_id_external~",
                table: "course_snapshot_items",
                columns: new[] { "external_learning_content_id", "external_course_id" });

            migrationBuilder.CreateIndex(
                name: "ux_course_snapshot_items_snapshot_key",
                table: "course_snapshot_items",
                columns: new[] { "course_snapshot_id", "external_content_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_snapshots_scan_run_id_external_course_id",
                table: "course_snapshots",
                columns: new[] { "scan_run_id", "external_course_id" });

            migrationBuilder.CreateIndex(
                name: "ux_course_snapshots_current_course",
                table: "course_snapshots",
                column: "external_course_id",
                unique: true,
                filter: "\"is_current\"");

            migrationBuilder.CreateIndex(
                name: "ux_course_snapshots_scan_run_id",
                table: "course_snapshots",
                column: "scan_run_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_subscriptions_external_course_id",
                table: "course_subscriptions",
                column: "external_course_id");

            migrationBuilder.CreateIndex(
                name: "ux_course_subscriptions_owner_course",
                table: "course_subscriptions",
                columns: new[] { "owner_id", "external_course_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_course_subscriptions_study_module_id",
                table: "course_subscriptions",
                column: "study_module_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_external_courses_identity",
                table: "external_courses",
                columns: new[] { "source_type", "source_instance", "external_course_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_external_learning_contents_course_key",
                table: "external_learning_contents",
                columns: new[] { "external_course_id", "external_content_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_scan_runs_activation_subscription_id_external_course_id",
                table: "scan_runs",
                columns: new[] { "activation_subscription_id", "external_course_id" });

            migrationBuilder.CreateIndex(
                name: "ux_scan_runs_running_course",
                table: "scan_runs",
                column: "external_course_id",
                unique: true,
                filter: "\"status\" = 'Running'");

            migrationBuilder.CreateIndex(
                name: "IX_source_updates_detected_by_scan_run_id",
                table: "source_updates",
                column: "detected_by_scan_run_id");

            migrationBuilder.CreateIndex(
                name: "ux_source_updates_subscription_content_state",
                table: "source_updates",
                column: "subscription_content_state_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_subscription_content_states_course_subscription_id_external~",
                table: "subscription_content_states",
                columns: new[] { "course_subscription_id", "external_course_id" });

            migrationBuilder.CreateIndex(
                name: "IX_subscription_content_states_external_learning_content_id_ex~",
                table: "subscription_content_states",
                columns: new[] { "external_learning_content_id", "external_course_id" });

            migrationBuilder.CreateIndex(
                name: "ux_subscription_content_states_study_task",
                table: "subscription_content_states",
                column: "study_task_id",
                unique: true,
                filter: "\"study_task_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_subscription_content_states_subscription_content",
                table: "subscription_content_states",
                columns: new[] { "course_subscription_id", "external_learning_content_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_modules_AspNetUsers_owner_id",
                table: "modules",
                column: "owner_id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_modules_AspNetUsers_owner_id",
                table: "modules");

            migrationBuilder.DropTable(
                name: "course_snapshot_items");

            migrationBuilder.DropTable(
                name: "source_updates");

            migrationBuilder.DropTable(
                name: "course_snapshots");

            migrationBuilder.DropTable(
                name: "subscription_content_states");

            migrationBuilder.DropTable(
                name: "scan_runs");

            migrationBuilder.DropTable(
                name: "external_learning_contents");

            migrationBuilder.DropTable(
                name: "course_subscriptions");

            migrationBuilder.DropTable(
                name: "external_courses");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "due_date",
                table: "tasks",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
