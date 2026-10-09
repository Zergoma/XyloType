using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace XyloType.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261009090757_AddUsersAndExerciseAttempts : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ExerciseAttempts",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                UserId = table.Column<int>(type: "INTEGER", nullable: false),
                ExerciseId = table.Column<Guid>(type: "TEXT", nullable: false),
                CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                DurationSeconds = table.Column<double>(type: "REAL", nullable: false),
                Characters = table.Column<int>(type: "INTEGER", nullable: false),
                CharactersWithError = table.Column<int>(type: "INTEGER", nullable: false),
                WrongKeyPresses = table.Column<int>(type: "INTEGER", nullable: false),
                WordsPerMinute = table.Column<double>(type: "REAL", nullable: false),
                Accuracy = table.Column<double>(type: "REAL", nullable: false),
                Score = table.Column<double>(type: "REAL", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExerciseAttempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExerciseAttempts_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ExerciseAttempts_UserId_ExerciseId",
            table: "ExerciseAttempts",
            columns: new[] { "UserId", "ExerciseId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ExerciseAttempts");

        migrationBuilder.DropTable(
            name: "Users");
    }
}
