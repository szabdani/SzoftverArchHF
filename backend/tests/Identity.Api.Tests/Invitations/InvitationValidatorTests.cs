using Identity.Api.Invitations;

namespace Identity.Api.Tests.Invitations;

/// <summary>
/// A meghívás e-mail címének ellenőrzése.
/// </summary>
public class InvitationValidatorTests
{
    private const string CoachEmail = "coach@futoedzes.local";

    /// <summary>
    /// Érvényes cím átmegy, és kisbetűs, szóköz nélküli alakra normalizálódik.
    /// </summary>
    [Fact]
    public void ValidEmail_IsNormalized()
    {
        var isValid = InvitationValidator.TryValidate(
            new InvitationRequest("  Runner@FutoEdzes.Local "), CoachEmail, out var runnerEmail, out var errors);

        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.Equal("runner@futoedzes.local", runnerEmail);
    }

    /// <summary>
    /// Hiányzó, üres, hibás formátumú vagy "Név &lt;cím&gt;" alakú cím nem mehet át.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("nem-email")]
    [InlineData("runner@")]
    [InlineData("Futó <runner@futoedzes.local>")]
    public void InvalidEmail_Fails(string? email)
    {
        var isValid = InvitationValidator.TryValidate(new InvitationRequest(email), CoachEmail, out _, out var errors);

        Assert.False(isValid);
        Assert.True(errors.ContainsKey("email"));
    }

    /// <summary>
    /// Túl hosszú (320 karakternél hosszabb) cím nem mehet át.
    /// </summary>
    [Fact]
    public void TooLongEmail_Fails()
    {
        var email = new string('a', 320) + "@futoedzes.local";

        var isValid = InvitationValidator.TryValidate(new InvitationRequest(email), CoachEmail, out _, out _);

        Assert.False(isValid);
    }

    /// <summary>
    /// Az edző saját magát nem hívhatja meg, kis- és nagybetűtől függetlenül.
    /// </summary>
    [Fact]
    public void SelfInvite_Fails()
    {
        var isValid = InvitationValidator.TryValidate(
            new InvitationRequest("COACH@futoedzes.local"), CoachEmail, out _, out var errors);

        Assert.False(isValid);
        Assert.Equal(["Saját magadat nem hívhatod meg."], errors["email"]);
    }
}
