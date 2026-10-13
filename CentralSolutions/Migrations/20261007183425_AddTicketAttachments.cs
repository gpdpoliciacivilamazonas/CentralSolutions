using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralSolutions.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticket_attachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SupportTicketId = table.Column<string>(type: "TEXT", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_attachments_support_tickets_SupportTicketId",
                        column: x => x.SupportTicketId,
                        principalTable: "support_tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ticket_attachments_SupportTicketId",
                table: "ticket_attachments",
                column: "SupportTicketId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_attachments");
        }
    }
}
