using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationFormTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRecommendationFormGenerated",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecommendationFormGeneratedDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecommendationFormGenerationAttempts",
                table: "PositionApplications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RecommendationFormGenerationError",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRecommendationFormGenerated",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "RecommendationFormGeneratedDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "RecommendationFormGenerationAttempts",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "RecommendationFormGenerationError",
                table: "PositionApplications");
        }
    }
}
