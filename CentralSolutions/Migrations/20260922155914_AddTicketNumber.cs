using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralSolutions.Migrations
{
	/// <inheritdoc />
	public partial class AddTicketNumber : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AlterColumn<Guid>(
				name: "Id",
				table: "support_tickets",
				type: "TEXT",
				nullable: false,
				oldClrType: typeof(int),
				oldType: "INTEGER")
				.OldAnnotation("Sqlite:Autoincrement", true);

			migrationBuilder.AddColumn<int>(
				name: "TicketNumber",
				table: "support_tickets",
				type: "INTEGER",
				nullable: false,
				defaultValue: 0);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "TicketNumber",
				table: "support_tickets");

			migrationBuilder.AlterColumn<int>(
				name: "Id",
				table: "support_tickets",
				type: "INTEGER",
				nullable: false,
				oldClrType: typeof(Guid),
				oldType: "TEXT")
				.Annotation("Sqlite:Autoincrement", true);
		}
	}
}
