using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StravaEditBotApi.Migrations;

/// <inheritdoc />
public partial class AddStravaProfileToAppUser : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StravaFirstname",
            table: "AspNetUsers",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StravaLastname",
            table: "AspNetUsers",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StravaProfile",
            table: "AspNetUsers",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StravaProfileMedium",
            table: "AspNetUsers",
            type: "nvarchar(max)",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "StravaFirstname",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "StravaLastname",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "StravaProfile",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "StravaProfileMedium",
            table: "AspNetUsers");
    }
}
