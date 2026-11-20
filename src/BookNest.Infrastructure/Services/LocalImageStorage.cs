using BookNest.Core.Constants;
using BookNest.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace BookNest.Infrastructure.Services;

/// <summary>
/// Stores covers on disk under wwwroot/uploads/covers.
/// Validation is content-based (magic bytes), not extension-based (NFR-12).
/// </summary>
public class LocalImageStorage : IImageStorage
{
    private readonly string _rootPath;
    private readonly string _requestPath;
    private readonly ILogger<LocalImageStorage> _logger;

    public LocalImageStorage(string rootPath, string requestPath, ILogger<LocalImageStorage> logger)
    {
        _rootPath = rootPath;
        _requestPath = requestPath;
        _logger = logger;
    }

    public async Task<string?> SaveCoverAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0 || buffer.Length > StoreSettings.MaxUploadBytes)
        {
            return null;
        }

        var bytes = buffer.ToArray();
        var extension = DetectExtension(bytes);

        if (extension is null)
        {
            _logger.LogWarning("Rejected cover upload {FileName}: unknown magic bytes.", fileName);
            return null;
        }

        var directory = Path.Combine(_rootPath, "uploads", "covers");
        Directory.CreateDirectory(directory);

        var safeName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, safeName);

        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

        return $"{_requestPath.TrimEnd('/')}/uploads/covers/{safeName}";
    }

    public void Delete(string? publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            return;
        }

        var fileName = Path.GetFileName(publicUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return;
        }

        var path = Path.Combine(_rootPath, "uploads", "covers", fileName);

        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static string? DetectExtension(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return ".jpg";
        }

        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
        {
            return ".png";
        }

        if (bytes.Length >= 12 &&
            bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' &&
            bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P')
        {
            return ".webp";
        }

        return null;
    }
}
