namespace HotelBooking.Application.Common.Storage;

public sealed record UploadImageFile(
    Stream Content,
    string ContentType,
    string FileName);