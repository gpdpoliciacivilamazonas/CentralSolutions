using CentralSolutions.Models;
using Microsoft.EntityFrameworkCore;

namespace CentralSolutions.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.ToTable("support_tickets");
            entity.Property(ticket => ticket.Department).HasMaxLength(100).IsRequired();
            entity.Property(ticket => ticket.TicketType).HasMaxLength(100).IsRequired();
            entity.Property(ticket => ticket.ResponsibleTechnician).HasMaxLength(120);
            entity.Property(ticket => ticket.Resolution).HasMaxLength(1000);
            entity.Property(ticket => ticket.ResolutionStatus).IsRequired();
            entity.Property(ticket => ticket.CreatedAt).IsRequired();
        });
    }
}
