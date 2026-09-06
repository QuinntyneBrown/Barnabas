using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barnabas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReportAndReviewAListing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FlaggedAt",
                table: "Listings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RemovedAt",
                table: "Listings",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ListingReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CongregationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReportedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Listings_Flagged",
                table: "Listings",
                columns: new[] { "CongregationId", "FlaggedAt" },
                filter: "[FlaggedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ListingReports_CongregationId_ResolvedAt",
                table: "ListingReports",
                columns: new[] { "CongregationId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingReports_OneReportPerMember",
                table: "ListingReports",
                columns: new[] { "ListingId", "ReporterId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingReports");

            migrationBuilder.DropIndex(
                name: "IX_Listings_Flagged",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "FlaggedAt",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "RemovedAt",
                table: "Listings");
        }
    }
}
