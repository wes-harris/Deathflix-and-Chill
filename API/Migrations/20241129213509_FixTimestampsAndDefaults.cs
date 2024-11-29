using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathflixAPI.Migrations
{
    public partial class FixTimestampsAndDefaults : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix LastDeathCheck infinity values
            migrationBuilder.Sql(
                @"UPDATE ""Actors"" 
                  SET ""LastDeathCheck"" = TIMESTAMP WITH TIME ZONE '0001-01-01 00:00:00+00' 
                  WHERE ""LastDeathCheck"" = '-infinity' OR ""LastDeathCheck"" IS NULL");

            // Fix LastDetailsCheck infinity values
            migrationBuilder.Sql(
                @"UPDATE ""Actors"" 
                  SET ""LastDetailsCheck"" = TIMESTAMP WITH TIME ZONE '0001-01-01 00:00:00+00' 
                  WHERE ""LastDetailsCheck"" = '-infinity' OR ""LastDetailsCheck"" IS NULL");

            // Set default values for new records
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastDeathCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "TIMESTAMP WITH TIME ZONE '0001-01-01 00:00:00+00'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastDetailsCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "TIMESTAMP WITH TIME ZONE '0001-01-01 00:00:00+00'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "LastDeathCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "TIMESTAMP WITH TIME ZONE '0001-01-01 00:00:00+00'");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastDetailsCheck",
                table: "Actors",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "TIMESTAMP WITH TIME ZONE '0001-01-01 00:00:00+00'");
        }
    }
}