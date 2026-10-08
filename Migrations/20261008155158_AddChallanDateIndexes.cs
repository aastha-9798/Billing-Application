using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateBilling.Migrations
{
    /// <inheritdoc />
    public partial class AddChallanDateIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Challans_ClientId",
                table: "Challans");

            migrationBuilder.CreateIndex(
                name: "IX_Challans_ClientId_Date",
                table: "Challans",
                columns: new[] { "ClientId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Challans_Date",
                table: "Challans",
                column: "Date");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Challans_ClientId_Date",
                table: "Challans");

            migrationBuilder.DropIndex(
                name: "IX_Challans_Date",
                table: "Challans");

            migrationBuilder.CreateIndex(
                name: "IX_Challans_ClientId",
                table: "Challans",
                column: "ClientId");
        }
    }
}
