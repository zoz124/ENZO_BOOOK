using System.Reflection;
using FluentAssertions;
using Xunit;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Guard")]
public class DependencyTests
{
    [Fact]
    public void Core_ShouldNotDependOn_Infrastructure()
    {
        // Arrange
        var coreAssembly = Assembly.Load("BookNest.Core");
        const string infrastructureAssemblyName = "BookNest.Infrastructure";

        // Act
        var references = coreAssembly.GetReferencedAssemblies().Select(a => a.Name);

        // Assert
        references.Should().NotContain(infrastructureAssemblyName,
            "Core layer should not depend on Infrastructure");
    }

    [Fact]
    public void Core_ShouldNotDependOn_Web()
    {
        // Arrange
        var coreAssembly = Assembly.Load("BookNest.Core");
        const string webAssemblyName = "BookNest.Web";

        // Act
        var references = coreAssembly.GetReferencedAssemblies().Select(a => a.Name);

        // Assert
        references.Should().NotContain(webAssemblyName,
            "Core layer should not depend on Web");
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Web()
    {
        // Arrange
        var infrastructureAssembly = Assembly.Load("BookNest.Infrastructure");
        const string webAssemblyName = "BookNest.Web";

        // Act
        var references = infrastructureAssembly.GetReferencedAssemblies().Select(a => a.Name);

        // Assert
        references.Should().NotContain(webAssemblyName,
            "Infrastructure should not depend on Web");
    }
}
