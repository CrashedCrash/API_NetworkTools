using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API_NetworkTools.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateForUrlShortener : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShortUrlMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ShortCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    LongUrl = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClickCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShortUrlMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShortUrlMappings_ShortCode",
                table: "ShortUrlMappings",
                column: "ShortCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShortUrlMappings");
        }
    }
}
