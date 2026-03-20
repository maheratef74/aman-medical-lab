using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrMohamedWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAvailableToPatientVisit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "PatientVisits",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "PatientVisits");
        }
    }
}
