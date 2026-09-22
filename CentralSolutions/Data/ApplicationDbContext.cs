using CentralSolutions.Models;
using Microsoft.EntityFrameworkCore;

namespace CentralSolutions.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
	public DbSet<Department> Departments => Set<Department>();

	public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

	public DbSet<User> Users => Set<User>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<Department>(entity =>
		{
			entity.ToTable("departments");
			entity.HasIndex(department => department.Name).IsUnique();
			entity.Property(department => department.Name).HasMaxLength(120).IsRequired();
			entity.Property(department => department.IsActive).IsRequired();
			entity.Property(department => department.CreatedAt).IsRequired();
		});

		modelBuilder.Entity<SupportTicket>(entity =>
		{
			entity.ToTable("support_tickets");
			entity.Property(ticket => ticket.Department).HasMaxLength(100).IsRequired();
			entity.Property(ticket => ticket.TicketType).HasMaxLength(100).IsRequired();
			entity.Property(ticket => ticket.ResponsibleTechnician).HasMaxLength(120);
			entity.Property(ticket => ticket.Problem).HasMaxLength(1000).IsRequired();
			entity.Property(ticket => ticket.Solution).HasMaxLength(1000);
			entity.Property(ticket => ticket.ResolutionStatus).IsRequired();
			entity.Property(ticket => ticket.CreatedAt).IsRequired();
		});

		modelBuilder.Entity<User>(entity =>
		{
			entity.ToTable("users");
			entity.HasIndex(user => user.Id).IsUnique();
			entity.Property(user => user.Email).IsRequired();
			entity.Property(user => user.PasswordHash).IsRequired();
			entity.Property(user => user.IsActive).IsRequired();
		});
	}
}
