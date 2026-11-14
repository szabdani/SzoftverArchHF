namespace Contracts.Identity;

/// <summary>A futó elfogadta az edző meghívását, a kapcsolat létrejött.</summary>
public record CoachRunnerLinked(Guid CoachId, Guid RunnerId, DateTimeOffset OccurredAt);

/// <summary>A futó megszüntette a kapcsolatot.</summary>
public record CoachRunnerUnlinked(Guid CoachId, Guid RunnerId, DateTimeOffset OccurredAt);
