using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrMohamedWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddViewCountersToVisitsAndResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DownloadCount",
                table: "TestResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "TestResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "PatientVisits",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DownloadCount",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "TestResults");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "PatientVisits");
        }
    }
}
