using System.Security.Claims;
using InventarioCPR.Application.Photos;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using InventarioCPR.Infrastructure.Photos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class PhotoStageATests
{
    private static InventoryDbContext Context() => new(new DbContextOptionsBuilder<InventoryDbContext>()
        .UseNpgsql("Host=invalid.invalid;Database=not-used;Username=not-used;Password=not-used").Options);

    [Theory]
    [InlineData(SKEncodedImageFormat.Jpeg, "jpg")]
    [InlineData(SKEncodedImageFormat.Png, "png")]
    public async Task RealPixelsAreReencodedWithoutTrustingFileNameOrMime(SKEncodedImageFormat format, string expected)
    {
        using var bitmap = new SKBitmap(16, 12);
        bitmap.Erase(SKColors.Purple);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        using var stream = new MemoryStream(data.ToArray());
        var result = await PhotoImageProcessor.PrepareAsync(stream, default);
        Assert.Equal(expected, result.Format);
        Assert.Equal(16, result.Width);
        Assert.Equal(12, result.Height);
        Assert.Equal(64, result.Sha256.Length);
        Assert.NotEmpty(PhotoImageProcessor.Thumbnail(result.Bytes));
        using var repeated = new MemoryStream(data.ToArray());
        Assert.Equal(result.Sha256, (await PhotoImageProcessor.PrepareAsync(repeated, default)).Sha256);
    }

    [Fact]
    public async Task ExecutableTextAndOversizeStreamsAreRejectedBeforeStorage()
    {
        using var invalid = new MemoryStream("MZ not-a-photo.jpg"u8.ToArray());
        Assert.Equal(400, (await Assert.ThrowsAsync<PhotoException>(() => PhotoImageProcessor.PrepareAsync(invalid, default))).Status);
        using var oversize = new MemoryStream(new byte[PhotoRules.MaxBytes + 1]);
        Assert.Equal(413, (await Assert.ThrowsAsync<PhotoException>(() => PhotoImageProcessor.PrepareAsync(oversize, default))).Status);
    }

    [Fact]
    public async Task CameraExifOrientationIsAppliedThenRemoved()
    {
        using var bitmap = new SKBitmap(16, 12);
        bitmap.Erase(SKColors.Purple);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        byte[] exif = [0xFF,0xE1,0,34, 0x45,0x78,0x69,0x66,0,0,
            0x49,0x49,0x2A,0,8,0,0,0, 1,0, 0x12,1,3,0,1,0,0,0,6,0,0,0, 0,0,0,0];
        var jpeg = encoded.ToArray();
        using var input = new MemoryStream([.. jpeg.Take(2), .. exif, .. jpeg.Skip(2)]);
        var prepared = await PhotoImageProcessor.PrepareAsync(input, default);
        Assert.Equal(12, prepared.Width);
        Assert.Equal(16, prepared.Height);
        using var codec = SKCodec.Create(new SKMemoryStream(prepared.Bytes));
        Assert.Equal(SKEncodedOrigin.TopLeft, codec.EncodedOrigin);
    }

    [Fact]
    public async Task ExcessivePixelDimensionsAreRejectedBeforeDecodingPixels()
    {
        using var bitmap = new SKBitmap(4, 4);
        bitmap.Erase(SKColors.Purple);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var png = encoded.ToArray();
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(16, 4), 7000);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(png.AsSpan(20, 4), 4000);
        uint crc = 0xFFFFFFFF;
        foreach (var value in png.AsSpan(12, 17))
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) == 1 ? 0xEDB88320u : 0);
        }
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(png.AsSpan(29, 4), ~crc);
        using var codec = SKCodec.Create(new SKMemoryStream(png));
        Assert.Equal(7000, codec.Info.Width);
        using var input = new MemoryStream(png);
        await Assert.ThrowsAsync<PhotoException>(() => PhotoImageProcessor.PrepareAsync(input, default));
    }

    [Theory]
    [InlineData("MantenimientoAntes")]
    [InlineData("Daño")]
    [InlineData("Principal")]
    public void EventEvidenceCannotBeUploadedAsUnlinkedEquipmentPhotosInStageA(string type)
    {
        Assert.Throws<PhotoException>(() => PhotoRules.Validate(new PhotoDraft("test.png", type, "Prueba", "Archivo")));
    }

    [Fact]
    public async Task ExistingEvidenceCannotBeChangedOrDeletedEvenViaTrackedEntities()
    {
        await using var db = Context();
        var evidence = new PhotoEvidence(Guid.NewGuid(), "private/test", "General", DateTimeOffset.UtcNow);
        db.Attach(evidence);
        db.Entry(evidence).Property(x => x.CloudinaryPublicId).CurrentValue = "replacement";
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.Entry(evidence).State = EntityState.Deleted;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ClosedPrincipalPeriodCannotBeReopenedOrReassigned()
    {
        await using var db = Context();
        var period = new PhotoPrincipalPeriod
        {
            Id = Guid.NewGuid(), EquipmentId = Guid.NewGuid(), PhotoId = Guid.NewGuid(),
            FromUtc = DateTimeOffset.UtcNow.AddDays(-2), ToUtc = DateTimeOffset.UtcNow.AddDays(-1),
            ActorId = Guid.NewGuid(), ActorName = "Prueba"
        };
        db.Attach(period);
        period.ToUtc = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public void MigrationHasDatabaseEnforcedUniquenessAndSameEquipmentForeignKeysWithoutConnecting()
    {
        using var db = Context();
        var sql = db.GetService<IMigrator>().GenerateScript("InitialInventorySchema", "EquipmentPhotoHistory");
        Assert.Contains("CREATE UNIQUE INDEX \"IX_photo_principal_periods_EquipmentId\"", sql);
        Assert.Contains("WHERE \"ToUtc\" IS NULL", sql);
        Assert.Contains("FOREIGN KEY (\"PhotoId\", \"EquipmentId\") REFERENCES photo_evidence (\"Id\", \"EquipmentId\") ON DELETE RESTRICT", sql);
        Assert.Contains("FOREIGN KEY (\"EvidenceId\", \"EquipmentId\") REFERENCES photo_evidence (\"Id\", \"EquipmentId\") ON DELETE RESTRICT", sql);
        Assert.DoesNotContain("DELETE FROM", sql);
        Assert.DoesNotContain("UPDATE photo_evidence", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }

    [Fact]
    public async Task AuthorizationRunsBeforeAnyDatabaseOrCloudAccess()
    {
        var service = new EquipmentPhotoService(null!, new DeniedAccess(), null!, new ConfigurationBuilder().Build(), new PhotoWorkGate());
        var principal = new ClaimsPrincipal();
        var equipment = Guid.NewGuid();
        await Assert.ThrowsAsync<PhotoException>(() => service.ListAsync(principal, equipment, 0, default));
        await Assert.ThrowsAsync<PhotoException>(() => service.ReserveAsync(principal, equipment,
            new PhotoBatchRequest(Guid.NewGuid(), [new PhotoDraft("test.png", "General", "", "Archivo")]), default));
        await Assert.ThrowsAsync<PhotoException>(() => service.UploadAsync(principal, equipment, Guid.NewGuid(), Stream.Null, default));
        await Assert.ThrowsAsync<PhotoException>(() => service.SetPrincipalAsync(principal, equipment, Guid.NewGuid(), default));
        await Assert.ThrowsAsync<PhotoException>(() => service.ReadAsync(principal, equipment, Guid.NewGuid(), false, default));
    }

    [Fact]
    public async Task FeatureRemainsOffWithoutMigrationApprovalAndDoesNotQueryNewTables()
    {
        var service = new EquipmentPhotoService(null!, new AllowedAccess(), null!, new ConfigurationBuilder().Build(), new PhotoWorkGate());
        var page = await service.ListAsync(new ClaimsPrincipal(), Guid.NewGuid(), 0, default);
        Assert.False(page.Enabled);
        Assert.False(page.CanWrite);
        Assert.Empty(page.Items);
    }

    private sealed class DeniedAccess : IPhotoAccess
    {
        public Task<PhotoActor> AuthorizeAsync(ClaimsPrincipal principal, bool write) => throw new PhotoException("Denegado", 403);
    }

    [Fact]
    public async Task CancelledProcessingWaitDoesNotLeakOrReleaseAnotherRequestsLease()
    {
        var gate = new PhotoWorkGate();
        var first = await gate.EnterAsync(default);
        using var cancelled = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => gate.EnterAsync(cancelled.Token));
        first.Dispose();
        using var next = await gate.EnterAsync(default);
    }
    private sealed class AllowedAccess : IPhotoAccess
    {
        public Task<PhotoActor> AuthorizeAsync(ClaimsPrincipal principal, bool write) => Task.FromResult(new PhotoActor(Guid.NewGuid(), "Prueba", true));
    }
}
