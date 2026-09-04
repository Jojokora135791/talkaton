using Microsoft.EntityFrameworkCore;
using Talkaton.Infrastructure.Persistence;

namespace Talkaton.Api.Tests;

public class DatabaseSeederTests
{
    [Fact]
    public async Task Seed_creates_demo_user_with_calendars_from_the_mockup()
    {
        await using var db = NewInMemoryContext();

        await DatabaseSeeder.SeedAsync(db);

        Assert.Single(db.Users);
        Assert.Equal(4, await db.Calendars.CountAsync());
        Assert.Contains(db.Calendars, c => c.Name == "Рабочие встречи" && c.IsVisible);
        Assert.Contains(db.Calendars, c => c.Name == "Задачи и дедлайны" && !c.IsVisible);
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        await using var db = NewInMemoryContext();

        await DatabaseSeeder.SeedAsync(db);
        await DatabaseSeeder.SeedAsync(db);

        Assert.Equal(4, await db.Calendars.CountAsync());
    }

    private static TalkatonDbContext NewInMemoryContext() => new(
        new DbContextOptionsBuilder<TalkatonDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
