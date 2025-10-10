using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedOfficerToApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "Applications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedOfficerDesignation",
                table: "Applications",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedOfficerId",
                table: "Applications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedOfficerName",
                table: "Applications",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AssignedOfficerDesignation",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AssignedOfficerId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "AssignedOfficerName",
                table: "Applications");
        }
    }
}
