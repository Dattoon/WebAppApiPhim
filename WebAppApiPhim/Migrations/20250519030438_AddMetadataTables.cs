using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppApiPhim.Migrations
{
    /// <inheritdoc />
    public partial class AddMetadataTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ratings_UserId_MovieSlug",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId_MovieSlug",
                table: "Favorites");

            migrationBuilder.AlterColumn<string>(
                name: "MovieSlug",
                table: "WatchHistories",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MovieSlug",
                table: "Comments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApiValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApiValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovieTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApiValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PosterUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ThumbUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Year = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MovieTypeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Slug);
                    table.ForeignKey(
                        name: "FK_Movies_MovieTypes_MovieTypeId",
                        column: x => x.MovieTypeId,
                        principalTable: "MovieTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MovieCountries",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieCountries", x => new { x.MovieSlug, x.CountryId });
                    table.ForeignKey(
                        name: "FK_MovieCountries_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieCountries_Movies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "Movies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieGenres",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GenreId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenres", x => new { x.MovieSlug, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MovieGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieGenres_Movies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "Movies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WatchHistories_MovieSlug",
                table: "WatchHistories",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_MovieSlug",
                table: "Ratings",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId",
                table: "Ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_MovieSlug",
                table: "Favorites",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_MovieSlug",
                table: "Comments",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                table: "Countries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MovieCountries_CountryId",
                table: "MovieCountries",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenres_GenreId",
                table: "MovieGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_MovieTypeId",
                table: "Movies",
                column: "MovieTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieTypes_Name",
                table: "MovieTypes",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Movies_MovieSlug",
                table: "Comments",
                column: "MovieSlug",
                principalTable: "Movies",
                principalColumn: "Slug",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_Movies_MovieSlug",
                table: "Favorites",
                column: "MovieSlug",
                principalTable: "Movies",
                principalColumn: "Slug",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Ratings_Movies_MovieSlug",
                table: "Ratings",
                column: "MovieSlug",
                principalTable: "Movies",
                principalColumn: "Slug",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WatchHistories_Movies_MovieSlug",
                table: "WatchHistories",
                column: "MovieSlug",
                principalTable: "Movies",
                principalColumn: "Slug",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Movies_MovieSlug",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_Movies_MovieSlug",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_Ratings_Movies_MovieSlug",
                table: "Ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_WatchHistories_Movies_MovieSlug",
                table: "WatchHistories");

            migrationBuilder.DropTable(
                name: "MovieCountries");

            migrationBuilder.DropTable(
                name: "MovieGenres");

            migrationBuilder.DropTable(
                name: "Countries");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Movies");

            migrationBuilder.DropTable(
                name: "MovieTypes");

            migrationBuilder.DropIndex(
                name: "IX_WatchHistories_MovieSlug",
                table: "WatchHistories");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_MovieSlug",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_UserId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_MovieSlug",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_UserId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Comments_MovieSlug",
                table: "Comments");

            migrationBuilder.AlterColumn<string>(
                name: "MovieSlug",
                table: "WatchHistories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "MovieSlug",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId_MovieSlug",
                table: "Ratings",
                columns: new[] { "UserId", "MovieSlug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_UserId_MovieSlug",
                table: "Favorites",
                columns: new[] { "UserId", "MovieSlug" },
                unique: true);
        }
    }
}
