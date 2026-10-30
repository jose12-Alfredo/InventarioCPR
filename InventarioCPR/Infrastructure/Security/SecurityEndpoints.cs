using System.Text;
using System.Text.Encodings.Web;
using InventarioCPR.Application.Security;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace InventarioCPR.Infrastructure.Security;

public static class SecurityEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/acceso", (HttpContext context, IAntiforgery antiforgery) =>
        {
            var token = HtmlEncoder.Default.Encode(antiforgery.GetAndStoreTokens(context).RequestToken ?? "");
            var failed = context.Request.Query.ContainsKey("error") ? "<p role=\"alert\">No se pudo iniciar sesión. Compruebe los datos o espere si la cuenta está bloqueada.</p>" : "";
            return Results.Content($$"""
                <!doctype html><html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover">
                <title>Acceso | InventarioCPR</title><style>
                body{font:1rem/1.5 system-ui,sans-serif;background:#f6f7fb;color:#24262b;min-height:100dvh;display:grid;place-items:center;margin:0;padding:1rem}
                main{width:min(100%,27rem);background:white;border:1px solid #dee1e8;border-radius:.75rem;padding:2rem}
                h1{font-size:1.6rem;margin:0 0 .5rem}p{color:#626773}label{display:block;margin:1rem 0 .35rem;font-weight:650}
                input{box-sizing:border-box;width:100%;min-height:2.75rem;border:1px solid #dee1e8;border-radius:.5rem;padding:.65rem}
                button{width:100%;min-height:2.875rem;margin-top:1.5rem;background:#6d0dff;color:white;border:0;border-radius:.5rem;font-weight:700;cursor:pointer}
                :focus-visible{outline:2px solid #6d0dff;outline-offset:3px}
                </style></head><body><main><h1>InventarioCPR</h1><p>Acceso para cuentas autorizadas.</p>{{failed}}
                <form method="post" action="/acceso"><input type="hidden" name="__RequestVerificationToken" value="{{token}}">
                <label for="usuario">Usuario o correo electrónico</label><input id="usuario" name="usuario" autocomplete="username" required maxlength="256">
                <label for="clave">Contraseña</label><input id="clave" name="clave" type="password" autocomplete="current-password" required>
                <button type="submit">Iniciar sesión</button></form></main></body></html>
                """, "text/html; charset=utf-8");
        }).AllowAnonymous();

        app.MapPost("/acceso", async (HttpContext context, IAntiforgery antiforgery,
            UserManager<AccessUser> users, SignInManager<AccessUser> signIn) =>
        {
            if (!await ValidateAsync(context, antiforgery)) return Results.BadRequest();
            var form = await context.Request.ReadFormAsync();
            var name = form["usuario"].ToString().Trim();
            var password = form["clave"].ToString();
            if (name.Length > 256 || password.Length > 1024) return Results.Redirect("/acceso?error=1");
            var account = await users.FindByNameAsync(name);
            if (account is null && name.Contains('@'))
                account = await users.FindByEmailAsync(name);
            if (account?.IsActive != true) return Results.Redirect("/acceso?error=1");
            var result = await signIn.PasswordSignInAsync(account, password, isPersistent: false, lockoutOnFailure: true);
            return Results.Redirect(result.Succeeded ? "/inventario" : "/acceso?error=1");
        }).AllowAnonymous().RequireRateLimiting("login");

        app.MapPost("/salir", async (HttpContext context, IAntiforgery antiforgery,
            SignInManager<AccessUser> signIn, UserManager<AccessUser> users) =>
        {
            if (!await ValidateAsync(context, antiforgery)) return Results.BadRequest();
            var account = await users.GetUserAsync(context.User);
            if (account is not null) await users.UpdateSecurityStampAsync(account);
            await signIn.SignOutAsync();
            return Results.Redirect("/acceso");
        }).RequireAuthorization();

        app.MapGet("/acceso-denegado", () => Results.Content(
            "No tiene permiso para acceder al inventario. Consulte al administrador.", "text/plain; charset=utf-8"))
            .RequireAuthorization();

        app.MapGet("/admin/usuarios", async (HttpContext context, IAntiforgery antiforgery,
            UserManager<AccessUser> users) =>
        {
            var token = HtmlEncoder.Default.Encode(antiforgery.GetAndStoreTokens(context).RequestToken ?? "");
            var accounts = await users.Users.AsNoTracking().OrderBy(x => x.UserName).Take(100).ToListAsync();
            var html = new StringBuilder("<!doctype html><html lang='es'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'><title>Usuarios | InventarioCPR</title></head><body><main><h1>Administrar cuentas</h1><p>Las cuentas de acceso no representan empleados ni custodias de equipos.</p>");
            html.Append("<form method='post' action='/admin/usuarios'><input type='hidden' name='__RequestVerificationToken' value='").Append(token).Append("'>")
                .Append("<label>Usuario <input name='usuario' required maxlength='256'></label><label>Correo <input name='correo' type='email' required maxlength='256'></label>")
                .Append("<label>Contraseña temporal <input name='clave' type='password' required autocomplete='new-password'></label>")
                .Append("<label>Perfil <select name='perfil'><option>").Append(InventoryPolicies.Viewer)
                .Append("</option><option>").Append(InventoryPolicies.Operator)
                .Append("</option><option>").Append(InventoryPolicies.Administrator)
                .Append("</option></select></label>")
                .Append("<button type='submit'>Crear cuenta</button></form><h2>Cuentas</h2><ul>");
            foreach (var account in accounts)
            {
                html.Append("<li>").Append(HtmlEncoder.Default.Encode(account.UserName ?? ""))
                    .Append(account.IsActive ? " · Activa " : " · Desactivada ");
                if (account.IsActive && account.Id.ToString() != users.GetUserId(context.User))
                    html.Append("<form method='post' action='/admin/usuarios/").Append(account.Id).Append("/desactivar'>")
                        .Append("<input type='hidden' name='__RequestVerificationToken' value='").Append(token)
                        .Append("'><button type='submit'>Desactivar</button></form>");
                html.Append("</li>");
            }
            html.Append("</ul><a href='/inventario'>Volver al inventario</a></main></body></html>");
            return Results.Content(html.ToString(), "text/html; charset=utf-8");
        }).RequireAuthorization(InventoryPolicies.ManageUsers);

        app.MapPost("/admin/usuarios", async (HttpContext context, IAntiforgery antiforgery,
            UserManager<AccessUser> users, RoleManager<IdentityRole<Guid>> roles, AccessDbContext db) =>
        {
            if (!await ValidateAsync(context, antiforgery)) return Results.BadRequest();
            var form = await context.Request.ReadFormAsync();
            var name = form["usuario"].ToString().Trim();
            var email = form["correo"].ToString().Trim();
            var password = form["clave"].ToString();
            var role = form["perfil"].ToString();
            if (name.Length is < 1 or > 256 || email.Length is < 1 or > 256 || password.Length > 1024 ||
                role is not (InventoryPolicies.Administrator or InventoryPolicies.Operator or InventoryPolicies.Viewer) ||
                !await roles.RoleExistsAsync(role)) return Results.BadRequest();

            await using var tx = await db.Database.BeginTransactionAsync();
            var account = new AccessUser { UserName = name, Email = email };
            var created = await users.CreateAsync(account, password);
            if (!created.Succeeded) return Results.BadRequest("No se pudo crear la cuenta. Compruebe nombre, correo y política de contraseña.");
            var assigned = await users.AddToRoleAsync(account, role);
            if (!assigned.Succeeded) return Results.BadRequest("No se pudo asignar el perfil.");
            await tx.CommitAsync();
            return Results.Redirect("/admin/usuarios");
        }).RequireAuthorization(InventoryPolicies.ManageUsers);

        app.MapPost("/admin/usuarios/{id:guid}/desactivar", async (Guid id, HttpContext context,
            IAntiforgery antiforgery, UserManager<AccessUser> users) =>
        {
            if (!await ValidateAsync(context, antiforgery)) return Results.BadRequest();
            if (users.GetUserId(context.User) == id.ToString()) return Results.BadRequest("No puede desactivar su propia cuenta.");
            var account = await users.FindByIdAsync(id.ToString());
            if (account is null) return Results.NotFound();
            account.IsActive = false;
            var updated = await users.UpdateAsync(account);
            if (!updated.Succeeded) return Results.BadRequest();
            await users.UpdateSecurityStampAsync(account);
            return Results.Redirect("/admin/usuarios");
        }).RequireAuthorization(InventoryPolicies.ManageUsers);
    }

    private static async Task<bool> ValidateAsync(HttpContext context, IAntiforgery antiforgery)
    {
        try { await antiforgery.ValidateRequestAsync(context); return true; }
        catch (AntiforgeryValidationException) { return false; }
    }
}
