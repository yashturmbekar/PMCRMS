using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameToPositionApplications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SEAddresses_StructuralEngineerApplications_ApplicationId",
                table: "SEAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_SEDocuments_StructuralEngineerApplications_ApplicationId",
                table: "SEDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_SEExperiences_StructuralEngineerApplications_ApplicationId",
                table: "SEExperiences");

            migrationBuilder.DropForeignKey(
                name: "FK_SEQualifications_StructuralEngineerApplications_Application~",
                table: "SEQualifications");

            // Rename the table instead of dropping and recreating
            migrationBuilder.RenameTable(
                name: "StructuralEngineerApplications",
                newName: "PositionApplications");

            // Rename indexes
            migrationBuilder.RenameIndex(
                name: "IX_StructuralEngineerApplications_ApplicationNumber",
                table: "PositionApplications",
                newName: "IX_PositionApplications_ApplicationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_StructuralEngineerApplications_UserId",
                table: "PositionApplications",
                newName: "IX_PositionApplications_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SEAddresses_PositionApplications_ApplicationId",
                table: "SEAddresses",
                column: "ApplicationId",
                principalTable: "PositionApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SEDocuments_PositionApplications_ApplicationId",
                table: "SEDocuments",
                column: "ApplicationId",
                principalTable: "PositionApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SEExperiences_PositionApplications_ApplicationId",
                table: "SEExperiences",
                column: "ApplicationId",
                principalTable: "PositionApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SEQualifications_PositionApplications_ApplicationId",
                table: "SEQualifications",
                column: "ApplicationId",
                principalTable: "PositionApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SEAddresses_PositionApplications_ApplicationId",
                table: "SEAddresses");

            migrationBuilder.DropForeignKey(
                name: "FK_SEDocuments_PositionApplications_ApplicationId",
                table: "SEDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_SEExperiences_PositionApplications_ApplicationId",
                table: "SEExperiences");

            migrationBuilder.DropForeignKey(
                name: "FK_SEQualifications_PositionApplications_ApplicationId",
                table: "SEQualifications");

            // Rename the table back
            migrationBuilder.RenameTable(
                name: "PositionApplications",
                newName: "StructuralEngineerApplications");

            // Rename indexes back
            migrationBuilder.RenameIndex(
                name: "IX_PositionApplications_ApplicationNumber",
                table: "StructuralEngineerApplications",
                newName: "IX_StructuralEngineerApplications_ApplicationNumber");

            migrationBuilder.RenameIndex(
                name: "IX_PositionApplications_UserId",
                table: "StructuralEngineerApplications",
                newName: "IX_StructuralEngineerApplications_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SEAddresses_StructuralEngineerApplications_ApplicationId",
                table: "SEAddresses",
                column: "ApplicationId",
                principalTable: "StructuralEngineerApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SEDocuments_StructuralEngineerApplications_ApplicationId",
                table: "SEDocuments",
                column: "ApplicationId",
                principalTable: "StructuralEngineerApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SEExperiences_StructuralEngineerApplications_ApplicationId",
                table: "SEExperiences",
                column: "ApplicationId",
                principalTable: "StructuralEngineerApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SEQualifications_StructuralEngineerApplications_Application~",
                table: "SEQualifications",
                column: "ApplicationId",
                principalTable: "StructuralEngineerApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
