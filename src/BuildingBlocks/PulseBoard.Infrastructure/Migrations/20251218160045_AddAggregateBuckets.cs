using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PulseBoard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAggregateBuckets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AggregateBuckets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Metric = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Interval = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    BucketStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DimensionKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Value = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AggregateBuckets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AggregateBuckets_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AggregateBuckets_Lookup",
                table: "AggregateBuckets",
                columns: new[] { "TenantId", "ProjectId", "Metric", "Interval", "BucketStartUtc", "DimensionKey" },
                unique: true,
                filter: "[DimensionKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateBuckets_ProjectId",
                table: "AggregateBuckets",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AggregateBuckets_TimeSeries",
                table: "AggregateBuckets",
                columns: new[] { "TenantId", "ProjectId", "Metric", "Interval", "BucketStartUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AggregateBuckets");
        }
    }
}
