# Preparación de GitHub y Render

**Registro con fotografía obligatoria (validación real al 2026-11-13):** `20261112200209_EquipmentDraftFinalization` ya está aplicada exclusivamente en la rama autorizada `test-migracion-inicial` (`br-flat-smoke-b4kdmosq`). Backfill de 42 equipos anteriores y flujo real con tres fixtures/cuatro fotos Cloudinary verificados; no repetir esa migración ni asumir autorización para otra rama. Evidencia en `InventarioCPR/MANDATORY_PHOTO_REAL_VALIDATION.md`. No hubo commit, push o despliegue; el servicio Render anterior no incorpora automáticamente el nuevo flujo por haber migrado Neon. Data Protection validado permanece sin modificaciones.

Este documento describe la preparación del repositorio privado y el despliegue futuro de InventarioCPR. No autoriza migraciones, creación de cuentas ni cambios en la rama `production` de Neon.

La implementación local de Data Protection ahora usa PostgreSQL y certificado, compatible con Render Free. Su activación está pendiente: seguir `InventarioCPR/DATA_PROTECTION.md` y `InventarioCPR/CREDENTIAL_ROTATION.md` antes de publicar esta versión. No se ha cambiado el servicio remoto.

**Validación Neon real (2026-11-12):** migración `20261112184907_PersistProtectionKeys` aplicada únicamente a `test-migracion-inicial`, ID remoto `br-flat-smoke-b4kdmosq`, endpoint remoto `ep-raspy-dream-b476uukh`. Pasaron las pruebas de llavero entre procesos, cookies/antiforgery y rotación; no se hizo commit, push ni despliegue. Instrucciones concretas y límites en `InventarioCPR/DATA_PROTECTION_NEON_VALIDATION.md`. El certificado definitivo de Render sigue pendiente; al reutilizar ese llavero también se necesitan A y B como certificados históricos de lectura. No volver a aplicar la migración en esa rama ni asumir autorización para otra.

## Estado de preparación

- La raíz del repositorio es la carpeta que contiene `InventarioCPR.slnx`, `Dockerfile`, `.gitignore` y `.dockerignore`.
- Desarrollo conserva HTTPS en `https://localhost:7228` y User Secrets locales.
- Production detecta Render mediante su variable nativa `RENDER=true`; usa el `PORT` asignado por la plataforma, HTTP interno y HTTPS externo terminado por el proxy.
- La aplicación no ejecuta `EnsureCreated`, `Migrate` ni migraciones automáticamente.
- Las migraciones EF se conservan en el repositorio porque son necesarias para preparar una base autorizada.
- El despliegue real sigue bloqueado hasta aprobar y ejecutar un procedimiento separado para el esquema, roles y administrador de Production.
- Existe un servicio público en `https://inventariocpr.onrender.com`; la revisión actual de su rama Git, destino Neon y disco está en `RENDER_STAGE_A_READINESS.md`. No deducir el destino a partir del nombre del entorno ASP.NET Core ni de la rama local.

## Archivos que deben versionarse

- Código `*.cs`, `*.razor`, CSS y JavaScript de la aplicación.
- `InventarioCPR.csproj`, `InventarioCPR.Tests.csproj` e `InventarioCPR.slnx`.
- Migraciones y snapshots de Entity Framework Core.
- `appsettings.json` y `appsettings.Development.json`, que no contienen credenciales.
- `Properties/launchSettings.json`, solo para desarrollo local.
- `Dockerfile`, `.dockerignore`, `.gitignore` y documentación Markdown.
- Recursos estáticos necesarios dentro de `wwwroot`.

## Archivos que nunca deben versionarse

- `bin`, `obj`, `.vs`, `.idea`, resultados de pruebas, cobertura, logs y capturas temporales.
- `*.user`, `*.suo`, `*.DotSettings.user` y estado personal de editores/agentes.
- `secrets.json`, `.env`, `.env.*` y configuraciones locales con credenciales.
- Certificados o claves privadas: `*.pfx`, `*.p12`, `*.pem`, `*.key`, `*.ppk`, `*.jks` y `*.keystore`.
- Exportaciones de bases, copias de seguridad y bases locales.

User Secrets vive fuera del repositorio, bajo el perfil de Windows asociado a `UserSecretsId=InventarioCPR-local`. No copiar ese archivo a la solución ni a Render.

## Crear el repositorio privado en GitHub

1. En GitHub, seleccionar **New repository**.
2. Usar un nombre como `InventarioCPR` y marcar **Private**.
3. No inicializarlo con README, `.gitignore` ni licencia; estos archivos se administran localmente.
4. No agregar colaboradores hasta definir el acceso necesario.
5. Activar protección de la rama `main`, revisión de dependencias y Dependabot cuando el repositorio ya exista.
6. Considerar secret scanning y push protection si el plan de la organización los ofrece para repositorios privados.

Antes del primer commit, revisar siempre:

```powershell
git status --short --ignored
git diff --check
git ls-files --others --exclude-standard
```

El primer commit y el remoto se crean únicamente después de revisar la lista:

```powershell
git add .
git status --short
git commit -m "Prepare InventarioCPR for private hosting"
git remote add origin https://github.com/<organizacion-o-usuario>/InventarioCPR.git
git push -u origin main
```

No colocar tokens en la URL remota. Autenticarse con Git Credential Manager, GitHub CLI o SSH mediante una clave personal protegida y nunca almacenada en este repositorio.

## Variables futuras de Render

Crear un **Web Service** privado desde el repositorio y seleccionar **Docker** con la raíz del repositorio como contexto. Render debe construir el `Dockerfile`; no se necesita un comando manual de inicio.

Configurar como secretos o variables del servicio:

| Variable | Finalidad | Secreta |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT=Production` | Entorno ASP.NET Core; también está definido en la imagen | No |
| `Deployment__Provider=Render` | Override opcional fuera de Render; la plataforma ya proporciona `RENDER=true` | No |
| `AllowedHosts=<servicio>.onrender.com` | Opcional para el dominio nativo; obligatorio al agregar dominios personalizados, separados con `;` | No |
| `ConnectionStrings__Database` | Conexión Npgsql de la base Production autorizada, con usuario y contraseña | Sí |
| `Security__EnableIdentity=true` | Mantiene Identity obligatoria | No |
| `Inventory__ProductionWritesEnabled=true` | Habilita altas/ediciones solo después de aprobar esquema y operación | No |
| `Inventory__PhotosEnabled=true` | Habilita fotografías solo con EquipmentPhotoHistory aplicada en el destino aprobado | No |
| `DataProtection__UseDatabase=true` | Exige el llavero PostgreSQL y su migración independiente aprobada | No |
| `DataProtection__Certificate__Base64` o `DataProtection__Certificate__Path` | PFX definitivo en configuración segura o archivo secreto; exactamente una fuente | Sí |
| `DataProtection__Certificate__Password` | Contraseña del PFX que cifra el llavero; no es un certificado Kestrel | Sí |
| `ConnectionStrings__DataProtection` | Opcional: rol dedicado; si se omite se reutiliza Database | Sí |
| `Cloudinary__CloudName` | Nombre de cuenta Cloudinary | Según política |
| `Cloudinary__ApiKey` | Clave API Cloudinary | Sí |
| `Cloudinary__ApiSecret` | Secreto API Cloudinary | Sí |
| `Cloudinary__TestAssetFolder` | Carpeta exclusiva para la prueba física con imágenes ficticias; no mezclarla con fotografías empresariales | No |

No es necesario configurar `PORT`: Render usa `10000` de forma predeterminada y puede proporcionar otro valor. No configurar un certificado Kestrel: Render termina TLS y reenvía el esquema HTTPS. El certificado Data Protection y su contraseña sí son necesarios para cifrar el llavero en PostgreSQL. La aplicación reconoce `RENDER=true`, confía los encabezados reenviados únicamente en ese modo y escucha HTTP dentro del contenedor. `RENDER_EXTERNAL_HOSTNAME` restringe automáticamente el host nativo; configurar `AllowedHosts` manualmente al añadir un dominio personalizado.

La cadena `ConnectionStrings__Database` se valida con TLS `VerifyFull`. Guardarla solo en el panel de variables de Render y usar una credencial Neon con privilegios mínimos. No reutilizar User Secrets ni pegar la cadena en archivos, commits, incidencias o logs.

## Disco, sesiones y WebSockets

- Render Free no requiere disco persistente para esta implementación: las claves se almacenan cifradas en PostgreSQL y el certificado se suministra externamente.
- Revisar la transición del llavero antiguo antes de cualquier redeploy; sin recuperar sus claves no se pueden preservar cookies emitidas previamente. Ver `InventarioCPR/DATA_PROTECTION.md`.
- Empezar con una sola instancia. El llavero PostgreSQL es compartible; varias instancias también requieren certificados compartidos y una estrategia compatible con los circuitos de Blazor Server.
- Render admite WebSockets; comprobar después del despliegue que el circuito Blazor usa `wss`, reconecta y conserva autorización.
- Configurar el health check de Render en `/healthz`. Devuelve solo estado HTTP y no consulta Neon ni Cloudinary.

## Base de datos Production

El contenedor no aplica migraciones al iniciar. Antes del primer despliegue funcional se necesita una autorización separada para:

1. Confirmar el endpoint exacto de la rama `production` de Neon.
2. Revisar nuevamente el SQL de las migraciones de inventario e Identity.
3. Diseñar un comando de migración Production que no reutilice los factories limitados a `test-migracion-inicial`.
4. Aplicar migraciones manualmente y verificar ambos historiales EF.
5. Crear el primer administrador por un procedimiento de un solo uso revisado y retirar sus secretos.
6. Habilitar `Inventory__ProductionWritesEnabled=true` solo cuando el esquema y la política empresarial estén aprobados.

No apuntar Render a `test-migracion-inicial` como sustituto accidental de Production.

Para una prueba física aislada, confirmar expresamente el destino del servicio existente y su esquema antes de publicar. El runtime debe seguir en Production por seguridad, aunque la base elegida para un piloto autorizado sea una rama de pruebas. Esta revisión no cambia la conexión de Render, no autoriza migraciones en production de Neon y no convierte la validación local en una validación del esquema remoto.

## Compilación y pruebas

Validación local segura:

```powershell
dotnet restore InventarioCPR.slnx
dotnet build InventarioCPR.slnx --configuration Release --no-restore
dotnet test InventarioCPR.slnx --configuration Release --no-build --no-restore
dotnet publish InventarioCPR/InventarioCPR.csproj --configuration Release --no-restore
```

Las pruebas PostgreSQL usan `DatabaseFactAttribute` y se omiten por defecto. Solo la rama aislada confirmada puede habilitarlas con `INVENTARIOCPR_RUN_DATABASE_TESTS=test-migracion-inicial`. La prueba que conserva registros ficticios requiere además el opt-in separado `INVENTARIOCPR_RUN_PERSISTENT_DATABASE_TESTS=test-migracion-inicial`. No configurar esas variables en CI general ni contra Production.

Si Docker está instalado, validar desde la raíz:

```powershell
docker build --tag inventariocpr:local .
```

No ejecutar la imagen con credenciales Production durante una comprobación local.

## Conectar GitHub con Render posteriormente

1. Autorizar la aplicación de Render en GitHub solo para el repositorio privado de InventarioCPR.
2. Crear el Web Service Docker y seleccionar la rama protegida que se vaya a desplegar.
3. Aplicar la migración independiente de Data Protection solo con autorización y añadir certificado/variables anteriores antes del primer arranque de esta versión.
4. Desactivar auto-deploy hasta completar la preparación de Production y una revisión del cambio.
5. Tras autorizar el despliegue, comprobar `/healthz`, login, logout, permisos por rol, WebSockets, reinicio y persistencia de sesión.
6. Revisar logs sin habilitar datos sensibles, cadenas de conexión ni logging detallado de EF.

## Actualizaciones futuras

1. Crear una rama de trabajo desde `main`.
2. Ejecutar restore, build Release, pruebas seguras y auditoría de secretos.
3. Revisar migraciones generadas; nunca aplicarlas automáticamente por el push.
4. Abrir un pull request privado y revisar los cambios.
5. Fusionar solo después de aprobar código, configuración y cambios de base.
6. Desplegar desde Render de forma manual hasta establecer una política empresarial de CI/CD y rollback.
