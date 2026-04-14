using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StravaEditBotApi.Migrations;

/// <inheritdoc />
public partial class AddStravaFieldsToAppUser : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "StravaAccessToken",
            table: "AspNetUsers",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "StravaAthleteId",
            table: "AspNetUsers",
            type: "bigint",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StravaRefreshToken",
            table: "AspNetUsers",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "StravaTokenExpiresAt",
            table: "AspNetUsers",
            type: "datetime2",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "StravaAccessToken",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "StravaAthleteId",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "StravaRefreshToken",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "StravaTokenExpiresAt",
            table: "AspNetUsers");
    }
}
