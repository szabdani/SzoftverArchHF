namespace Identity.Api.Invitations;

/// <summary>
/// A meghívás állapota. Az elfogadott (Accepted) meghívás maga az élő edző–futó kapcsolat.
/// </summary>
public enum InvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Ended,
}

/// <summary>
/// Edző meghívása egy futónak e-mail cím alapján (UC12). Elfogadás után ez maga az
/// edző–futó kapcsolat, amit a futó bármikor megszüntethet (UC13).
/// </summary>
public class Invitation
{
    public const int EmailMaxLength = 320;

    private Invitation() { }

    public Guid Id { get; private set; }
    public Guid CoachId { get; private set; }
    public string CoachEmail { get; private set; } = "";
    public string RunnerEmail { get; private set; } = "";

    /// <summary>A futó azonosítója; csak elfogadás után ismert.</summary>
    public Guid? RunnerId { get; private set; }

    public InvitationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Új, függő meghívás az edzőtől a futó e-mail címére.
    /// </summary>
    public static Invitation Create(Guid coachId, string coachEmail, string runnerEmail, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        CoachId = coachId,
        CoachEmail = coachEmail,
        RunnerEmail = runnerEmail,
        Status = InvitationStatus.Pending,
        CreatedAt = now,
        UpdatedAt = now,
    };

    /// <summary>
    /// A futó elfogadja a meghívást: létrejön a kapcsolat.
    /// </summary>
    public void Accept(Guid runnerId, DateTimeOffset now)
    {
        RunnerId = runnerId;
        Status = InvitationStatus.Accepted;
        UpdatedAt = now;
    }

    /// <summary>
    /// A futó elutasítja a meghívást.
    /// </summary>
    public void Decline(DateTimeOffset now)
    {
        Status = InvitationStatus.Declined;
        UpdatedAt = now;
    }

    /// <summary>
    /// A futó megszünteti a kapcsolatot.
    /// </summary>
    public void End(DateTimeOffset now)
    {
        Status = InvitationStatus.Ended;
        UpdatedAt = now;
    }
}
