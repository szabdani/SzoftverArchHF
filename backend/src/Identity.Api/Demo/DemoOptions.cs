using Contracts.Demo;

namespace Identity.Api.Demo;

/// <summary>
/// A demó szereplői: a demó edző és a szimulált futók. Az azonosítók a Keycloak realmben előre
/// létrehozott felhasználókéi, így a demóban mindegyikkel be lehet lépni.
/// </summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";

    public Guid CoachId { get; init; }
    public string CoachEmail { get; init; } = "";
    public List<DemoRunnerOptions> Runners { get; init; } = [];
}

/// <summary>Egy szimulált futó: Keycloak azonosító, e-mail és minta.</summary>
public sealed class DemoRunnerOptions
{
    public Guid RunnerId { get; init; }
    public string Email { get; init; } = "";
    public DemoRunnerPattern Pattern { get; init; }
}
