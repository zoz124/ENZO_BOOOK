using System.Reflection;
using BookNest.Core.Entities;
using FluentAssertions;
using Xunit;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Security")]
public class EntitySecurityTests
{
    [Fact]
    public void Entities_StringProperties_MustHaveMaxLength()
    {
        // Arrange
        var entityTypes = typeof(Book).Assembly.GetTypes()
            .Where(t => t.Namespace == "BookNest.Core.Entities"
                        && !t.IsAbstract
                        && t.IsClass);

        var violations = new List<string>();

        // Act
        foreach (var entityType in entityTypes)
        {
            var stringProperties = entityType.GetProperties()
                .Where(p => p.PropertyType == typeof(string));

            foreach (var property in stringProperties)
            {
                var hasMaxLength = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.MaxLengthAttribute>() != null;
                var hasStringLength = property.GetCustomAttribute<System.ComponentModel.DataAnnotations.StringLengthAttribute>() != null;

                if (!hasMaxLength && !hasStringLength)
                {
                    violations.Add($"{entityType.Name}.{property.Name}");
                }
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "All string properties must have MaxLength or StringLength attribute. " +
            $"Violations: {string.Join(", ", violations)}");
    }
}
