using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Talkaton.Infrastructure.Persistence;

/// <summary>
/// Нужен только для `dotnet ef migrations add` — чтобы не поднимать Api ради генерации миграции.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TalkatonDbContext>
{
    public TalkatonDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TalkatonDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=talkaton;Username=talkaton;Password=talkaton")
            .Options;

        return new TalkatonDbContext(options);
    }
}
