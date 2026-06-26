using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace URMS.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomisedDataToGradesheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomisedData",
                table: "Gradesheets",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomisedData",
                table: "Gradesheets");
        }
    }
}
