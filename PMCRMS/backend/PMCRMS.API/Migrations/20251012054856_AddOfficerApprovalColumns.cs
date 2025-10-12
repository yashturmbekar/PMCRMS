using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficerApprovalColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AEArchitectApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AEArchitectApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AEArchitectApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AEArchitectDigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AEArchitectDigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AEArchitectRejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AEArchitectRejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AEArchitectRejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AELicenceApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AELicenceApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AELicenceApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AELicenceDigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AELicenceDigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AELicenceRejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AELicenceRejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AELicenceRejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AEStructuralApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AEStructuralApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AEStructuralApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AEStructuralDigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AEStructuralDigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AEStructuralRejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AEStructuralRejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AEStructuralRejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AESupervisor1ApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AESupervisor1ApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AESupervisor1ApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AESupervisor1DigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AESupervisor1DigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AESupervisor1RejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AESupervisor1RejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AESupervisor1RejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AESupervisor2ApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AESupervisor2ApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AESupervisor2ApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AESupervisor2DigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AESupervisor2DigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AESupervisor2RejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AESupervisor2RejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AESupervisor2RejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedAEArchitectId",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedAELicenceId",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedAEStructuralId",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedAESupervisor1Id",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedAESupervisor2Id",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedCityEngineerId",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedExecutiveEngineerId",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToAEArchitectDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToAELicenceDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToAEStructuralDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToAESupervisor1Date",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToAESupervisor2Date",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToCityEngineerDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToExecutiveEngineerDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityEngineerApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CityEngineerApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CityEngineerApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CityEngineerDigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CityEngineerDigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CityEngineerRejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CityEngineerRejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CityEngineerRejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutiveEngineerApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutiveEngineerApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExecutiveEngineerApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExecutiveEngineerDigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutiveEngineerDigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutiveEngineerRejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExecutiveEngineerRejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ExecutiveEngineerRejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JEApprovalComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JEApprovalDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JEApprovalStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JERejectionComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JERejectionDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "JERejectionStatus",
                table: "PositionApplications",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedAEArchitectId",
                table: "PositionApplications",
                column: "AssignedAEArchitectId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedAELicenceId",
                table: "PositionApplications",
                column: "AssignedAELicenceId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedAEStructuralId",
                table: "PositionApplications",
                column: "AssignedAEStructuralId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedAESupervisor1Id",
                table: "PositionApplications",
                column: "AssignedAESupervisor1Id");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedAESupervisor2Id",
                table: "PositionApplications",
                column: "AssignedAESupervisor2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedCityEngineerId",
                table: "PositionApplications",
                column: "AssignedCityEngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedExecutiveEngineerId",
                table: "PositionApplications",
                column: "AssignedExecutiveEngineerId");

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
                name: "FK_PositionApplications_Officers_AssignedCityEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedExecutiveEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedAEArchitectId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedAELicenceId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedAEStructuralId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedAESupervisor1Id",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedAESupervisor2Id",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedCityEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedExecutiveEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectDigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectDigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectRejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectRejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEArchitectRejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceDigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceDigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceRejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceRejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AELicenceRejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralDigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralDigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralRejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralRejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AEStructuralRejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1ApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1ApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1ApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1DigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1DigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1RejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1RejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor1RejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2ApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2ApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2ApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2DigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2DigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2RejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2RejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AESupervisor2RejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedAEArchitectId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedAELicenceId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedAEStructuralId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedAESupervisor1Id",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedAESupervisor2Id",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedCityEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedExecutiveEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToAEArchitectDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToAELicenceDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToAEStructuralDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToAESupervisor1Date",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToAESupervisor2Date",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToCityEngineerDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToExecutiveEngineerDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerDigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerDigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerRejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerRejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "CityEngineerRejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerDigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerDigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerRejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerRejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "ExecutiveEngineerRejectionStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JEApprovalComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JEApprovalDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JEApprovalStatus",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JERejectionComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JERejectionDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JERejectionStatus",
                table: "PositionApplications");
        }
    }
}
