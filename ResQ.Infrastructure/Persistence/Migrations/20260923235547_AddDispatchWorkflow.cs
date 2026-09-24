using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResQ.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequiredTeamType",
                table: "EmergencyTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Medical");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Emergencies",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTeamId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ResponseTeamId",
                table: "AspNetUsers",
                column: "ResponseTeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ResponseTeamId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RequiredTeamType",
                table: "EmergencyTypes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Emergencies");

            migrationBuilder.DropColumn(
                name: "ResponseTeamId",
                table: "AspNetUsers");
        }
    }
}
