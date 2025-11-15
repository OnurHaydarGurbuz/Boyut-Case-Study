using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BoyutAplication.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "INVOICE_STATUS_LOG",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    INVOICE_NUMBER = table.Column<string>(type: "TEXT", nullable: false),
                    TAX_NUMBER = table.Column<string>(type: "TEXT", nullable: false),
                    RESPONSE_CODE = table.Column<string>(type: "TEXT", nullable: false),
                    RESPONSE_MESSAGE = table.Column<string>(type: "TEXT", nullable: false),
                    REQUEST_TIME = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVOICE_STATUS_LOG", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "INVOICE_STATUS_LOG");
        }
    }
}
