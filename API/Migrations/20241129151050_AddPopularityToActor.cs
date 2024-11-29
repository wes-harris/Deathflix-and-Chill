using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeathflixAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPopularityToActor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Popularity",
                table: "Actors",
                type: "numeric(10,3)",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Popularity",
                table: "Actors");
        }
    }
}
