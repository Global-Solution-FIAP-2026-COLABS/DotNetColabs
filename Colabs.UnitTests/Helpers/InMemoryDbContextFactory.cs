using Colabs.Data;
using Microsoft.EntityFrameworkCore;

namespace Colabs.UnitTests.Helpers;

/// <summary>
/// Cria instâncias de <see cref="AppDbContext"/> usando o provider EF Core InMemory, cada uma com um nome
/// de banco único (Guid), garantindo isolamento total entre testes.
/// </summary>
public static class InMemoryDbContextFactory
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}