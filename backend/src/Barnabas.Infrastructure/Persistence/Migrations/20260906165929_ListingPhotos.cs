using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Barnabas.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ListingPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PhotoId",
                table: "Listings",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoId",
                table: "Listings");
        }
    }
}
