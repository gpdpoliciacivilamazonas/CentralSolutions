using System;
using CentralSolutions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CentralSolutions.Migrations;

[DbContext(typeof(ApplicationDbContext))]
partial class ApplicationDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "8.0.30");

        modelBuilder.Entity("CentralSolutions.Models.SupportTicket", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("INTEGER");

                b.Property<DateTime>("CreatedAt")
                    .HasColumnType("TEXT");

                b.Property<string>("Department")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("TEXT");

                b.Property<string>("Resolution")
                    .HasMaxLength(1000)
                    .HasColumnType("TEXT");

                b.Property<int>("ResolutionStatus")
                    .HasColumnType("INTEGER");

                b.Property<string>("ResponsibleTechnician")
                    .HasMaxLength(120)
                    .HasColumnType("TEXT");

                b.Property<string>("TicketType")
                    .IsRequired()
                    .HasMaxLength(100)
                    .HasColumnType("TEXT");

                b.Property<DateTime?>("UpdatedAt")
                    .HasColumnType("TEXT");

                b.HasKey("Id");

                b.ToTable("support_tickets", (string)null);
            });
#pragma warning restore 612, 618
    }
}
