using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyOrganizer.Infrastructure.Persistence.Migrations;

/// <summary>
/// Keeps every Matt import record in a separate schema while activating the
/// Superpowers import model. Accounts, modules, and tasks remain in public.
/// </summary>
public partial class SuperpowersProductCutover : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // All statements run in EF's transaction. A missing or unexpected Matt
        // table aborts the whole migration instead of silently losing records.
        migrationBuilder.Sql("""
            CREATE SCHEMA legacy_matt;

            ALTER TABLE public.course_snapshot_items SET SCHEMA legacy_matt;
            ALTER TABLE public.source_updates SET SCHEMA legacy_matt;
            ALTER TABLE public.course_snapshots SET SCHEMA legacy_matt;
            ALTER TABLE public.subscription_content_states SET SCHEMA legacy_matt;
            ALTER TABLE public.external_learning_contents SET SCHEMA legacy_matt;
            ALTER TABLE public.scan_runs SET SCHEMA legacy_matt;
            ALTER TABLE public.course_subscriptions SET SCHEMA legacy_matt;
            ALTER TABLE public.external_courses SET SCHEMA legacy_matt;

            -- The archive keeps IDs but must not prevent owners from deleting
            -- their active modules, tasks, or accounts after the cutover.
            ALTER TABLE legacy_matt.course_subscriptions
                DROP CONSTRAINT "FK_course_subscriptions_AspNetUsers_owner_id";
            ALTER TABLE legacy_matt.course_subscriptions
                DROP CONSTRAINT "FK_course_subscriptions_modules_study_module_id";
            ALTER TABLE legacy_matt.subscription_content_states
                DROP CONSTRAINT "FK_subscription_content_states_tasks_study_task_id";
            """);

        migrationBuilder.Sql("""
            CREATE TABLE public.external_courses (
                id uuid CONSTRAINT "PK_external_courses" PRIMARY KEY,
                provider_key text NOT NULL,
                external_course_id text NOT NULL,
                name text NOT NULL,
                active_scan_run_id uuid NULL,
                last_successful_scan_at_utc timestamp with time zone NULL,
                created_at_utc timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX ix_external_courses_provider_key_external_course_id
                ON public.external_courses (provider_key, external_course_id);

            CREATE TABLE public.course_subscriptions (
                id uuid CONSTRAINT "PK_course_subscriptions" PRIMARY KEY,
                owner_id uuid NOT NULL,
                external_course_id uuid NOT NULL
                    CONSTRAINT "FK_course_subscriptions_external_courses_external_course_id"
                    REFERENCES public.external_courses(id) ON DELETE RESTRICT,
                module_id uuid NOT NULL
                    CONSTRAINT "FK_course_subscriptions_modules_module_id"
                    REFERENCES public.modules(id) ON DELETE RESTRICT,
                created_at_utc timestamp with time zone NOT NULL
            );
            CREATE INDEX "IX_course_subscriptions_external_course_id"
                ON public.course_subscriptions (external_course_id);
            CREATE INDEX "IX_course_subscriptions_module_id"
                ON public.course_subscriptions (module_id);
            CREATE UNIQUE INDEX ix_course_subscriptions_owner_id_external_course_id
                ON public.course_subscriptions (owner_id, external_course_id);

            CREATE TABLE public.external_contents (
                id uuid CONSTRAINT "PK_external_contents" PRIMARY KEY,
                external_course_id uuid NOT NULL
                    CONSTRAINT "FK_external_contents_external_courses_external_course_id"
                    REFERENCES public.external_courses(id) ON DELETE RESTRICT,
                provider_content_id text NOT NULL,
                kind integer NOT NULL,
                title text NOT NULL,
                description text NULL,
                source_url text NOT NULL,
                structured_due_date_utc timestamp with time zone NULL,
                processing_state integer NOT NULL,
                review_reason integer NOT NULL,
                visibility integer NOT NULL,
                last_seen_at_utc timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX ix_external_contents_external_course_id_provider_content_id
                ON public.external_contents (external_course_id, provider_content_id);

            CREATE TABLE public.scan_runs (
                id uuid CONSTRAINT "PK_scan_runs" PRIMARY KEY,
                external_course_id uuid NOT NULL
                    CONSTRAINT "FK_scan_runs_external_courses_external_course_id"
                    REFERENCES public.external_courses(id) ON DELETE CASCADE,
                requested_by_owner_id uuid NOT NULL,
                status integer NOT NULL,
                started_at_utc timestamp with time zone NOT NULL,
                finished_at_utc timestamp with time zone NULL,
                error_code text NULL
            );
            CREATE INDEX "IX_scan_runs_external_course_id"
                ON public.scan_runs (external_course_id);

            CREATE TABLE public.external_task_links (
                id uuid CONSTRAINT "PK_external_task_links" PRIMARY KEY,
                course_subscription_id uuid NOT NULL
                    CONSTRAINT "FK_external_task_links_course_subscriptions_course_subscriptio~"
                    REFERENCES public.course_subscriptions(id) ON DELETE CASCADE,
                external_content_id uuid NOT NULL
                    CONSTRAINT "FK_external_task_links_external_contents_external_content_id"
                    REFERENCES public.external_contents(id) ON DELETE CASCADE,
                task_id uuid NOT NULL
                    CONSTRAINT "FK_external_task_links_tasks_task_id"
                    REFERENCES public.tasks(id) ON DELETE CASCADE,
                created_at_utc timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX ix_external_task_links_course_subscription_id_external_content_id
                ON public.external_task_links (course_subscription_id, external_content_id);
            CREATE INDEX "IX_external_task_links_external_content_id"
                ON public.external_task_links (external_content_id);
            CREATE UNIQUE INDEX ix_external_task_links_task_id
                ON public.external_task_links (task_id);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "The Superpowers cutover contains archived Matt data. Restore a database backup instead of reversing this migration.");
    }
}
