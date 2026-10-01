using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XyloType.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261001075311_AddImportedSourcesAndWordExclusion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsExcluded",
            table: "Words",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateTable(
            name: "ImportedSources",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Title = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                ImportedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                WordsRead = table.Column<int>(type: "INTEGER", nullable: false),
                NewWords = table.Column<int>(type: "INTEGER", nullable: false),
                UpdatedWords = table.Column<int>(type: "INTEGER", nullable: false),
                IgnoredWords = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ImportedSources", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Words_IsExcluded",
            table: "Words",
            column: "IsExcluded");

        migrationBuilder.CreateIndex(
            name: "IX_ImportedSources_ContentHash",
            table: "ImportedSources",
            column: "ContentHash");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ImportedSources");

        migrationBuilder.DropIndex(
            name: "IX_Words_IsExcluded",
            table: "Words");

        migrationBuilder.DropColumn(
            name: "IsExcluded",
            table: "Words");
    }
}
