using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InventarioCPR.Application.Photos;
using System.Net;
using System.Security.Cryptography;

namespace InventarioCPR.Infrastructure.Photos;

public sealed class CloudinaryPhotoStorage(IConfiguration configuration, IHttpClientFactory clients) : IPhotoStorage
{
    public async Task<StoredPhoto> StoreAsync(PhotoContent content, string publicId, CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var existing = await client.GetResourceAsync(new GetResourceParams(publicId) { Type = "authenticated", ResourceType = ResourceType.Image }, cancellationToken);
        if (existing.Error is null)
        {
            // Recover a previous successful cloud write after a lost response / failed DB commit.
            var previous = await ReadAsync(publicId, long.Parse(existing.Version, System.Globalization.CultureInfo.InvariantCulture), content.Format, cancellationToken);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(previous), SHA256.HashData(content.Bytes)))
                throw new PhotoException("La reserva ya contiene otra imagen; no se sobrescribirá.");
            return new StoredPhoto(publicId, long.Parse(existing.Version, System.Globalization.CultureInfo.InvariantCulture));
        }
        if (existing.StatusCode != HttpStatusCode.NotFound)
            throw new PhotoException("Cloudinary no pudo comprobar la reserva. Reintente la misma imagen.", 502);

        using var stream = new MemoryStream(content.Bytes, false);
        var result = await client.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription("evidencia." + content.Format, stream),
            PublicId = publicId, Type = "authenticated", Overwrite = false, UniqueFilename = false,
            AssetFolder = configuration["Cloudinary:TestAssetFolder"]
        }, cancellationToken);
        if (result.Error is not null || result.PublicId != publicId)
            throw new PhotoException("No se confirmó la subida. Reintente el mismo archivo para recuperar la operación.", 502);
        return new StoredPhoto(publicId, long.Parse(result.Version, System.Globalization.CultureInfo.InvariantCulture));
    }

    public async Task<byte[]> ReadAsync(string publicId, long? version, string? format, CancellationToken cancellationToken = default)
    {
        var cloudinary = CreateClient();
        if (version is null || format is null)
        {
            var resource = await cloudinary.GetResourceAsync(new GetResourceParams(publicId) { Type = "authenticated", ResourceType = ResourceType.Image }, cancellationToken);
            if (resource.Error is not null) throw new PhotoException("La imagen privada no está disponible.", 502);
            version = long.Parse(resource.Version, System.Globalization.CultureInfo.InvariantCulture);
            format = resource.Format;
        }
        if (format is not ("jpg" or "jpeg" or "png")) throw new PhotoException("El formato histórico no está habilitado.", 415);
        var url = cloudinary.Api.UrlImgUp.Type("authenticated").Signed(true).Secure(true)
            .Version(version.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)).Format(format).BuildUrl(publicId);
        var uri = new Uri(url);
        if (uri.Scheme != "https" || uri.Host != "res.cloudinary.com")
            throw new PhotoException("Destino de almacenamiento no permitido.", 502);
        // This signed URL is server-only. No redirects, public URLs or credentials are returned to a browser.
        using var response = await clients.CreateClient("private-photos").GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new PhotoException("No se pudo recuperar la imagen privada.", 502);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await PhotoImageProcessor.ReadBoundedAsync(stream, cancellationToken);
    }

    private Cloudinary CreateClient()
    {
        var name = configuration["Cloudinary:CloudName"];
        var key = configuration["Cloudinary:ApiKey"];
        var secret = configuration["Cloudinary:ApiSecret"];
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
            throw new PhotoException("Cloudinary no está configurado en el servidor.", 503);
        return new Cloudinary(new Account(name, key, secret));
    }
}
