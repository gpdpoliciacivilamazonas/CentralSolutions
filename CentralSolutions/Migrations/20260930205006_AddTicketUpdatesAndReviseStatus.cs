using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralSolutions.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketUpdatesAndReviseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticket_updates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SupportTicketId = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_updates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_updates_support_tickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "support_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_updates_SupportTicketId",
                table: "ticket_updates",
                column: "SupportTicketId");

            // Migrate existing Solution data into ticket_updates to prevent data loss
            migrationBuilder.Sql(@"
                INSERT INTO ticket_updates (Id, SupportTicketId, Description, CreatedBy, CreatedAt)
                SELECT 
                    lower(hex(randomblob(4)) || '-' || hex(randomblob(2)) || '-' || '4' || substr(hex(randomblob(2)), 2) || '-' || substr('89ab', 1 + (abs(random()) % 4), 1) || substr(hex(randomblob(2)), 2) || '-' || hex(randomblob(6))),
                    Id,
                    Solution,
                    COALESCE(ResponsibleTechnician, 'Sistema'),
                    CreatedAt
                FROM support_tickets
                WHERE Solution IS NOT NULL AND trim(Solution) <> '';
            ");

            // Remap legacy status values:
            // 3 (Yes / Completo) -> 2 (Done / Concluído)
            // 2 (No / Incompleto) -> 1 (OnGoing / Em andamento)
            migrationBuilder.Sql(@"
                UPDATE support_tickets 
                SET ResolutionStatus = CASE 
                    WHEN ResolutionStatus = 3 THEN 2 
                    WHEN ResolutionStatus = 2 THEN 1 
                    ELSE ResolutionStatus 
                END;
            ");

            migrationBuilder.DropColumn(
                name: "Solution",
                table: "support_tickets");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Solution",
                table: "support_tickets",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.DropTable(
                name: "ticket_updates");
        }
    }
}
