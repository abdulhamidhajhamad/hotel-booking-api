namespace HotelBooking.Application.Abstractions.Storage;

public sealed record StoredImage(string Url, string PublicId);

public interface IImageStorage
{
    Task<StoredImage> UploadAsync(
        Stream content,
        string contentType,
        string folder,
        CancellationToken cancellationToken);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken);
}