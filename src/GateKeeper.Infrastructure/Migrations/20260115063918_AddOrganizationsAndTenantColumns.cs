using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GateKeeper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationsAndTenantColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOrganizationAdmin",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Subdomain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomDomain = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillingPlan = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SettingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            // Ensure a default organization exists so existing Users/Clients with a default
            // OrganizationId (zero GUID) will not violate FK constraints when foreign keys are added.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM Organizations WHERE Id = '00000000-0000-0000-0000-000000000000')
BEGIN
    INSERT INTO Organizations (Id, Name, Subdomain, CustomDomain, IsActive, CreatedAt, BillingPlan, SettingsJson)
    VALUES ('00000000-0000-0000-0000-000000000000', 'Default Organization', 'default', NULL, 1, SYSUTCDATETIME(), 'free', NULL);
END
");

                                    // Backfill any OrganizationIds referenced by Clients or Users that don't yet exist
                                    migrationBuilder.Sql(@"
            INSERT INTO Organizations (Id, Name, Subdomain, CustomDomain, IsActive, CreatedAt, BillingPlan, SettingsJson)
            SELECT DISTINCT src.OrgId,
                         'MigratedOrg-' + LEFT(CONVERT(varchar(36), src.OrgId), 8),
                         'migrated', NULL, 1, SYSUTCDATETIME(), 'free', NULL
            FROM (
                    SELECT OrganizationId AS OrgId FROM Clients
                    UNION
                    SELECT OrganizationId AS OrgId FROM Users
            ) AS src
            WHERE src.OrgId IS NOT NULL
                AND NOT EXISTS (SELECT 1 FROM Organizations o WHERE o.Id = src.OrgId);
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Users_OrganizationId",
                table: "Users",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_OrganizationId",
                table: "Clients",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Organizations_OrganizationId",
                table: "Clients",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Organizations_OrganizationId",
                table: "Clients");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Organizations_OrganizationId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Users_OrganizationId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Clients_OrganizationId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "IsOrganizationAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Clients");
        }
    }
}
