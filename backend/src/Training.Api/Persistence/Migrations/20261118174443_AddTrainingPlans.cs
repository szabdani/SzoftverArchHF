using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Training.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "planned_workout_id",
                table: "workouts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "training_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    runner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    goal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_training_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "planned_workouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    runner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    planned_distance_km = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    planned_duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    target_rpe = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    workout_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planned_workouts", x => x.id);
                    table.ForeignKey(
                        name: "fk_planned_workouts_training_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "training_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_planned_workouts_plan_id",
                table: "planned_workouts",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_planned_workouts_runner_id_date",
                table: "planned_workouts",
                columns: new[] { "runner_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_training_plans_runner_id",
                table: "training_plans",
                column: "runner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planned_workouts");

            migrationBuilder.DropTable(
                name: "training_plans");

            migrationBuilder.DropColumn(
                name: "planned_workout_id",
                table: "workouts");
        }
    }
}
