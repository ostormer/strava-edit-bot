using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StravaEditBotApi.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Activities",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ActivitySport = table.Column<string>(type: "nvarchar(max)", nullable: false),
                StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                Distance = table.Column<double>(type: "float", nullable: false),
                ElapsedTime = table.Column<TimeSpan>(type: "time", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Activities", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Activities");
    }
}
