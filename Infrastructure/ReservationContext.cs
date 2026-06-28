using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Reservation.Domain.Models;
using Reservation.Infrastructure.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure
{
    public class ReservationContext : IdentityDbContext<ApplicationUser>
    {
        public ReservationContext() { }
        public ReservationContext(DbContextOptions<ReservationContext> options) : base(options) { }
        
        public DbSet<Sport> Sports { get; set; }
        public DbSet<Court> Courts { get; set; }
        public DbSet<TimeSlot> TimeSlots { get; set; }

        public DbSet<ReservationEntity> Reservations { get; set; }
        public DbSet<ReservationItem> ReservationItems { get; set; }

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

            modelBuilder.Entity<TimeSlot>(entity =>
            {
                entity.HasKey(t => t.TimeSlotId);
                entity.Property(t => t.Date).IsRequired();      
                entity.Property(t => t.StartTime).IsRequired();
                entity.Property(t => t.EndTime).IsRequired();

                entity.HasOne(t => t.Court)
                      .WithMany(c => c.TimeSlots)
                      .HasForeignKey(t => t.CourtId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TimeSlot>()
                .Property(t => t.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<TimeSlot>()        
                .Property(t => t.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ReservationEntity>(entity =>
            {
                entity.HasKey(r => r.ReservationId);
                entity.Property(r => r.TotalPrice).HasPrecision(18, 2);
                entity.Property(r => r.Status).HasConversion<string>();

                entity.HasMany(r => r.ReservationItems)
                      .WithOne(i => i.Reservation)
                      .HasForeignKey(i => i.ReservationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });


            modelBuilder.Entity<ReservationItem>(entity =>
            {
                entity.HasKey(i => new { i.ReservationId, i.RowNumber });
                entity.Property(i => i.Price)
                    .HasPrecision(18, 2);
                entity.HasOne(i => i.TimeSlot)
                      .WithMany()
                      .HasForeignKey(i => i.TimeSlotId)
                      .OnDelete(DeleteBehavior.Restrict);
            });


        }
    }
}
