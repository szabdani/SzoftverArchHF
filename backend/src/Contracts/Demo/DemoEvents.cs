namespace Contracts.Demo;

/// <summary>A szimulált futók négy jellegzetes mintája (spec 3.2).</summary>
public enum DemoRunnerPattern
{
    SteadyBuild,
    SuddenSpike,
    FrequentSkipping,
    TooManyIntenseDays,
}

/// <summary>Egy szimulált futó és a mintája.</summary>
public record DemoRunner(Guid RunnerId, DemoRunnerPattern Pattern);

/// <summary>Az admin demóadatot kért (UC18): a Training legenerálja a futók 12 hetét.</summary>
public record DemoDataRequested(List<DemoRunner> Runners, DateTimeOffset OccurredAt);

/// <summary>Az admin törölte a demóadatot: a szimulált futók adatai törlődnek.</summary>
public record DemoDataRemoved(List<Guid> RunnerIds, DateTimeOffset OccurredAt);

/// <summary>
/// A futó összes Training-adata törlődött (demó törlése vagy újragenerálása előtt). Elemenkénti törlési
/// események helyett egy esemény, hogy az Analytics ne számoljon közben félig törölt adatból.
/// </summary>
public record RunnerDataReset(Guid RunnerId, DateTimeOffset OccurredAt);
