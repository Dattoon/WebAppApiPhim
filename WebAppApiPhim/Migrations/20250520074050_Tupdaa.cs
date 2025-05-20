using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppApiPhim.Migrations
{
    /// <inheritdoc />
    public partial class Tupdaa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CachedEpisodes_CachedMovies_MovieId",
                table: "CachedEpisodes");

            migrationBuilder.DropIndex(
                name: "IX_CachedEpisodes_MovieId",
                table: "CachedEpisodes");

            migrationBuilder.DropColumn(
                name: "MovieId",
                table: "CachedEpisodes");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Movies",
                newName: "Quality");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "Movies",
                newName: "OriginalName");

            migrationBuilder.AddColumn<string>(
                name: "Actors",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Director",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "Movies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Movies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "MovieSlug",
                table: "CachedEpisodes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EpisodeSlug",
                table: "CachedEpisodes",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CreatedAt",
                table: "Comments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CachedEpisodes_MovieSlug_EpisodeSlug",
                table: "CachedEpisodes",
                columns: new[] { "MovieSlug", "EpisodeSlug" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Comments_CreatedAt",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_CachedEpisodes_MovieSlug_EpisodeSlug",
                table: "CachedEpisodes");

            migrationBuilder.DropColumn(
                name: "Actors",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Director",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Movies");

            migrationBuilder.RenameColumn(
                name: "Quality",
                table: "Movies",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "OriginalName",
                table: "Movies",
                newName: "Country");

            migrationBuilder.AlterColumn<string>(
                name: "MovieSlug",
                table: "CachedEpisodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "EpisodeSlug",
                table: "CachedEpisodes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<int>(
                name: "MovieId",
                table: "CachedEpisodes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_CachedEpisodes_MovieId",
                table: "CachedEpisodes",
                column: "MovieId");

            migrationBuilder.AddForeignKey(
                name: "FK_CachedEpisodes_CachedMovies_MovieId",
                table: "CachedEpisodes",
                column: "MovieId",
                principalTable: "CachedMovies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
