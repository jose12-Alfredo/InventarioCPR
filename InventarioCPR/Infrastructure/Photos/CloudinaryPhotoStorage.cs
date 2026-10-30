using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventarioCPR.Application.Photos;
using SkiaSharp;

namespace InventarioCPR.Infrastructure.Photos;

public sealed class CloudinaryPhotoStorage(IConfiguration configuration) : IPhotoStorage
{
    private const int MaxBytes = 10 * 1024 * 1024;
    private const long MaxPixels = 25_000_000;

    public async Task<string> UploadAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
            throw new ArgumentException("Solo se admiten fotografías JPEG o PNG.", nameof(fileName));

        await using var input = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (input.Length + read > MaxBytes)
                throw new ArgumentException("La fotografía supera el límite de 10 MB.", nameof(content));
            await input.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        input.Position = 0;
        using var codec = SKCodec.Create(new SKManagedStream(input, false));
        var expectedFormat = extension == ".png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        if (codec is null || codec.Info.Width < 1 || codec.Info.Height < 1 ||
            (long)codec.Info.Width * codec.Info.Height > MaxPixels || codec.EncodedFormat != expectedFormat)
            throw new ArgumentException("El contenido de la fotografía no coincide con un formato permitido.", nameof(content));
        if (codec.FrameCount != 1)
            throw new ArgumentException("No se admiten fotografías animadas.", nameof(content));

        // Los píxeles decodificados se vuelven a codificar; no se copian EXIF ni otros metadatos originales.
        using var bitmap = SKBitmap.Decode(codec);
        if (bitmap is null)
            throw new ArgumentException("No se pudo decodificar la fotografía.", nameof(content));
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(expectedFormat, expectedFormat == SKEncodedImageFormat.Jpeg ? 90 : 100);
        if (encoded is null)
            throw new ArgumentException("No se pudo procesar la fotografía.", nameof(content));
        await using var sanitized = new MemoryStream();
        encoded.SaveTo(sanitized);
        sanitized.Position = 0;

        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];
        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            throw new InvalidOperationException("Configure Cloudinary en User Secrets.");

        var cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
        var publicId = $"inventario/equipos/{Guid.NewGuid():N}";
        var result = await cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription("evidencia" + (extension == ".png" ? ".png" : ".jpg"), sanitized),
            PublicId = publicId,
            Type = "authenticated",
            Overwrite = false,
            UniqueFilename = false
        }, cancellationToken);

        if (result.Error is not null || string.IsNullOrWhiteSpace(result.PublicId))
            throw new InvalidOperationException("No se pudo almacenar la fotografía.");
        return result.PublicId;
    }
}
