using Xunit;

namespace BookNest.IntegrationTests;

/// <summary>
/// Testcontainers needs a running Docker engine. Without one these tests skip
/// instead of failing, so `dotnet test` stays green on a plain developer machine.
/// </summary>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerProbe.IsAvailable)
        {
            Skip = "Docker is not running; Testcontainers tests need a Docker engine.";
        }
    }
}

internal static class DockerProbe
{
    public static bool IsAvailable => OperatingSystem.IsWindows()
        ? File.Exists(@"\\.\pipe\docker_engine")
        : File.Exists("/var/run/docker.sock") ||
          !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOCKER_HOST"));
}
