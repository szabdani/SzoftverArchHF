using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Analytics.Api.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRuleSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rule_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    acwr_critical = table.Column<double>(type: "double precision", nullable: false),
                    acwr_elevated = table.Column<double>(type: "double precision", nullable: false),
                    weekly_km_increase_percent = table.Column<int>(type: "integer", nullable: false),
                    intense_rpe = table.Column<int>(type: "integer", nullable: false),
                    intense_days_per_week = table.Column<int>(type: "integer", nullable: false),
                    missed_workouts_per_week = table.Column<int>(type: "integer", nullable: false),
                    acwr_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    weekly_km_increase_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    intense_days_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    no_rest_day_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    missed_workouts_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_settings", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rule_settings");
        }
    }
}
