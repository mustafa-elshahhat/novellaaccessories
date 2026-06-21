using Novella.Application.Abstractions;
using Novella.Application.Common;

namespace Novella.Application.Uploads;

public sealed record UploadedImageDto(string Url, string PublicId);

/// <summary>
/// Admin image uploads via <see cref="IImageStorageProvider"/>. The Cloudinary secret stays
/// server-side; only the secure URL + public id are returned. Enforces the folder convention.
/// </summary>
public sealed class UploadService
{
    private readonly IImageStorageProvider _storage;

    public UploadService(IImageStorageProvider storage) => _storage = storage;

    /// <summary>Builds a Cloudinary folder per the convention novella/{entityType}/{entityId}/.</summary>
    public static string BuildFolder(string? entityType, string? entityId)
    {
        var type = (entityType ?? "misc").Trim('/');
        return string.IsNullOrWhiteSpace(entityId) ? $"novella/{type}" : $"novella/{type}/{entityId.Trim('/')}";
    }

    public async Task<UploadedImageDto> UploadAsync(Stream content, string fileName, string? entityType, string? entityId, CancellationToken ct)
    {
        if (content is null || string.IsNullOrWhiteSpace(fileName))
            throw AppException.Validation("A valid image file is required.");
        var folder = BuildFolder(entityType, entityId);
        var result = await _storage.UploadAsync(content, fileName, folder, ct);
        return new UploadedImageDto(result.Url, result.PublicId);
    }

    public async Task DeleteAsync(string publicId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw AppException.Validation("publicId is required.");
        await _storage.DeleteAsync(publicId, ct);
    }
}
