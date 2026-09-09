using CentralSolutions.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralSolutions.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260827190000_AddProblemAndSolutionToTickets")]
public partial class AddProblemAndSolutionToTickets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameColumn(
            name: "Resolution",
            table: "support_tickets",
            newName: "Problem");

        migrationBuilder.Sql("UPDATE support_tickets SET Problem = '' WHERE Problem IS NULL;");

        migrationBuilder.AddColumn<string>(
            name: "Solution",
            table: "support_tickets",
            type: "TEXT",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Solution",
            table: "support_tickets");

        migrationBuilder.RenameColumn(
            name: "Problem",
            table: "support_tickets",
            newName: "Resolution");
    }
}
