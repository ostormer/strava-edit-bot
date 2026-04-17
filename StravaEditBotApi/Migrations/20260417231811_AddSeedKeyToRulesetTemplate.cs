using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StravaEditBotApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSeedKeyToRulesetTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeedKey",
                table: "RulesetTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RulesetTemplates_SeedKey",
                table: "RulesetTemplates",
                column: "SeedKey",
                unique: true,
                filter: "[SeedKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RulesetTemplates_SeedKey",
                table: "RulesetTemplates");

            migrationBuilder.DropColumn(
                name: "SeedKey",
                table: "RulesetTemplates");
        }
    }
}
