using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseBoard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EventIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventRecords_EventId",
                table: "EventRecords");

            migrationBuilder.AlterColumn<string>(
                name: "EventId",
                table: "EventRecords",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_TenantId_EventId",
                table: "EventRecords",
                columns: new[] { "TenantId", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventRecords_TenantId_EventId",
                table: "EventRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "EventId",
                table: "EventRecords",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateIndex(
                name: "IX_EventRecords_EventId",
                table: "EventRecords",
                column: "EventId",
                unique: true);
        }
    }
}
