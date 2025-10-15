using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddEEStage2AndCEStage2ApprovalStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CEStage2ApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CEStage2ApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CEStage2ApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EEStage2ApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EEStage2ApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EEStage2ApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CEStage2ApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CEStage2ApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CEStage2ApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "EEStage2ApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "EEStage2ApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "EEStage2ApprovalStatus",
                table: "PositionApplications");
        }
    }
}
