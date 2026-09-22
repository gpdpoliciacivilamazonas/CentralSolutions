using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralSolutions.Migrations
{
	/// <inheritdoc />
	public partial class AddUsers : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.CreateTable(
				name: "users",
				columns: table => new
				{
					Id = table.Column<Guid>(type: "TEXT", nullable: false),
					Email = table.Column<string>(type: "TEXT", nullable: false),
					PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
					IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
				},
				constraints: table =>
				{
					table.PrimaryKey("PK_users", x => x.Id);
				});

			migrationBuilder.CreateIndex(
				name: "IX_users_Id",
				table: "users",
				column: "Id",
				unique: true);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropTable(
				name: "users");
		}
	}
}
