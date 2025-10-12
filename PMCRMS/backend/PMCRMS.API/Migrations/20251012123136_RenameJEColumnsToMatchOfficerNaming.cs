using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameJEColumnsToMatchOfficerNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AppointmentScheduledDate",
                table: "PositionApplications");

            migrationBuilder.RenameColumn(
                name: "JECompletedDate",
                table: "PositionApplications",
                newName: "JEDocumentVerificationDate");

            migrationBuilder.RenameColumn(
                name: "DocumentsVerifiedDate",
                table: "PositionApplications",
                newName: "JEDigitalSignatureDate");

            migrationBuilder.RenameColumn(
                name: "DigitalSignatureDate",
                table: "PositionApplications",
                newName: "JEAppointmentScheduledDate");

            migrationBuilder.RenameColumn(
                name: "DigitalSignatureApplied",
                table: "PositionApplications",
                newName: "JEDigitalSignatureApplied");

            migrationBuilder.RenameColumn(
                name: "AppointmentScheduled",
                table: "PositionApplications",
                newName: "JEAppointmentScheduled");

            migrationBuilder.RenameColumn(
                name: "AllDocumentsVerified",
                table: "PositionApplications",
                newName: "JEAllDocumentsVerified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "JEDocumentVerificationDate",
                table: "PositionApplications",
                newName: "JECompletedDate");

            migrationBuilder.RenameColumn(
                name: "JEDigitalSignatureDate",
                table: "PositionApplications",
                newName: "DocumentsVerifiedDate");

            migrationBuilder.RenameColumn(
                name: "JEDigitalSignatureApplied",
                table: "PositionApplications",
                newName: "DigitalSignatureApplied");

            migrationBuilder.RenameColumn(
                name: "JEAppointmentScheduledDate",
                table: "PositionApplications",
                newName: "DigitalSignatureDate");

            migrationBuilder.RenameColumn(
                name: "JEAppointmentScheduled",
                table: "PositionApplications",
                newName: "AppointmentScheduled");

            migrationBuilder.RenameColumn(
                name: "JEAllDocumentsVerified",
                table: "PositionApplications",
                newName: "AllDocumentsVerified");

            migrationBuilder.AddColumn<DateTime>(
                name: "AppointmentScheduledDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);
        }
    }
}
