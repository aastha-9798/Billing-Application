using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlateBilling.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GlobalAreaRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    GSTIN = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlateTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Length = table.Column<decimal>(type: "TEXT", nullable: false),
                    Breadth = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlateTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Challans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    ChallanNo = table.Column<string>(type: "TEXT", nullable: false),
                    PlateDescription = table.Column<string>(type: "TEXT", nullable: false),
                    PlateTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaBilling = table.Column<bool>(type: "INTEGER", nullable: false),
                    AppliedRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    PlateLength = table.Column<decimal>(type: "TEXT", nullable: false),
                    PlateBreadth = table.Column<decimal>(type: "TEXT", nullable: false),
                    AppliedAreaRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    TotalCost = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challans_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Challans_PlateTypes_PlateTypeId",
                        column: x => x.PlateTypeId,
                        principalTable: "PlateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClientPlateRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClientId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlateTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Rate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientPlateRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientPlateRates_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClientPlateRates_PlateTypes_PlateTypeId",
                        column: x => x.PlateTypeId,
                        principalTable: "PlateTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Challans_ClientId",
                table: "Challans",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Challans_PlateTypeId",
                table: "Challans",
                column: "PlateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientPlateRates_ClientId_PlateTypeId",
                table: "ClientPlateRates",
                columns: new[] { "ClientId", "PlateTypeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientPlateRates_PlateTypeId",
                table: "ClientPlateRates",
                column: "PlateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_Name",
                table: "Clients",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlateTypes_Code",
                table: "PlateTypes",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSettings");

            migrationBuilder.DropTable(
                name: "Challans");

            migrationBuilder.DropTable(
                name: "ClientPlateRates");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "PlateTypes");
        }
    }
}
