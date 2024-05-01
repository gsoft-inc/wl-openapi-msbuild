namespace Workleap.OpenApi.MSBuild.Tests;

public class SpectralDiffCalculatorTests
{
    private const string RulesetV1 = "./Spectral/v1/spectral-ruleset.yaml";
    private const string RulesetV2 = "./Spectral/v2/spectral-ruleset.yaml";
    private const string OpenApiDocumentV1 = "./Spectral/v1/openapi.yaml";
    private const string OpenApiDocumentV2 = "./Spectral/v2/openapi.yaml";
    private const string OpenApiDocumentAdminV2 = "./Spectral/v2/openapi-admin.yaml";
    
    private readonly SpectralDiffCalculator _spectralDiffCalculator;

    public SpectralDiffCalculatorTests()
    {
        this._spectralDiffCalculator = new SpectralDiffCalculator(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
    }
    
    [Fact]
    public void Given_Nothing_Saved_When_HasRulesetChangedSinceLastExecution_Then_True()
    {
        // Given

        // When
        var hasChanged = this._spectralDiffCalculator.HasRulesetChangedSinceLastExecution(RulesetV1);

        // Assert
        Assert.True(hasChanged);
    }
    
    [Fact]
    public void Given_Spectral_Rules_Saved_When_HasRulesetChangedSinceLastExecution_With_Same_Ruleset_Then_False()
    {
        // Given
        this._spectralDiffCalculator.SaveCurrentExecutionChecksum(RulesetV1, Array.Empty<string>());

        // When
        var hasChanged = this._spectralDiffCalculator.HasRulesetChangedSinceLastExecution(RulesetV1);

        // Assert
        Assert.False(hasChanged);
    }
    
    [Fact]
    public void Given_Spectral_Rules_Saved_When_HasRulesetChangedSinceLastExecution_With_Other_Ruleset_Then_True()
    {
        // Given
        this._spectralDiffCalculator.SaveCurrentExecutionChecksum(RulesetV1, Array.Empty<string>());

        // When
        var hasChanged = this._spectralDiffCalculator.HasRulesetChangedSinceLastExecution(RulesetV2);

        // Assert
        Assert.True(hasChanged);
    }
    
    [Fact]
    public void Given_Nothing_Saved_When_HasOpenApiDocumentChangedSinceLastExecution_Then_True()
    {
        // Given

        // When
        var hasChanged = this._spectralDiffCalculator.HasOpenApiDocumentChangedSinceLastExecution(new[] { OpenApiDocumentV1 });

        // Assert
        Assert.True(hasChanged);
    }
    
    [Fact]
    public void Given_OpenApi_Document_Saved_When_HasRulesetChangedSinceLastExecution_With_Same_Ruleset_Then_False()
    {
        // Given
        this._spectralDiffCalculator.SaveCurrentExecutionChecksum(RulesetV1, new[] { OpenApiDocumentV1 });

        // When
        var hasChanged = this._spectralDiffCalculator.HasOpenApiDocumentChangedSinceLastExecution(new[] { OpenApiDocumentV1 });

        // Assert
        Assert.False(hasChanged);
    }
    
    [Fact]
    public void Given_OpenApi_Document_Saved_When_HasRulesetChangedSinceLastExecution_With_New_Version_Then_True()
    {
        // Given
        this._spectralDiffCalculator.SaveCurrentExecutionChecksum(RulesetV1, new[] { OpenApiDocumentV1 });

        // When
        var hasChanged = this._spectralDiffCalculator.HasOpenApiDocumentChangedSinceLastExecution(new[] { OpenApiDocumentV2 });

        // Assert
        Assert.True(hasChanged);
    }
    
    [Fact]
    public void Given_OpenApi_Document_Saved_When_HasRulesetChangedSinceLastExecution_With_Extra_Version_Then_True()
    {
        // Given
        this._spectralDiffCalculator.SaveCurrentExecutionChecksum(RulesetV1, new[] { OpenApiDocumentV1 });

        // When
        var hasChanged = this._spectralDiffCalculator.HasOpenApiDocumentChangedSinceLastExecution(new[] { OpenApiDocumentV1, OpenApiDocumentAdminV2 });

        // Assert
        Assert.True(hasChanged);
    }
}