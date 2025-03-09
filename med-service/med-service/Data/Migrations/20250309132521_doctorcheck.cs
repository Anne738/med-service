using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace med_service.Data.Migrations
{
    /// <inheritdoc />
    public partial class doctorcheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkingHours",
                table: "Doctors");

            migrationBuilder.AddColumn<int>(
                name: "WorkDayEnd",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkDayStart",
                table: "Doctors",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkDayEnd",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "WorkDayStart",
                table: "Doctors");

            migrationBuilder.AddColumn<string>(
                name: "WorkingHours",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
