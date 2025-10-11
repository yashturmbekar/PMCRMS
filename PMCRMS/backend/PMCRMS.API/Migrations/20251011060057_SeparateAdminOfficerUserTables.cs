using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PMCRMS.API.Migrations
{
    /// <inheritdoc />
    public partial class SeparateAdminOfficerUserTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationComments_Users_CommentedBy",
                table: "ApplicationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationDocuments_Users_VerifiedBy",
                table: "ApplicationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficerInvitations_Users_InvitedByUserId",
                table: "OfficerInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficerInvitations_Users_UserId",
                table: "OfficerInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Users_ProcessedBy",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_SEDocuments_Users_VerifiedBy",
                table: "SEDocuments");

            migrationBuilder.DropIndex(
                name: "IX_OfficerInvitations_UserId",
                table: "OfficerInvitations");

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "OfficerInvitations",
                newName: "OfficerId");

            migrationBuilder.RenameColumn(
                name: "InvitedByUserId",
                table: "OfficerInvitations",
                newName: "InvitedByAdminId");

            migrationBuilder.RenameIndex(
                name: "IX_OfficerInvitations_InvitedByUserId",
                table: "OfficerInvitations",
                newName: "IX_OfficerInvitations_InvitedByAdminId");

            migrationBuilder.AlterColumn<int>(
                name: "ChangedByUserId",
                table: "FormFeeHistories",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ChangedByAdminId",
                table: "FormFeeHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "UpdatedByUserId",
                table: "ApplicationStatuses",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByOfficerId",
                table: "ApplicationStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Officers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvitationId = table.Column<int>(type: "integer", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Officers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemAdmins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EmployeeId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSuperAdmin = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Department = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Designation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAdmins", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "SystemAdmins",
                columns: new[] { "Id", "CreatedBy", "CreatedDate", "Department", "Designation", "Email", "EmployeeId", "IsActive", "IsSuperAdmin", "LastLoginAt", "LockedUntil", "LoginAttempts", "Name", "PasswordHash", "PhoneNumber", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 1, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Administration", "System Administrator", "admin@gmail.com", "ADMIN001", true, true, null, null, 0, "System Administrator", "", "9999999999", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true,
                filter: "\"EmployeeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OfficerInvitations_OfficerId",
                table: "OfficerInvitations",
                column: "OfficerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormFeeHistories_ChangedByAdminId",
                table: "FormFeeHistories",
                column: "ChangedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatuses_UpdatedByOfficerId",
                table: "ApplicationStatuses",
                column: "UpdatedByOfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_Officers_Email",
                table: "Officers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Officers_EmployeeId",
                table: "Officers",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdmins_Email",
                table: "SystemAdmins",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemAdmins_EmployeeId",
                table: "SystemAdmins",
                column: "EmployeeId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationComments_Officers_CommentedBy",
                table: "ApplicationComments",
                column: "CommentedBy",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDocuments_Officers_VerifiedBy",
                table: "ApplicationDocuments",
                column: "VerifiedBy",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationStatuses_Officers_UpdatedByOfficerId",
                table: "ApplicationStatuses",
                column: "UpdatedByOfficerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FormFeeHistories_SystemAdmins_ChangedByAdminId",
                table: "FormFeeHistories",
                column: "ChangedByAdminId",
                principalTable: "SystemAdmins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfficerInvitations_Officers_OfficerId",
                table: "OfficerInvitations",
                column: "OfficerId",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OfficerInvitations_SystemAdmins_InvitedByAdminId",
                table: "OfficerInvitations",
                column: "InvitedByAdminId",
                principalTable: "SystemAdmins",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Officers_ProcessedBy",
                table: "Payments",
                column: "ProcessedBy",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SEDocuments_Officers_VerifiedBy",
                table: "SEDocuments",
                column: "VerifiedBy",
                principalTable: "Officers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationComments_Officers_CommentedBy",
                table: "ApplicationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationDocuments_Officers_VerifiedBy",
                table: "ApplicationDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationStatuses_Officers_UpdatedByOfficerId",
                table: "ApplicationStatuses");

            migrationBuilder.DropForeignKey(
                name: "FK_FormFeeHistories_SystemAdmins_ChangedByAdminId",
                table: "FormFeeHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficerInvitations_Officers_OfficerId",
                table: "OfficerInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficerInvitations_SystemAdmins_InvitedByAdminId",
                table: "OfficerInvitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Officers_ProcessedBy",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_SEDocuments_Officers_VerifiedBy",
                table: "SEDocuments");

            migrationBuilder.DropTable(
                name: "Officers");

            migrationBuilder.DropTable(
                name: "SystemAdmins");

            migrationBuilder.DropIndex(
                name: "IX_Users_EmployeeId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_OfficerInvitations_OfficerId",
                table: "OfficerInvitations");

            migrationBuilder.DropIndex(
                name: "IX_FormFeeHistories_ChangedByAdminId",
                table: "FormFeeHistories");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationStatuses_UpdatedByOfficerId",
                table: "ApplicationStatuses");

            migrationBuilder.DropColumn(
                name: "ChangedByAdminId",
                table: "FormFeeHistories");

            migrationBuilder.DropColumn(
                name: "UpdatedByOfficerId",
                table: "ApplicationStatuses");

            migrationBuilder.RenameColumn(
                name: "OfficerId",
                table: "OfficerInvitations",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "InvitedByAdminId",
                table: "OfficerInvitations",
                newName: "InvitedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_OfficerInvitations_InvitedByAdminId",
                table: "OfficerInvitations",
                newName: "IX_OfficerInvitations_InvitedByUserId");

            migrationBuilder.AlterColumn<int>(
                name: "ChangedByUserId",
                table: "FormFeeHistories",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UpdatedByUserId",
                table: "ApplicationStatuses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Address", "CreatedBy", "CreatedDate", "Email", "EmployeeId", "IsActive", "LastLoginAt", "LockedUntil", "LoginAttempts", "Name", "PasswordHash", "PhoneNumber", "Role", "UpdatedBy", "UpdatedDate" },
                values: new object[] { 1, null, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@gmail.com", "ADMIN001", true, null, null, 0, "System Administrator", null, "9999999999", 1, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_OfficerInvitations_UserId",
                table: "OfficerInvitations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationComments_Users_CommentedBy",
                table: "ApplicationComments",
                column: "CommentedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDocuments_Users_VerifiedBy",
                table: "ApplicationDocuments",
                column: "VerifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OfficerInvitations_Users_InvitedByUserId",
                table: "OfficerInvitations",
                column: "InvitedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfficerInvitations_Users_UserId",
                table: "OfficerInvitations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Users_ProcessedBy",
                table: "Payments",
                column: "ProcessedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_SEDocuments_Users_VerifiedBy",
                table: "SEDocuments",
                column: "VerifiedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
