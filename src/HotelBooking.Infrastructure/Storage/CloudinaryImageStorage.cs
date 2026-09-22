using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Infrastructure.Storage.Options;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Storage;

public sealed class CloudinaryImageStorage : IImageStorage
{
    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;

    public CloudinaryImageStorage(IOptions<CloudinaryOptions> options)
    {
        _options = options.Value;
        var account = new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<StoredImage> UploadAsync(
        Stream content,
        string contentType,
        string folder,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullFolder = string.IsNullOrWhiteSpace(_options.DefaultFolder)
            ? folder
            : $"{_options.DefaultFolder}/{folder}";

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription($"{Guid.NewGuid():N}", content),
            Folder = fullFolder,
            UseFilename = false,
            UniqueFilename = true,
            Overwrite = false,
        };

        var result = await _cloudinary.UploadAsync(uploadParams, cancellationToken);

        if (result.Error is not null)
            throw new InvalidOperationException(
                $"Cloudinary upload failed: {result.Error.Message}");

        return new StoredImage(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

        if (result.Error is not null)
            throw new InvalidOperationException(
                $"Cloudinary delete failed: {result.Error.Message}");
    }
}