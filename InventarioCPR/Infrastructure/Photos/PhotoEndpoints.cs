using InventarioCPR.Application.Photos;
using InventarioCPR.Application.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;

namespace InventarioCPR.Infrastructure.Photos;

public static class PhotoEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/equipment/{equipment:guid}/photos")
            .RequireAuthorization(InventoryPolicies.Read);
        group.AddEndpointFilter(async (invocation, next) =>
        {
            var context = invocation.HttpContext;
            context.Response.Headers.CacheControl = "private, no-store, max-age=0";
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            try
            {
                if (!HttpMethods.IsGet(context.Request.Method))
                {
                    var limit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
                    if (limit is { IsReadOnly: false }) limit.MaxRequestBodySize =
                        context.Request.Path.Value?.EndsWith("/content", StringComparison.Ordinal) == true ? PhotoRules.MaxBytes : 65536;
                    await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
                }
                return await next(invocation);
            }
            catch (PhotoException ex) { return Results.Json(new { error = ex.Message }, statusCode: ex.Status); }
            catch (AntiforgeryValidationException) { return Results.Json(new { error = "La sesión cambió. Recargue antes de continuar." }, statusCode: 400); }
            catch (BadHttpRequestException) { return Results.Json(new { error = "El archivo o la solicitud supera el límite permitido." }, statusCode: 413); }
            catch (System.Text.Json.JsonException) { return Results.Json(new { error = "Solicitud de fotografías inválida." }, statusCode: 400); }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { return Results.StatusCode(499); }
            catch (Exception ex)
            {
                app.Logger.LogWarning("Operación de fotografías no confirmada ({ErrorType}).", ex.GetType().Name);
                return Results.Json(new { error = "No se confirmó la operación. Recargue la galería o reintente el mismo archivo; no se sobrescribirá ninguna evidencia." }, statusCode: 503);
            }
        });

        group.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) =>
            Results.Json(new { token = antiforgery.GetAndStoreTokens(context).RequestToken }));
        group.MapGet("", async (Guid equipment, int? page, HttpContext context, EquipmentPhotoService service) =>
            Results.Json(await service.ListAsync(context.User, equipment, page ?? 0, context.RequestAborted)));
        group.MapPost("/batches", async (Guid equipment, HttpContext context, EquipmentPhotoService service) =>
        {
            var request = await context.Request.ReadFromJsonAsync<PhotoBatchRequest>(context.RequestAborted)
                ?? throw new PhotoException("Seleccione un lote válido.", 400);
            return Results.Json(await service.ReserveAsync(context.User, equipment, request, context.RequestAborted));
        }).RequireAuthorization(InventoryPolicies.Write).RequireRateLimiting("photo-writes");
        group.MapPost("/uploads/{upload:guid}/content", async (Guid equipment, Guid upload, HttpContext context, EquipmentPhotoService service) =>
        {
            if (context.Request.ContentLength is > PhotoRules.MaxBytes)
                throw new PhotoException("La imagen supera el límite de 10 MB.", 413);
            return Results.Json(await service.UploadAsync(context.User, equipment, upload, context.Request.Body, context.RequestAborted));
        }).RequireAuthorization(InventoryPolicies.Write).RequireRateLimiting("photo-writes");
        group.MapPost("/{photo:guid}/principal", async (Guid equipment, Guid photo, HttpContext context, EquipmentPhotoService service) =>
        {
            await service.SetPrincipalAsync(context.User, equipment, photo, context.RequestAborted);
            return Results.NoContent();
        }).RequireAuthorization(InventoryPolicies.Write).RequireRateLimiting("photo-writes");
        group.MapGet("/{photo:guid}/content", async (Guid equipment, Guid photo, string? size, HttpContext context, EquipmentPhotoService service) =>
        {
            if (size is not (null or "thumb")) throw new PhotoException("Tamaño de imagen no permitido.", 400);
            var result = await service.ReadAsync(context.User, equipment, photo, size == "thumb", context.RequestAborted);
            return Results.Stream(new MemoryStream(result.Bytes, false), result.ContentType, enableRangeProcessing: false);
        }).RequireRateLimiting("photo-downloads");
    }
}
