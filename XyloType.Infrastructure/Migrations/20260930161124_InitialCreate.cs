using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XyloType.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20260930161124_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Words",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Text = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                LanguageCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                Length = table.Column<int>(type: "INTEGER", nullable: false),
                OccurrenceCount = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Words", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "WordAnalyses",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Layout = table.Column<int>(type: "INTEGER", nullable: false),
                UsesLeftHand = table.Column<bool>(type: "INTEGER", nullable: false),
                UsesRightHand = table.Column<bool>(type: "INTEGER", nullable: false),
                RowMask = table.Column<int>(type: "INTEGER", nullable: false),
                FingerMask = table.Column<int>(type: "INTEGER", nullable: false),
                ExternalAccent = table.Column<bool>(type: "INTEGER", nullable: false),
                WordId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WordAnalyses", x => x.Id);
                table.ForeignKey(
                    name: "FK_WordAnalyses_Words_WordId",
                    column: x => x.WordId,
                    principalTable: "Words",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WordAnalyses_Layout",
            table: "WordAnalyses",
            column: "Layout");

        migrationBuilder.CreateIndex(
            name: "IX_WordAnalyses_Layout_FingerMask",
            table: "WordAnalyses",
            columns: new[] { "Layout", "FingerMask" });

        migrationBuilder.CreateIndex(
            name: "IX_WordAnalyses_Layout_RowMask",
            table: "WordAnalyses",
            columns: new[] { "Layout", "RowMask" });

        migrationBuilder.CreateIndex(
            name: "IX_WordAnalyses_Layout_UsesLeftHand_UsesRightHand",
            table: "WordAnalyses",
            columns: new[] { "Layout", "UsesLeftHand", "UsesRightHand" });

        migrationBuilder.CreateIndex(
            name: "IX_WordAnalyses_WordId",
            table: "WordAnalyses",
            column: "WordId");

        migrationBuilder.CreateIndex(
            name: "IX_WordAnalyses_WordId_Layout",
            table: "WordAnalyses",
            columns: new[] { "WordId", "Layout" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Words_LanguageCode",
            table: "Words",
            column: "LanguageCode");

        migrationBuilder.CreateIndex(
            name: "IX_Words_Length",
            table: "Words",
            column: "Length");

        migrationBuilder.CreateIndex(
            name: "IX_Words_OccurrenceCount",
            table: "Words",
            column: "OccurrenceCount");

        migrationBuilder.CreateIndex(
            name: "IX_Words_Text_LanguageCode",
            table: "Words",
            columns: new[] { "Text", "LanguageCode" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WordAnalyses");

        migrationBuilder.DropTable(
            name: "Words");
    }
}
