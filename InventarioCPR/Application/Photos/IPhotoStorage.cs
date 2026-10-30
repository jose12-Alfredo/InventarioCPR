namespace InventarioCPR.Application.Photos;

public interface IPhotoStorage
{
    // Solo infraestructura; el servicio de aplicación deberá autorizar y persistir metadatos.
    Task<string> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}
