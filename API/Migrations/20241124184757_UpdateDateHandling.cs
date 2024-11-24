using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathflixAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDateHandling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movie_ReleaseDate",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movie_Title",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movie_TmdbId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_MovieCredit_ActorId_MovieId",
                table: "MovieCredits");

            migrationBuilder.DropIndex(
                name: "IX_DeathRecord_DateOfDeath",
                table: "DeathRecords");

            migrationBuilder.DropIndex(
                name: "IX_Actor_DateOfDeath",
                table: "Actors");

            migrationBuilder.DropIndex(
                name: "IX_Actor_Name",
                table: "Actors");

            migrationBuilder.DropIndex(
                name: "IX_Actor_TmdbId",
                table: "Actors");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfDeath",
                table: "DeathRecords",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfDeath",
                table: "Actors",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Actors",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamptz",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovieCredits_ActorId",
                table: "MovieCredits",
                column: "ActorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovieCredits_ActorId",
                table: "MovieCredits");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfDeath",
                table: "DeathRecords",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfDeath",
                table: "Actors",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateOfBirth",
                table: "Actors",
                type: "timestamptz",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movie_ReleaseDate",
                table: "Movies",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Movie_Title",
                table: "Movies",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Movie_TmdbId",
                table: "Movies",
                column: "TmdbId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovieCredit_ActorId_MovieId",
                table: "MovieCredits",
                columns: new[] { "ActorId", "MovieId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeathRecord_DateOfDeath",
                table: "DeathRecords",
                column: "DateOfDeath");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_DateOfDeath",
                table: "Actors",
                column: "DateOfDeath");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_Name",
                table: "Actors",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_TmdbId",
                table: "Actors",
                column: "TmdbId",
                unique: true);
        }
    }
}
