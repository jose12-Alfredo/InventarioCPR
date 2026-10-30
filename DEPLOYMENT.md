# Preparación de GitHub y Render

Este documento describe la preparación del repositorio privado y el despliegue futuro de InventarioCPR. No autoriza migraciones, creación de cuentas ni cambios en la rama `production` de Neon.

## Estado de preparación

- La raíz del repositorio es la carpeta que contiene `InventarioCPR.slnx`, `Dockerfile`, `.gitignore` y `.dockerignore`.
- Desarrollo conserva HTTPS en `https://localhost:7228` y User Secrets locales.
- Production solo arranca cuando `Deployment__Provider=Render`; usa el `PORT` asignado por Render, HTTP interno y HTTPS externo terminado por el proxy.
- La aplicación no ejecuta `EnsureCreated`, `Migrate` ni migraciones automáticamente.
- Las migraciones EF se conservan en el repositorio porque son necesarias para preparar una base autorizada.
- El despliegue real sigue bloqueado hasta aprobar y ejecutar un procedimiento separado para el esquema, roles y administrador de Production.

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
| `Deployment__Provider=Render` | Habilita exclusivamente la configuración revisada del proxy | No |
| `AllowedHosts=<servicio>.onrender.com` | Restringe el host público; separar dominios adicionales con `;` | No |
| `ConnectionStrings__Database` | Conexión Npgsql de la base Production autorizada, con usuario y contraseña | Sí |
| `Security__EnableIdentity=true` | Mantiene Identity obligatoria | No |
| `Inventory__ProductionWritesEnabled=true` | Habilita altas/ediciones solo después de aprobar esquema y operación | No |
| `DataProtection__KeysPath=/var/data/keys` | Conserva cookies y antiforgery entre reinicios | No |
| `Cloudinary__CloudName` | Nombre de cuenta Cloudinary | Según política |
| `Cloudinary__ApiKey` | Clave API Cloudinary | Sí |
| `Cloudinary__ApiSecret` | Secreto API Cloudinary | Sí |

No configurar `PORT`: Render lo proporciona. No configurar un certificado Kestrel ni una contraseña PFX: Render termina TLS y reenvía el esquema HTTPS. La aplicación confía los encabezados reenviados únicamente en el modo Render explícito y escucha HTTP dentro del contenedor.

La cadena `ConnectionStrings__Database` se valida con TLS `VerifyFull`. Guardarla solo en el panel de variables de Render y usar una credencial Neon con privilegios mínimos. No reutilizar User Secrets ni pegar la cadena en archivos, commits, incidencias o logs.

## Disco, sesiones y WebSockets

- Adjuntar un **Persistent Disk** de Render montado en `/var/data` antes de iniciar Production.
- Mantener `DataProtection__KeysPath=/var/data/keys` para que cookies Identity y tokens antiforgery sobrevivan reinicios.
- Empezar con una sola instancia. Varias instancias requerirían claves compartidas y una estrategia compatible con los circuitos de Blazor Server.
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
3. Añadir el disco persistente y las variables anteriores antes del primer arranque.
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
