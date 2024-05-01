namespace Workleap.OpenApi.MSBuild.Unit.Tests;

public class UnitTest1
{

    [Fact]
    public void HasRulesetChangedSinceLastExecution_ReturnsExpectedResult()
    {
        // Arrange
        var spectralDiffCalculator = new SpectralDiffCalculator("<your_directory_path>");
        string spectralRulset = "<your_spectral_ruleset>";

        // Act
        var result = spectralDiffCalculator.HasRulesetChangedSinceLastExecution(spectralRulset);

        // Assert
        // Replace 'expectedResult' with the expected result
        bool expectedResult = true; 
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void HasOpenApiDocumentChangedSinceLastExecution_ReturnsExpectedResult()
    {
        // Arrange
        var spectralDiffCalculator = new SpectralDiffCalculator("<your_directory_path>");
        IReadOnlyCollection<string> openApiDocumentPaths = new List<string> { "<your_document_path>" };

        // Act
        var result = spectralDiffCalculator.HasOpenApiDocumentChangedSinceLastExecution(openApiDocumentPaths);

        // Assert
        // Replace 'expectedResult' with the expected result
        bool expectedResult = true; 
        Assert.Equal(expectedResult, result);
    }
}