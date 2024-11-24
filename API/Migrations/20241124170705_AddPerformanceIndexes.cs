using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathflixAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MovieCredits_ActorId",
                table: "MovieCredits");

            migrationBuilder.RenameIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                newName: "IX_Movie_TmdbId");

            migrationBuilder.RenameIndex(
                name: "IX_Actors_TmdbId",
                table: "Actors",
                newName: "IX_Actor_TmdbId");

            migrationBuilder.RenameIndex(
                name: "IX_Actors_DateOfDeath",
                table: "Actors",
                newName: "IX_Actor_DateOfDeath");

            migrationBuilder.CreateIndex(
                name: "IX_Movie_ReleaseDate",
                table: "Movies",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Movie_Title",
                table: "Movies",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_MovieCredit_ActorId_MovieId",
                table: "MovieCredits",
                columns: new[] { "ActorId", "MovieId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeathRecord_DateOfDeath",
                table: "DeathRecords",
                column: "DateOfDeath");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_Name",
                table: "Actors",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movie_ReleaseDate",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movie_Title",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_MovieCredit_ActorId_MovieId",
                table: "MovieCredits");

            migrationBuilder.DropIndex(
                name: "IX_DeathRecord_DateOfDeath",
                table: "DeathRecords");

            migrationBuilder.DropIndex(
                name: "IX_Actor_Name",
                table: "Actors");

            migrationBuilder.RenameIndex(
                name: "IX_Movie_TmdbId",
                table: "Movies",
                newName: "IX_Movies_TmdbId");

            migrationBuilder.RenameIndex(
                name: "IX_Actor_TmdbId",
                table: "Actors",
                newName: "IX_Actors_TmdbId");

            migrationBuilder.RenameIndex(
                name: "IX_Actor_DateOfDeath",
                table: "Actors",
                newName: "IX_Actors_DateOfDeath");

            migrationBuilder.CreateIndex(
                name: "IX_MovieCredits_ActorId",
                table: "MovieCredits",
                column: "ActorId");
        }
    }
}
