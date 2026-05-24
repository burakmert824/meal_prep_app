using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPrepper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProteinPerUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ProteinPerUnit",
                table: "Foods",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProteinPerUnit",
                table: "Foods");
        }
    }
}
