using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddJuniorEngineerWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllDocumentsVerified",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AppointmentScheduled",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppointmentScheduledDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedJuniorEngineerId",
                table: "PositionApplications",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedToJEDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DigitalSignatureApplied",
                table: "PositionApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DigitalSignatureDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DocumentsVerifiedDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JEComments",
                table: "PositionApplications",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "JECompletedDate",
                table: "PositionApplications",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ContactPerson = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Place = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RoomNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledByOfficerId = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RescheduledToAppointmentId = table.Column<int>(type: "integer", nullable: true),
                    RescheduledFromAppointmentId = table.Column<int>(type: "integer", nullable: true),
                    EmailNotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    SmsNotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Appointments_RescheduledToAppointmentId",
                        column: x => x.RescheduledToAppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Appointments_Officers_ScheduledByOfficerId",
                        column: x => x.ScheduledByOfficerId,
                        principalTable: "Officers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_PositionApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PositionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AutoAssignmentRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositionType = table.Column<int>(type: "integer", nullable: false),
                    TargetOfficerRole = table.Column<int>(type: "integer", nullable: false),
                    Strategy = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    MaxWorkloadPerOfficer = table.Column<int>(type: "integer", nullable: false),
                    MinimumExperienceMonths = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Conditions = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedByAdminId = table.Column<string>(type: "text", nullable: true),
                    ModifiedByAdminId = table.Column<string>(type: "text", nullable: true),
                    AutoAssignOnSubmission = table.Column<bool>(type: "boolean", nullable: false),
                    SendNotification = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationTemplate = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EscalationTimeHours = table.Column<int>(type: "integer", nullable: true),
                    EscalationRole = table.Column<int>(type: "integer", nullable: true),
                    TimesApplied = table.Column<int>(type: "integer", nullable: false),
                    LastAppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRoundRobinIndex = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoAssignmentRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DigitalSignatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SignedByOfficerId = table.Column<int>(type: "integer", nullable: false),
                    SignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignedDocumentPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SignatureHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CertificateThumbprint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CertificateIssuer = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CertificateSubject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CertificateExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HsmProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HsmTransactionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    KeyLabel = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SignatureCoordinates = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    LastVerifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    OtpUsed = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    SignatureStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignatureCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SignatureDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    HsmResponse = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalSignatures_Officers_SignedByOfficerId",
                        column: x => x.SignedByOfficerId,
                        principalTable: "Officers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DigitalSignatures_PositionApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PositionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<int>(type: "integer", nullable: false),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    VerifiedByOfficerId = table.Column<int>(type: "integer", nullable: true),
                    VerifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerificationComments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsAuthentic = table.Column<bool>(type: "boolean", nullable: true),
                    IsCompliant = table.Column<bool>(type: "boolean", nullable: true),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: true),
                    ChecklistItems = table.Column<string>(type: "text", nullable: true),
                    VerificationDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DocumentSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    DocumentHash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentVerifications_Officers_VerifiedByOfficerId",
                        column: x => x.VerifiedByOfficerId,
                        principalTable: "Officers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DocumentVerifications_PositionApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PositionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentVerifications_SEDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "SEDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationId = table.Column<int>(type: "integer", nullable: false),
                    PreviousOfficerId = table.Column<int>(type: "integer", nullable: true),
                    AssignedToOfficerId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AssignedByAdminId = table.Column<string>(type: "text", nullable: true),
                    AutoAssignmentRuleId = table.Column<int>(type: "integer", nullable: true),
                    OfficerWorkloadAtAssignment = table.Column<int>(type: "integer", nullable: true),
                    StrategyUsed = table.Column<int>(type: "integer", nullable: true),
                    PriorityScore = table.Column<decimal>(type: "numeric", nullable: true),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OfficerAccepted = table.Column<bool>(type: "boolean", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApplicationStatusAtAssignment = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    InactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssignmentDurationHours = table.Column<decimal>(type: "numeric", nullable: true),
                    AdminComments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssignmentHistories_AutoAssignmentRules_AutoAssignmentRuleId",
                        column: x => x.AutoAssignmentRuleId,
                        principalTable: "AutoAssignmentRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssignmentHistories_Officers_AssignedToOfficerId",
                        column: x => x.AssignedToOfficerId,
                        principalTable: "Officers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssignmentHistories_Officers_PreviousOfficerId",
                        column: x => x.PreviousOfficerId,
                        principalTable: "Officers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AssignmentHistories_PositionApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "PositionApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_AssignedJuniorEngineerId",
                table: "PositionApplications",
                column: "AssignedJuniorEngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionApplications_Status_AssignedJuniorEngineerId",
                table: "PositionApplications",
                columns: new[] { "Status", "AssignedJuniorEngineerId" });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ApplicationId",
                table: "Appointments",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_RescheduledToAppointmentId",
                table: "Appointments",
                column: "RescheduledToAppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledByOfficerId",
                table: "Appointments",
                column: "ScheduledByOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_Status_ReviewDate",
                table: "Appointments",
                columns: new[] { "Status", "ReviewDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistories_ApplicationId",
                table: "AssignmentHistories",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistories_AssignedToOfficerId",
                table: "AssignmentHistories",
                column: "AssignedToOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistories_AutoAssignmentRuleId",
                table: "AssignmentHistories",
                column: "AutoAssignmentRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistories_IsActive_AssignedDate",
                table: "AssignmentHistories",
                columns: new[] { "IsActive", "AssignedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentHistories_PreviousOfficerId",
                table: "AssignmentHistories",
                column: "PreviousOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_AutoAssignmentRules_PositionType_IsActive",
                table: "AutoAssignmentRules",
                columns: new[] { "PositionType", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AutoAssignmentRules_Priority",
                table: "AutoAssignmentRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AutoAssignmentRules_TargetOfficerRole_IsActive",
                table: "AutoAssignmentRules",
                columns: new[] { "TargetOfficerRole", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignatures_ApplicationId",
                table: "DigitalSignatures",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignatures_HsmTransactionId",
                table: "DigitalSignatures",
                column: "HsmTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignatures_SignedByOfficerId",
                table: "DigitalSignatures",
                column: "SignedByOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignatures_Status_SignedDate",
                table: "DigitalSignatures",
                columns: new[] { "Status", "SignedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerifications_ApplicationId",
                table: "DocumentVerifications",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerifications_DocumentId",
                table: "DocumentVerifications",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerifications_Status_VerifiedDate",
                table: "DocumentVerifications",
                columns: new[] { "Status", "VerifiedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVerifications_VerifiedByOfficerId",
                table: "DocumentVerifications",
                column: "VerifiedByOfficerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PositionApplications_Officers_AssignedJuniorEngineerId",
                table: "PositionApplications",
                column: "AssignedJuniorEngineerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PositionApplications_Officers_AssignedJuniorEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AssignmentHistories");

            migrationBuilder.DropTable(
                name: "DigitalSignatures");

            migrationBuilder.DropTable(
                name: "DocumentVerifications");

            migrationBuilder.DropTable(
                name: "AutoAssignmentRules");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_AssignedJuniorEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropIndex(
                name: "IX_PositionApplications_Status_AssignedJuniorEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AllDocumentsVerified",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AppointmentScheduled",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AppointmentScheduledDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedJuniorEngineerId",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "AssignedToJEDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "DigitalSignatureApplied",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "DigitalSignatureDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "DocumentsVerifiedDate",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JEComments",
                table: "PositionApplications");

            migrationBuilder.DropColumn(
                name: "JECompletedDate",
                table: "PositionApplications");
        }
    }
}
