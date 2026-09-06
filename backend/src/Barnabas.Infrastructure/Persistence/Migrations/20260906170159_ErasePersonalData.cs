using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barnabas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ErasePersonalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ErasedAt",
                table: "Members",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErasedAt",
                table: "Members");
        }
    }
}
