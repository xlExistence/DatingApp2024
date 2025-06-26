using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Data.Migrations
{
    /// <inheritdoc />
    public partial class MessageGroupsAdded_ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connections_Groups_MessageGroupName",
                table: "Connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Groups",
                table: "Groups");

            migrationBuilder.RenameTable(
                name: "Groups",
                newName: "MessageGroups");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageGroups",
                table: "MessageGroups",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Connections_MessageGroups_MessageGroupName",
                table: "Connections",
                column: "MessageGroupName",
                principalTable: "MessageGroups",
                principalColumn: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connections_MessageGroups_MessageGroupName",
                table: "Connections");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageGroups",
                table: "MessageGroups");

            migrationBuilder.RenameTable(
                name: "MessageGroups",
                newName: "Groups");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Groups",
                table: "Groups",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Connections_Groups_MessageGroupName",
                table: "Connections",
                column: "MessageGroupName",
                principalTable: "Groups",
                principalColumn: "Name");
        }
    }
}
