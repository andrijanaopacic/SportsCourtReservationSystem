using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public class ReservationContext : DbContext
    {
        public ReservationContext() { }
        public ReservationContext(DbContextOptions<ReservationContext> options) : base(options) { }

        public DbSet<Sport> Sports { get; set; }
        public DbSet<Court> Courts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sport>(entity =>
            {
                entity.HasKey(s => s.SportId);
                entity.Property(s => s.Name).IsRequired().HasMaxLength(50);
                entity.Property(s => s.MaxPlayers).IsRequired();
            });

            modelBuilder.Entity<Court>(entity =>
            {
                entity.HasKey(c => c.CourtId);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Location).IsRequired().HasMaxLength(200);
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.PricePerHour).HasPrecision(18, 2);
                entity.Property(c => c.IsIndoor).IsRequired();

                entity.HasOne(c => c.Sport)
                      .WithMany(s => s.Courts)
                      .HasForeignKey(c => c.SportId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
