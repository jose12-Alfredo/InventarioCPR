using InventarioCPR.Application.Photos;
using SkiaSharp;

namespace InventarioCPR.Infrastructure.Photos;

public static class PhotoImageProcessor
{
    public static async Task<byte[]> ReadBoundedAsync(Stream stream, CancellationToken ct)
    {
        using var input = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) != 0)
        {
            if (input.Length + read > PhotoRules.MaxBytes)
                throw new PhotoException("La imagen supera el límite de 10 MB.", 413);
            await input.WriteAsync(buffer.AsMemory(0, read), ct);
        }
        return input.ToArray();
    }

    public static async Task<PhotoContent> PrepareAsync(Stream stream, CancellationToken ct)
    {
        var bytes = await ReadBoundedAsync(stream, ct);
        using var codec = SKCodec.Create(new SKMemoryStream(bytes));
        if (codec is null || codec.Info.Width < 1 || codec.Info.Height < 1 ||
            (long)codec.Info.Width * codec.Info.Height > PhotoRules.MaxPixels || codec.FrameCount > 1 ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
            throw new PhotoException("Seleccione una imagen JPEG o PNG válida, no animada y de hasta 25 megapíxeles. HEIC no está disponible.", 400);
        using var decoded = SKBitmap.Decode(codec);
        if (decoded is null) throw new PhotoException("No se pudo decodificar la imagen.", 400);
        var swapped = codec.EncodedOrigin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        using var oriented = new SKBitmap(swapped ? decoded.Height : decoded.Width, swapped ? decoded.Width : decoded.Height);
        using (var canvas = new SKCanvas(oriented))
        {
            var w = decoded.Width;
            var h = decoded.Height;
            var matrix = codec.EncodedOrigin switch
            {
                SKEncodedOrigin.TopRight => new SKMatrix { ScaleX = -1, ScaleY = 1, TransX = w, Persp2 = 1 },
                SKEncodedOrigin.BottomRight => new SKMatrix { ScaleX = -1, ScaleY = -1, TransX = w, TransY = h, Persp2 = 1 },
                SKEncodedOrigin.BottomLeft => new SKMatrix { ScaleX = 1, ScaleY = -1, TransY = h, Persp2 = 1 },
                SKEncodedOrigin.LeftTop => new SKMatrix { SkewX = 1, SkewY = 1, Persp2 = 1 },
                SKEncodedOrigin.RightTop => new SKMatrix { SkewX = -1, SkewY = 1, TransX = h, Persp2 = 1 },
                SKEncodedOrigin.RightBottom => new SKMatrix { SkewX = -1, SkewY = -1, TransX = h, TransY = w, Persp2 = 1 },
                SKEncodedOrigin.LeftBottom => new SKMatrix { SkewX = 1, SkewY = -1, TransY = w, Persp2 = 1 },
                _ => SKMatrix.Identity
            };
            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(decoded, 0, 0);
        }
        using var image = SKImage.FromBitmap(oriented);
        using var encoded = image.Encode(codec.EncodedFormat, 90);
        if (encoded is null || encoded.Size > PhotoRules.MaxBytes)
            throw new PhotoException("La imagen procesada supera el límite de 10 MB.", 413);
        return new PhotoContent(encoded.ToArray(), codec.EncodedFormat == SKEncodedImageFormat.Png ? "png" : "jpg", oriented.Width, oriented.Height);
    }

    public static byte[] Thumbnail(byte[] bytes)
    {
        ValidateStored(bytes);
        using var original = SKBitmap.Decode(bytes) ?? throw new PhotoException("No se pudo leer la fotografía.", 502);
        var ratio = Math.Min(1, Math.Min(320d / original.Width, 240d / original.Height));
        using var resized = original.Resize(new SKImageInfo(Math.Max(1, (int)(original.Width * ratio)), Math.Max(1, (int)(original.Height * ratio))), new SKSamplingOptions(SKFilterMode.Linear));
        if (resized is null) throw new PhotoException("No se pudo crear la miniatura.", 502);
        using var image = SKImage.FromBitmap(resized);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return encoded.ToArray();
    }

    public static string ValidateStored(byte[] bytes)
    {
        using var codec = SKCodec.Create(new SKMemoryStream(bytes));
        if (codec is null || codec.Info.Width < 1 || codec.Info.Height < 1 ||
            (long)codec.Info.Width * codec.Info.Height > PhotoRules.MaxPixels || codec.FrameCount > 1 ||
            codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png))
            throw new PhotoException("La evidencia almacenada no cumple el formato o los límites de imagen.", 415);
        return codec.EncodedFormat == SKEncodedImageFormat.Png ? "image/png" : "image/jpeg";
    }
}
