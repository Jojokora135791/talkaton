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
            .UseSqlite("Data Source=talkaton.db")
            .Options;

        return new TalkatonDbContext(options);
    }
}
