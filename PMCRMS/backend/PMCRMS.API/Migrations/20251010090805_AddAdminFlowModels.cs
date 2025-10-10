using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminFlowModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.CreateTable(
                name: "FormConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FormType = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BaseFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ProcessingFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LateFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AllowOnlineSubmission = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingDays = table.Column<int>(type: "integer", nullable: false),
                    CustomFields = table.Column<string>(type: "jsonb", nullable: true),
                    RequiredDocuments = table.Column<string>(type: "jsonb", nullable: true),
                    MaxFileSizeMB = table.Column<int>(type: "integer", nullable: true),
                    MaxFilesAllowed = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OfficerInvitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TemporaryPassword = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    InvitedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvitedByUserId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfficerInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficerInvitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfficerInvitations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FormFeeHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormConfigurationId = table.Column<int>(type: "integer", nullable: false),
                    OldBaseFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewBaseFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OldProcessingFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    NewProcessingFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "integer", nullable: false),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFeeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFeeHistories_FormConfigurations_FormConfigurationId",
                        column: x => x.FormConfigurationId,
                        principalTable: "FormConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FormFeeHistories_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "FormConfigurations",
                columns: new[] { "Id", "AllowOnlineSubmission", "BaseFee", "CreatedBy", "CreatedDate", "CustomFields", "Description", "FormName", "FormType", "IsActive", "LateFee", "MaxFileSizeMB", "MaxFilesAllowed", "ProcessingDays", "ProcessingFee", "RequiredDocuments", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 1, true, 5000m, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Application for new building construction or major renovation", "Building Permit Application", 1, true, 500m, 10, 15, 30, 1000m, null, null, null },
                    { 2, true, 2500m, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Application for Structural Engineer registration", "Structural Engineer License", 2, true, 250m, 5, 10, 15, 500m, null, null, null },
                    { 3, true, 2500m, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Application for Architect registration", "Architect License", 3, true, 250m, 5, 10, 15, 500m, null, null, null },
                    { 4, true, 3000m, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Application for Occupancy Certificate", "Occupancy Certificate", 6, true, 300m, 10, 12, 20, 750m, null, null, null }
                });

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmployeeId", "Role" },
                values: new object[] { "ADMIN001", 16 });

            migrationBuilder.CreateIndex(
                name: "IX_FormConfigurations_FormType",
                table: "FormConfigurations",
                column: "FormType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFeeHistories_ChangedByUserId",
                table: "FormFeeHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFeeHistories_FormConfigurationId",
                table: "FormFeeHistories",
                column: "FormConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerInvitations_Email",
                table: "OfficerInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerInvitations_EmployeeId",
                table: "OfficerInvitations",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficerInvitations_InvitedByUserId",
                table: "OfficerInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerInvitations_UserId",
                table: "OfficerInvitations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormFeeHistories");

            migrationBuilder.DropTable(
                name: "OfficerInvitations");

            migrationBuilder.DropTable(
                name: "FormConfigurations");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmployeeId", "Role" },
                values: new object[] { null, 7 });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "CreatedBy", "CreatedDate", "Email", "EmployeeId", "IsActive", "LastLoginAt", "LockedUntil", "LoginAttempts", "Name", "PasswordHash", "PhoneNumber", "Role", "UpdatedBy", "UpdatedDate" },
                values: new object[,]
                {
                    { 2, null, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "je@pmcrms.gov.in", null, true, null, null, 0, "Junior Engineer", null, "9999999998", 2, null, null },
                    { 3, null, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ae@pmcrms.gov.in", null, true, null, null, 0, "Assistant Engineer", null, "9999999997", 3, null, null },
                    { 4, null, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ee@pmcrms.gov.in", null, true, null, null, 0, "Executive Engineer", null, "9999999996", 4, null, null },
                    { 5, null, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ce@pmcrms.gov.in", null, true, null, null, 0, "City Engineer", null, "9999999995", 5, null, null },
                    { 6, null, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "clerk@pmcrms.gov.in", null, true, null, null, 0, "Clerk", null, "9999999994", 6, null, null }
                });
        }
    }
}
