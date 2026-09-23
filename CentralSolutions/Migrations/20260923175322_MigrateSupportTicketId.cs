using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralSolutions.Migrations
{
  /// <inheritdoc />
  public partial class MigrateSupportTicketId : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "support_tickets_new",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "TEXT", nullable: false),
            TicketNumber = table.Column<int>(type: "INTEGER", nullable: false),
            Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            TicketType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            ResponsibleTechnician = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
            Problem = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
            Solution = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
            ResolutionStatus = table.Column<int>(type: "INTEGER", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_support_tickets_new", x => x.Id);
          });

      migrationBuilder.Sql("""
        INSERT INTO support_tickets_new
        (
            Id,
            TicketNumber,
            Department,
            TicketType,
            ResponsibleTechnician,
            Problem,
            Solution,
            ResolutionStatus,
            CreatedAt,
            UpdatedAt
        )
        SELECT
            lower(
                hex(randomblob(4)) || '-' ||
                hex(randomblob(2)) || '-' ||
                hex(randomblob(2)) || '-' ||
                hex(randomblob(2)) || '-' ||
                hex(randomblob(6))
            ),
            Id,
            Department,
            TicketType,
            ResponsibleTechnician,
            Problem,
            Solution,
            ResolutionStatus,
            CreatedAt,
            UpdatedAt
        FROM support_tickets;
        """);

      migrationBuilder.DropTable(
          name: "support_tickets");

      migrationBuilder.RenameTable(
          name: "support_tickets_new",
          newName: "support_tickets");

      migrationBuilder.CreateIndex(
          name: "IX_support_tickets_TicketNumber",
          table: "support_tickets",
          column: "TicketNumber",
          unique: true);
    }
    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "support_tickets_old",
          columns: table => new
          {
            Id = table.Column<int>(type: "INTEGER", nullable: false)
                  .Annotation("Sqlite:Autoincrement", true),
            Department = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            TicketType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
            ResponsibleTechnician = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
            Problem = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
            Solution = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
            ResolutionStatus = table.Column<int>(type: "INTEGER", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
            UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_support_tickets_old", x => x.Id);
          });

      migrationBuilder.Sql("""
        INSERT INTO support_tickets_old
        (
            Id,
            Department,
            TicketType,
            ResponsibleTechnician,
            Problem,
            Solution,
            ResolutionStatus,
            CreatedAt,
            UpdatedAt
        )
        SELECT
            TicketNumber,
            Department,
            TicketType,
            ResponsibleTechnician,
            Problem,
            Solution,
            ResolutionStatus,
            CreatedAt,
            UpdatedAt
        FROM support_tickets;
        """);

      migrationBuilder.DropTable(
          name: "support_tickets");

      migrationBuilder.RenameTable(
          name: "support_tickets_old",
          newName: "support_tickets");
    }
  }
}
