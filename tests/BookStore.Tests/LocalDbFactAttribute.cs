using Xunit;

namespace BookStore.Tests;

/// <summary>LocalDB only exists on Windows; other agents skip instead of failing.</summary>
public sealed class LocalDbFactAttribute : FactAttribute
{
    public LocalDbFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "SQL Server LocalDB is only available on Windows.";
        }
    }
}
