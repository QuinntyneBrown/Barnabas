using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barnabas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    CongregationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => new { x.MemberId, x.Kind });
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CongregationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    SubjectMemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubjectMemberDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ListingTitle = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ThreadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_RecipientId_CreatedAt",
                table: "Notifications",
                columns: new[] { "RecipientId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Unread",
                table: "Notifications",
                column: "RecipientId",
                filter: "[ReadAt] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
