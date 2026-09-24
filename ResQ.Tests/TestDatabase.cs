using Microsoft.EntityFrameworkCore;
using ResQ.Infrastructure.Persistence;

namespace ResQ.Tests;

internal static class TestDatabase
{
    public static ResQDbContext Create() => new(
        new DbContextOptionsBuilder<ResQDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
