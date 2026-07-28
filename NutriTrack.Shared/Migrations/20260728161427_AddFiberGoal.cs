using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutriTrack.Shared.Migrations
{
    /// <inheritdoc />
    public partial class AddFiberGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FiberGoalG",
                table: "UserPreferences",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiberGoalG",
                table: "UserPreferences");
        }
    }
}
