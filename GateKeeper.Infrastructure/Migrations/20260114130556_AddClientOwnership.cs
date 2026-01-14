using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GateKeeper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add OwnerId column as nullable first
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: true);

            // Step 2: Set OwnerId for existing clients to first user (if any exist)
            migrationBuilder.Sql(@"
                UPDATE Clients 
                SET OwnerId = (SELECT TOP 1 Id FROM Users ORDER BY CreatedAt)
                WHERE OwnerId IS NULL AND EXISTS (SELECT 1 FROM Users)
            ");

            // Step 3: Make OwnerId NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerId",
                table: "Clients",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Step 4: Create index
            migrationBuilder.CreateIndex(
                name: "IX_Clients_OwnerId",
                table: "Clients",
                column: "OwnerId");

            // Step 5: Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Clients_Users_OwnerId",
                table: "Clients",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clients_Users_OwnerId",
                table: "Clients");

            migrationBuilder.DropIndex(
                name: "IX_Clients_OwnerId",
                table: "Clients");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Clients");
        }
    }
}
