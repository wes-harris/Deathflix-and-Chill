using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathflixAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixLastDeathCheckDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing -infinity values to MinValue
            migrationBuilder.Sql(
                @"UPDATE ""Actors"" 
                  SET ""LastDeathCheck"" = '0001-01-01 00:00:00+00' 
                  WHERE ""LastDeathCheck"" = '-infinity'");

            // Set default value for new records
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastDeathCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastDeathCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}