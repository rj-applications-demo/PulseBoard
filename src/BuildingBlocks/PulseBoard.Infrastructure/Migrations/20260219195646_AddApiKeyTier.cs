using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseBoard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "ApiKeys",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tier",
                table: "ApiKeys");
        }
    }
}
