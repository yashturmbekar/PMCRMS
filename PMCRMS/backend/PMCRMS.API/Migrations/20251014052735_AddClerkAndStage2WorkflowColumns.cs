using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddClerkAndStage2WorkflowColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEArchitectId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAELicenceId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEStructuralId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor1Id",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor2Id",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedCityEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedExecutiveEngineerId",
                table: "PositionApplications");

            migrationBuilder.AddColumn<int>(
                name: "AssignedCEStage2Id",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedClerkId",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedEEStage2Id",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToCEStage2Date",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToClerkDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToEEStage2Date",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CEStage2DigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CEStage2DigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClerkApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClerkApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClerkApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClerkRejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClerkRejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ClerkRejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EEStage2DigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "EEStage2DigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedCEStage2Id",
                table: "PositionApplications",
                column: "AssignedCEStage2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedClerkId",
                table: "PositionApplications",
                column: "AssignedClerkId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedEEStage2Id",
                table: "PositionApplications",
                column: "AssignedEEStage2Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEArchitectId",
                table: "PositionApplications",
                column: "AssignedAEArchitectId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAELicenceId",
                table: "PositionApplications",
                column: "AssignedAELicenceId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEStructuralId",
                table: "PositionApplications",
                column: "AssignedAEStructuralId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor1Id",
                table: "PositionApplications",
                column: "AssignedAESupervisor1Id",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor2Id",
                table: "PositionApplications",
                column: "AssignedAESupervisor2Id",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedCEStage2Id",
                table: "PositionApplications",
                column: "AssignedCEStage2Id",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedCityEngineerId",
                table: "PositionApplications",
                column: "AssignedCityEngineerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedClerkId",
                table: "PositionApplications",
                column: "AssignedClerkId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedEEStage2Id",
                table: "PositionApplications",
                column: "AssignedEEStage2Id",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedExecutiveEngineerId",
                table: "PositionApplications",
                column: "AssignedExecutiveEngineerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEArchitectId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAELicenceId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEStructuralId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor1Id",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor2Id",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedCEStage2Id",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedCityEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedClerkId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedEEStage2Id",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedExecutiveEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedCEStage2Id",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedClerkId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedEEStage2Id",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedCEStage2Id",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedClerkId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedEEStage2Id",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToCEStage2Date",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToClerkDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToEEStage2Date",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CEStage2DigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CEStage2DigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ClerkApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ClerkApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ClerkApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ClerkRejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ClerkRejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ClerkRejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "EEStage2DigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "EEStage2DigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEArchitectId",
                table: "PositionApplications",
                column: "AssignedAEArchitectId",
                principalTable: "Officers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAELicenceId",
                table: "PositionApplications",
                column: "AssignedAELicenceId",
                principalTable: "Officers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAEStructuralId",
                table: "PositionApplications",
                column: "AssignedAEStructuralId",
                principalTable: "Officers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor1Id",
                table: "PositionApplications",
                column: "AssignedAESupervisor1Id",
                principalTable: "Officers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedAESupervisor2Id",
                table: "PositionApplications",
                column: "AssignedAESupervisor2Id",
                principalTable: "Officers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedCityEngineerId",
                table: "PositionApplications",
                column: "AssignedCityEngineerId",
                principalTable: "Officers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedExecutiveEngineerId",
                table: "PositionApplications",
                column: "AssignedExecutiveEngineerId",
                principalTable: "Officers",
                principalColumn: "Id");
        }
    }
}
