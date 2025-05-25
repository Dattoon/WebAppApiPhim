using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppApiPhim.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CachedMovies",
                columns: table => new
                {
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PosterUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ThumbUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Year = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    Director = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RawData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TmdbId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrailerUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Rating = table.Column<double>(type: "float", nullable: true),
                    MovieSlug = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedMovies", x => x.Slug);
                });

            migrationBuilder.CreateTable(
                name: "MovieCountries",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieCountries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovieGenres",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MovieTypes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionCompanies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionCompanies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StreamingPlatforms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamingPlatforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CachedEpisodes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    EpisodeNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CachedEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CachedEpisodes_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyViews",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewCount = table.Column<long>(type: "bigint", nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyViews_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieActors",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieActors", x => new { x.MovieSlug, x.ActorId });
                    table.ForeignKey(
                        name: "FK_MovieActors_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovieActors_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieRatings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Rating = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovieRatings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieRatings_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieStatistics",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    FavoriteCount = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieStatistics", x => x.MovieSlug);
                    table.ForeignKey(
                        name: "FK_MovieStatistics_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserComments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserComments_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserFavorites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFavorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFavorites_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserFavorites_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMovies",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMovies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMovies_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserMovies_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovieCountryMappings",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    CountryId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieCountryMappings", x => new { x.MovieSlug, x.CountryId });
                    table.ForeignKey(
                        name: "FK_MovieCountryMappings_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieCountryMappings_MovieCountries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "MovieCountries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovieGenreMappings",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    GenreId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieGenreMappings", x => new { x.MovieSlug, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MovieGenreMappings_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieGenreMappings_MovieGenres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "MovieGenres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovieTypeMappings",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    TypeId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieTypeMappings", x => new { x.MovieSlug, x.TypeId });
                    table.ForeignKey(
                        name: "FK_MovieTypeMappings_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieTypeMappings_MovieTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "MovieTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovieProductionCompanies",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    ProductionCompanyId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieProductionCompanies", x => new { x.MovieSlug, x.ProductionCompanyId });
                    table.ForeignKey(
                        name: "FK_MovieProductionCompanies_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieProductionCompanies_ProductionCompanies_ProductionCompanyId",
                        column: x => x.ProductionCompanyId,
                        principalTable: "ProductionCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovieStreamingPlatforms",
                columns: table => new
                {
                    MovieSlug = table.Column<string>(type: "nvarchar(100)", nullable: false),
                    StreamingPlatformId = table.Column<string>(type: "nvarchar(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovieStreamingPlatforms", x => new { x.MovieSlug, x.StreamingPlatformId });
                    table.ForeignKey(
                        name: "FK_MovieStreamingPlatforms_CachedMovies_MovieSlug",
                        column: x => x.MovieSlug,
                        principalTable: "CachedMovies",
                        principalColumn: "Slug",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovieStreamingPlatforms_StreamingPlatforms_StreamingPlatformId",
                        column: x => x.StreamingPlatformId,
                        principalTable: "StreamingPlatforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeProgresses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EpisodeId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    WatchedPercentage = table.Column<double>(type: "float", nullable: false),
                    LastWatched = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeProgresses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EpisodeProgresses_CachedEpisodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "CachedEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpisodeServers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EpisodeId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServerUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpisodeServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpisodeServers_CachedEpisodes_EpisodeId",
                        column: x => x.EpisodeId,
                        principalTable: "CachedEpisodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CachedEpisodes_MovieSlug_EpisodeNumber",
                table: "CachedEpisodes",
                columns: new[] { "MovieSlug", "EpisodeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CachedMovies_Slug",
                table: "CachedMovies",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyViews_MovieSlug",
                table: "DailyViews",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeProgresses_EpisodeId",
                table: "EpisodeProgresses",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeProgresses_UserId_EpisodeId",
                table: "EpisodeProgresses",
                columns: new[] { "UserId", "EpisodeId" });

            migrationBuilder.CreateIndex(
                name: "IX_EpisodeServers_EpisodeId",
                table: "EpisodeServers",
                column: "EpisodeId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieActors_ActorId",
                table: "MovieActors",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieCountryMappings_CountryId",
                table: "MovieCountryMappings",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieGenreMappings_GenreId",
                table: "MovieGenreMappings",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieProductionCompanies_ProductionCompanyId",
                table: "MovieProductionCompanies",
                column: "ProductionCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatings_MovieSlug",
                table: "MovieRatings",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_MovieRatings_UserId_MovieSlug",
                table: "MovieRatings",
                columns: new[] { "UserId", "MovieSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_MovieStreamingPlatforms_StreamingPlatformId",
                table: "MovieStreamingPlatforms",
                column: "StreamingPlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_MovieTypeMappings_TypeId",
                table: "MovieTypeMappings",
                column: "TypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_MovieSlug",
                table: "UserComments",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_UserComments_UserId_MovieSlug",
                table: "UserComments",
                columns: new[] { "UserId", "MovieSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_MovieSlug",
                table: "UserFavorites",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_UserFavorites_UserId_MovieSlug",
                table: "UserFavorites",
                columns: new[] { "UserId", "MovieSlug" });

            migrationBuilder.CreateIndex(
                name: "IX_UserMovies_MovieSlug",
                table: "UserMovies",
                column: "MovieSlug");

            migrationBuilder.CreateIndex(
                name: "IX_UserMovies_UserId_MovieSlug",
                table: "UserMovies",
                columns: new[] { "UserId", "MovieSlug" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DailyViews");

            migrationBuilder.DropTable(
                name: "EpisodeProgresses");

            migrationBuilder.DropTable(
                name: "EpisodeServers");

            migrationBuilder.DropTable(
                name: "MovieActors");

            migrationBuilder.DropTable(
                name: "MovieCountryMappings");

            migrationBuilder.DropTable(
                name: "MovieGenreMappings");

            migrationBuilder.DropTable(
                name: "MovieProductionCompanies");

            migrationBuilder.DropTable(
                name: "MovieRatings");

            migrationBuilder.DropTable(
                name: "MovieStatistics");

            migrationBuilder.DropTable(
                name: "MovieStreamingPlatforms");

            migrationBuilder.DropTable(
                name: "MovieTypeMappings");

            migrationBuilder.DropTable(
                name: "UserComments");

            migrationBuilder.DropTable(
                name: "UserFavorites");

            migrationBuilder.DropTable(
                name: "UserMovies");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CachedEpisodes");

            migrationBuilder.DropTable(
                name: "Actors");

            migrationBuilder.DropTable(
                name: "MovieCountries");

            migrationBuilder.DropTable(
                name: "MovieGenres");

            migrationBuilder.DropTable(
                name: "ProductionCompanies");

            migrationBuilder.DropTable(
                name: "StreamingPlatforms");

            migrationBuilder.DropTable(
                name: "MovieTypes");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CachedMovies");
        }
    }
}
