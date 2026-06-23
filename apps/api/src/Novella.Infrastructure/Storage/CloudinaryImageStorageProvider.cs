using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novella.Application.Abstractions;
using Novella.Application.Common;
using Novella.Infrastructure.Configuration;
using AppImageUploadResult = Novella.Application.Abstractions.ImageUploadResult;

namespace Novella.Infrastructure.Storage;

/// <summary>
/// Cloudinary-backed image storage. The Cloudinary secret never leaves the server; only the
/// secure URL and public id are returned/stored.
/// </summary>
public sealed class CloudinaryImageStorageProvider : IImageStorageProvider
{
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryImageStorageProvider> _logger;
    private readonly Cloudinary? _cloudinary;

    public CloudinaryImageStorageProvider(IOptions<CloudinaryOptions> options, ILogger<CloudinaryImageStorageProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
        if (_options.IsConfigured)
        {
            _cloudinary = new Cloudinary(new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret))
            {
                Api = { Secure = true }
            };
        }
    }

    public async Task<AppImageUploadResult> UploadAsync(Stream content, string fileName, string folder, CancellationToken ct = default)
    {
        if (_cloudinary is null)
            throw new AppException(ErrorCodes.UploadFailed, "Cloudinary is not configured.", 503);

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);
        if (result.Error is not null || string.IsNullOrEmpty(result.SecureUrl?.ToString()))
        {
            _logger.LogWarning("Cloudinary upload failed: {Error}", result.Error?.Message);
            throw new AppException(ErrorCodes.UploadFailed, "Image upload failed.", 502);
        }

        return new AppImageUploadResult(result.SecureUrl!.ToString(), result.PublicId);
    }
}
