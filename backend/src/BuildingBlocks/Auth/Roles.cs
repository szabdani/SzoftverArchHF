namespace BuildingBlocks.Auth;

/// <summary>Keycloak realm szerepkörök.</summary>
public static class Roles
{
    public const string Runner = "runner";
    public const string Coach = "coach";
    public const string Admin = "admin";
}

/// <summary>Végpontokon használt jogosultsági szabályok.</summary>
public static class Policies
{
    public const string Runner = nameof(Runner);
    public const string Coach = nameof(Coach);
    public const string Admin = nameof(Admin);
    public const string RunnerOrCoach = nameof(RunnerOrCoach);
}
