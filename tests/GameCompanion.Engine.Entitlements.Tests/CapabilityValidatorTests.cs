namespace GameCompanion.Engine.Entitlements.Tests;

using FluentAssertions;
using GameCompanion.Engine.Entitlements.Capabilities;

public class CapabilityValidatorTests
{
    private readonly byte[] _signingKey = new byte[32];
    private readonly CapabilityValidator _validator;
    private readonly CapabilityIssuer _issuer;

    public CapabilityValidatorTests()
    {
        // Use a deterministic key for testing
        for (var i = 0; i < 32; i++)
            _signingKey[i] = (byte)(i + 1);

        _validator = new CapabilityValidator(_signingKey);
        _issuer = new CapabilityIssuer(_validator);
    }

    [Fact]
    public void Validate_WithValidCapability_ReturnsSuccess()
    {
        var cap = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture");

        var result = _validator.Validate(cap, CapabilityActions.SaveModify, "star_rupture");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(cap.Id);
    }

    [Fact]
    public void Validate_WithWildcardScope_MatchesAnyGame()
    {
        var cap = _issuer.Issue(CapabilityActions.SaveModify, "*");

        var result = _validator.Validate(cap, CapabilityActions.SaveModify, "star_rupture");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithWrongAction_ReturnsFailure()
    {
        var cap = _issuer.Issue(CapabilityActions.SaveInspect, "star_rupture");

        var result = _validator.Validate(cap, CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("action mismatch");
    }

    [Fact]
    public void Validate_WithWrongGameScope_ReturnsFailure()
    {
        var cap = _issuer.Issue(CapabilityActions.SaveModify, "minecraft");

        var result = _validator.Validate(cap, CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("scope mismatch");
    }

    [Fact]
    public void Validate_WithExpiredCapability_ReturnsFailure()
    {
        // Issue with -1 hour lifetime (already expired)
        var cap = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture", TimeSpan.FromHours(-1));

        var result = _validator.Validate(cap, CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public void Validate_WithTamperedSignature_ReturnsFailure()
    {
        var cap = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture");

        // Tamper with the signature
        var tampered = new Capability
        {
            Id = cap.Id,
            Action = cap.Action,
            GameScope = cap.GameScope,
            IssuedAt = cap.IssuedAt,
            ExpiresAt = cap.ExpiresAt,
            Signature = "AAAA" + cap.Signature[4..] // Corrupt the signature
        };

        var result = _validator.Validate(tampered, CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("signature");
    }

    [Fact]
    public void Validate_WithTamperedAction_ReturnsFailure()
    {
        var cap = _issuer.Issue(CapabilityActions.SaveInspect, "star_rupture");

        // Create a capability with modified action but original signature
        var tampered = new Capability
        {
            Id = cap.Id,
            Action = CapabilityActions.SaveModify, // Changed from SaveInspect
            GameScope = cap.GameScope,
            IssuedAt = cap.IssuedAt,
            ExpiresAt = cap.ExpiresAt,
            Signature = cap.Signature // Original signature for SaveInspect
        };

        var result = _validator.Validate(tampered, CapabilityActions.SaveModify, "star_rupture");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("signature");
    }

    [Fact]
    public void Validate_WithNonExpiring_ReturnsSuccess()
    {
        var cap = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture");

        cap.ExpiresAt.Should().BeNull();
        cap.IsExpired.Should().BeFalse();

        var result = _validator.Validate(cap, CapabilityActions.SaveModify, "star_rupture");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Issue_GeneratesUniqueIds()
    {
        var cap1 = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture");
        var cap2 = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture");

        cap1.Id.Should().NotBe(cap2.Id);
    }

    [Fact]
    public void Issue_GeneratesUniqueSignatures()
    {
        var cap1 = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture");
        var cap2 = _issuer.Issue(CapabilityActions.SaveModify, "star_rupture");

        cap1.Signature.Should().NotBe(cap2.Signature);
    }

    [Fact]
    public void Constructor_RejectsShortKey()
    {
        var shortKey = new byte[16]; // Too short
        var act = () => new CapabilityValidator(shortKey);
        act.Should().Throw<ArgumentException>();
    }
}
