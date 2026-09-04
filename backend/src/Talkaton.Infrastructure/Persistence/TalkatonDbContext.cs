using Microsoft.EntityFrameworkCore;
using Talkaton.Domain.Entities;

namespace Talkaton.Infrastructure.Persistence;

public class TalkatonDbContext(DbContextOptions<TalkatonDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Calendar> Calendars => Set<Calendar>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(user =>
        {
            user.ToTable("users");
            user.HasKey(x => x.Id);
            user.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            user.Property(x => x.Email).HasMaxLength(320).IsRequired();
            user.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
            user.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Calendar>(calendar =>
        {
            calendar.ToTable("calendars");
            calendar.HasKey(x => x.Id);
            calendar.Property(x => x.Name).HasMaxLength(200).IsRequired();
            calendar.Property(x => x.Color).HasMaxLength(9).IsRequired();
            calendar.HasOne(x => x.Owner)
                .WithMany(x => x.Calendars)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            calendar.HasIndex(x => new { x.OwnerId, x.SortOrder });
        });
    }
}
