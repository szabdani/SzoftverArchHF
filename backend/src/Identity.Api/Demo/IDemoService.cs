using Contracts.Demo;

namespace Identity.Api.Demo;

/// <summary>Egy szimulált futó a válaszban (edzésadat nélkül, az admin csak ennyit lát).</summary>
public record DemoRunnerResponse(Guid RunnerId, string Email, DemoRunnerPattern Pattern);

/// <summary>
/// Demóadatok létrehozása és törlése (UC18).
/// </summary>
public interface IDemoService
{
    /// <summary>
    /// Összekapcsolja a demó edzőt a szimulált futókkal, és kéri a 12 hét adat legenerálását.
    /// Ismételt hívásnál az adatok újragenerálódnak.
    /// </summary>
    Task<List<DemoRunnerResponse>> CreateAsync(CancellationToken ct);

    /// <summary>
    /// Megszünteti a demó kapcsolatokat, és kéri a szimulált futók adatainak törlését.
    /// </summary>
    Task RemoveAsync(CancellationToken ct);
}
