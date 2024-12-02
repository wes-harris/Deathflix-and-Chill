using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathflixAPI.Migrations
{
    /// <inheritdoc />
    public partial class FixTimestampColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfDeath",
                table: "Actors",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Actors",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfDeath",
                table: "Actors",
                type: "timestamp",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Actors",
                type: "timestamp",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);
        }
    }
}
