using System;
using CentralSolutions.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralSolutions.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260101000000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "support_tickets",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                TicketType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ResponsibleTechnician = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                Resolution = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                ResolutionStatus = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_support_tickets", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "support_tickets");
    }
}
