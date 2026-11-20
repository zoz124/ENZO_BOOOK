using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Security")]
public class PackageSecurityTests
{
    [Fact]
    public void Solution_ShouldNotHave_VulnerablePackages()
    {
        // Arrange
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "list package --vulnerable --include-transitive",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = GetSolutionDirectory()
            }
        };

        // Act
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        // Assert
        output.Should().NotContain("has the following vulnerable packages",
            "Solution should not contain packages with known vulnerabilities");
    }

    private static string GetSolutionDirectory()
    {
        var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        while (directory != null && !directory.GetFiles("*.sln").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Solution file not found");
    }
}
