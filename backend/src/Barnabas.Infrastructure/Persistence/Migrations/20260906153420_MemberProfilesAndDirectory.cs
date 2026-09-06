using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barnabas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MemberProfilesAndDirectory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Members",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LeftAt",
                table: "Members",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HelpTags",
                table: "Congregations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "MemberHelpTags",
                columns: table => new
                {
                    MemberId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberHelpTags", x => new { x.MemberId, x.Tag });
                    table.ForeignKey(
                        name: "FK_MemberHelpTags_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Members_CongregationId_Status_DisplayName",
                table: "Members",
                columns: new[] { "CongregationId", "Status", "DisplayName" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberHelpTags_Tag",
                table: "MemberHelpTags",
                column: "Tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberHelpTags");

            migrationBuilder.DropIndex(
                name: "IX_Members_CongregationId_Status_DisplayName",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "LeftAt",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "HelpTags",
                table: "Congregations");
        }
    }
}
