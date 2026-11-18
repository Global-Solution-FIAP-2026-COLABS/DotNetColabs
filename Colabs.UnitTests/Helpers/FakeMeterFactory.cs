using System.Diagnostics.Metrics;
using Moq;

namespace Colabs.UnitTests.Helpers;

/// <summary>
/// Os Services recebem <see cref="IMeterFactory"/> via DI para criar métricas customizadas (Counters).
/// Nos testes unitários só precisamos que a chamada a "Create" não quebre a construção do Service:
/// este helper devolve um Mock que sempre retorna um Meter real (e descartável).
/// </summary>
public static class FakeMeterFactory
{
    public static IMeterFactory Create()
    {
        var mock = new Mock<IMeterFactory>();
        mock.Setup(m => m.Create(It.IsAny<MeterOptions>()))
            .Returns(new Meter("Colabs.UnitTests"));

        return mock.Object;
    }
}