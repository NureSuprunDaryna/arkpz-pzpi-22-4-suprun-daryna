using Core.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAL
{

    public class ApplicationContext : IdentityDbContext<AppUser>
    {
        public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

        public DbSet<Elixir> Elixirs { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<History> Histories { get; set; }
        public DbSet<ElixirComposition> ElixirCompositions { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<Preferences> Preferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // AppUser -> Preferences
            modelBuilder.Entity<AppUser>()
                .HasOne(a => a.Preferences)
                .WithOne(p => p.User)
                .HasForeignKey<Preferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // AppUser -> Elixirs
            modelBuilder.Entity<AppUser>()
                .HasMany(a => a.Elixirs)
                .WithOne(e => e.Author)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Elixir -> History
            modelBuilder.Entity<Elixir>()
                .HasMany(e => e.Histories)
                .WithOne(h => h.Elixir)
                .HasForeignKey(h => h.ElixirId)
                .OnDelete(DeleteBehavior.SetNull);

            // Device -> History
            modelBuilder.Entity<Device>()
                .HasMany(d => d.Histories)
                .WithOne(h => h.Device)
                .HasForeignKey(h => h.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ElixirComposition>()
                .HasKey(ec => new { ec.ElixirId, ec.NoteId });

            modelBuilder.Entity<ElixirComposition>()
                .Property(ec => ec.Proportion)
                .HasPrecision(18, 4);

            // Elixir -> ElixirComposition
            modelBuilder.Entity<Elixir>()
                .HasMany(e => e.ElixirComposition)
                .WithOne(ec => ec.Elixir)
                .HasForeignKey(ec => ec.ElixirId)
                .OnDelete(DeleteBehavior.Cascade);

            // Note -> ElixirComposition
            modelBuilder.Entity<Note>()
                .HasMany(n => n.ElixirComposition)
                .WithOne(ec => ec.Note)
                .HasForeignKey(ec => ec.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // History -> Device and Elixir
            modelBuilder.Entity<History>()
                .HasOne(h => h.Device)
                .WithMany(d => d.Histories)
                .HasForeignKey(h => h.DeviceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<History>()
                .HasOne(h => h.Elixir)
                .WithMany(e => e.Histories)
                .HasForeignKey(h => h.ElixirId)
                .OnDelete(DeleteBehavior.NoAction);

            // Preferences -> AppUser
            modelBuilder.Entity<Preferences>()
                .HasOne(p => p.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<Preferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}