using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathflixAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddActorTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastDeathCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDetailsCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDeathCheck",
                table: "Actors");

            migrationBuilder.DropColumn(
                name: "LastDetailsCheck",
                table: "Actors");
        }
    }
}
