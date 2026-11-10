namespace InventarioCPR.Application.Photos;

public interface IPhotoStorage
{
    Task<StoredPhoto> StoreAsync(PhotoContent content, string publicId, CancellationToken cancellationToken = default);
    Task<byte[]> ReadAsync(string publicId, long? version, string? format, CancellationToken cancellationToken = default);
}
