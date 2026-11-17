# Preparación final y actualización del servicio Render existente

**Asignaciones/empresas (validación 2026-11-17):** migración `20261116181150_CompaniesAndEquipmentCustodies` aplicada únicamente a Neon `test-migracion-inicial`, tras recuperación lógica ensayada y comprobación de datos conservados. Suite Release 89/0/0; navegador HTTPS local con Neon/Cloudinary reales, 78 checks. No repetir la migración ni asumir autorización para otro destino. Código/migración no actualizan automáticamente la base al arrancar. Alcance y conciliación en `InventarioCPR/ASSIGNMENTS.md`; evidencia en `InventarioCPR/ASSIGNMENTS_NEON_VALIDATION.md`. El módulo sigue sin commit/push/despliegue; no se cambió configuración de Data Protection ni credenciales.

**Estado comprobado 2026-11-17:** código preparado, no autorizado para commit/push/despliegue. HEAD Git publicado registrado: `8703f931f68ac2c883111ed692f6532bd6bc70f9`; empresas/asignaciones siguen en el worktree. Auditoría READ ONLY: las cuatro migraciones de inventario y las de Identity/Data Protection están aplicadas en test-migracion-inicial, sin pendientes ni diferencias de modelo, y los datos anteriores se conservan. GET público actual: healthz204, acceso200, boot Blazor200. El deployment/commit y las variables efectivas de Render requieren comprobación manual; las respuestas públicas no prueban su conexión Neon. Informe principal: `RELEASE_READINESS.md`.

**Histórico: validación de fotografía obligatoria al 2026-11-13.** `20261112200209_EquipmentDraftFinalization` se aplicó exclusivamente a `test-migracion-inicial`, con backfill de 42 registros y tres fixtures/cuatro fotos reales. Aquella validación no desplegó código; la funcionalidad se publicó después en los commits anteriores al HEAD indicado arriba. No repetir la migración ni asumir autorización para otra rama. Evidencia en `InventarioCPR/MANDATORY_PHOTO_REAL_VALIDATION.md`; no es el procedimiento de actualización actual.

Este documento describe la preparación del repositorio privado y el despliegue futuro de InventarioCPR. No autoriza migraciones, creación de cuentas ni cambios en la rama `production` de Neon.

Data Protection usa PostgreSQL y certificado, compatible con Render Free. El certificado definitivo C **ya existe** (RSA3072, hasta 2028-11-13 16:16:56 UTC); A/B históricos también están disponibles localmente y se necesitan para el llavero existente. El incidente de configuración del 13 de noviembre está documentado históricamente en `RENDER_STARTUP_FIX.md`; no asumir que hay que generar otro certificado o volver a importar variables. Comparar primero la configuración efectiva del servicio, sin mostrar valores. No se cambió el servicio remoto.

**Validación Neon real:** migración `20261112184907_PersistProtectionKeys` aplicada únicamente a `test-migracion-inicial`, ID `br-flat-smoke-b4kdmosq`, endpoint `ep-raspy-dream-b476uukh`. Pasaron pruebas entre procesos, cookies/antiforgery y lectura histórica. En la preparación actual se conservaron las dos claves, sin escrituras. C ya está creado; A/B siguen siendo necesarios para descifrar las claves antiguas, aunque C proteja las futuras. No repetir la migración ni cambiar el llavero al desplegar.

## Plan exacto de actualización — todavía NO ejecutar

### Responsabilidades

| Acción | Automatizable | Intervención / autorización pendiente |
|---|---|---|
| Auditoría de rama autorizada, historiales/modelos, datos y manifiesto | Sí; realizada en READ ONLY | Ninguna escritura incluida |
| Build/publish, pruebas seguras, revisión diff y artefactos | Sí; publish y 54 pruebas seguras correctos | Docker/Linux se verifica en el build real |
| Revisión de certificados locales sin imprimirlos | Sí; C/A/B vigentes y con privada | Comparar con Render y confirmar custodia |
| Catálogos/conciliación | UI existente | Usuario confirma hechos; registrar cambios requiere autorización operativa |
| Retiro de fixtures | Puede prepararse/ensayarse | Autorización separada; no forma parte del deploy |
| Rotación Neon/Cloudinary | Validaciones y herramientas posibles tras autorizar | Titular coordina proveedor, rol/consumidores y secretos; no afectar production |
| Auto-deploy, Environment, rollback y deploy Render | No se dispone de sesión/API autorizada del servicio | Usuario en dashboard; no compartir secretos por chat |
| Commit/push y elección de commit de despliegue | Sí, una vez solicitados | Requieren autorización expresa; no realizados |

### A. Preflight del servicio actual (usuario en Render)

1. Abrir **InventarioCPR** existente, no crear otro servicio. Registrar privadamente `srv-…`, URL, repositorio, rama Git, runtime Docker, deployment/commit Live, Auto-Deploy y número de instancias. Confirmar Dockerfile/contexto en la raíz del repositorio. Mantener una instancia.
2. Verificar **sin copiar secretos al chat** el host/base de `ConnectionStrings__Database`. Para este piloto debe corresponder a `ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech`, `neondb`, rama `br-flat-smoke-b4kdmosq`. Confirmar la asociación en Neon y mediante SELECT de `neon.branch_id`/`neon.endpoint_id` con la conexión efectiva. Si difiere, detener este plan; **no aplicar migraciones ni modificar production**.
3. Revisar `ConnectionStrings__DataProtection`: si existe, comprobar separadamente su destino e historial; si está ausente, usa Database. No asumir que ambos apuntan al mismo lugar. El llavero verificado es el de la rama indicada.
4. Guardar una copia privada de la configuración vigente y el deployment/commit de retorno. Comprobar acceso a recuperación antes de cambiar secretos. El respaldo histórico de 33 tablas no reemplaza uno actualizado de las 36 y sus fotos antes de operaciones destructivas.
5. Antes de un futuro push autorizado, desactivar Auto-Deploy **con autorización**, para que publicar código no arranque la actualización sin preparar configuración. No se cambió en esta tarea.

### B. Cerrar seguridad y revisar variables

6. Resolver la credencial Neon expuesta conforme a `InventarioCPR/CREDENTIAL_ROTATION.md`: identificar rol, ramas/consumidores y posibles usos de production antes de autorizar una rotación. Actualizar los consumidores aprobados en una ventana coordinada y probar conexiones nuevas. No restaurar una credencial comprometida como mecanismo de rollback.
7. Confirmar si las claves Cloudinary requieren rotación por exposición; si se rotan, mantener cuenta, recursos, public_id y delivery authenticated. La contraseña Identity publicada antiguamente ya se reemplazó: confirmar custodia de la vigente, no resetear por rutina ni cambiar roles.
8. Contrastar **todas las variables de la tabla siguiente**, incluidos environment groups. No importar a ciegas un `.env` ni mostrar su contenido. C y A/B ya existen; reutilizarlos. El archivo privado preparado anteriormente contiene solo las siete variables DP, no Database/Cloudinary ni rotaciones pendientes. Confirmar que sus valores corresponden a los certificados vigentes antes de importarlo si falta algo.
9. No añadir marcadores `NeonDevelopmentApp__BranchConfirmed`, opt-ins de tests, bootstrap/reset ni User Secrets a Render. Para una comprobación inicialmente sin escritura, se puede acordar `Inventory__ProductionWritesEnabled=false` sin el marcador de Development; **no bloquea por sí solo todos los posibles escritores/acciones administrativas**. Una ventana sin escrituras exige coordinar y detener todos los procesos pertinentes.

### C. Publicación de código y arranque (solo después de autorización)

10. Revisar `git status`, diff, archivos nuevos y secretos; incluir solo el módulo/documentación previstos. Excluir respaldos, CSV de revisión, capturas, PFX, `.env` y el directorio ajeno `design_referencias/`. No usar `git add .` sin revisar el alcance. Autorizar commit/push por separado y registrar el SHA final.
11. Esta rama **ya tiene las seis migraciones de los tres contextos**. No ejecutar `database update`, Down ni SQL de migración para este despliegue. Si el destino se cambia, se necesita otra auditoría/autorización; este plan no lo cubre.
12. En Render, guardar únicamente los cambios de configuración aprobados y elegir **Manual Deploy / Deploy a specific commit** (o la acción equivalente de la interfaz) con el SHA revisado. Si guardar Environment inicia un rebuild/deploy, hacerlo solo en este punto autorizado. No instalar un comando de migración pre-deploy/startup.
13. Esperar build y estado **Live**. Docker exige un `blazor.web.js` no vacío; verificar también carga de Skia Linux. Ante error de DP, revisar flags/conexión/C+A/B; no quitar el fail-closed, borrar claves o generar una cuenta alternativa para conseguir que arranque.

### D. Comprobaciones posteriores (usuario + automatización autorizada)

14. GET `/healthz`204, `/acceso`200, `/_framework/blazor.web.js`200 y módulos/CSS del SHA desplegado. El health check no verifica dependencias: no basta por sí solo.
15. Login habitual del usuario sin divulgar la contraseña: Inicio, Inventario y `/asignaciones` interactivos; circuito SignalR **wss/101**, filtros, expediente amplio, foto privada y visor sin recorte; logout y rechazo de anónimos. Verificar permisos reales con cuentas autorizadas, sin modificar roles para la prueba.
16. Repetir SELECT de rama/endpoint y revisar los tres historiales con **la configuración efectiva del servicio**; ninguna pendiente. Comprobar conservación y conteos respecto del baseline de la ventana (hoy 55 equipos/21 fotos, o 46/13 solo si el retiro se autorizó y completó).
17. Comprobar empresa propietaria/usuaria independientes, selección de áreas/trabajadores e historial, Total=Libres+Ocupados, borradores excluidos y solo dos disponibilidades. Probar guardado/subida/asignación/devolución **solo sobre un fixture y con autorización**; no manipular un activo empresarial para un smoke test. No repetir las pruebas persistentes completas por defecto.
18. Reinicio/redeploy controlado autorizado: conserva datos, fotos y lectura de cookies/claves; una sesión expirada o invalidada por rotación no equivale a pérdida de datos. DP puede generar una nueva clave cifrada con C cuando corresponda; no comparar siempre con «exactamente dos claves» después de un arranque legítimo. Mantener A/B para lectura histórica.
19. Validar desde un teléfono físico cámara JPEG/PNG, carga, teclado y reconexión. La emulación Chromium previa no certifica cámara física ni Safari. Revisar logs sin secretos ni URLs firmadas y registrar deployment/fecha/resultados.

### E. Reversión si falla

20. Detener nuevas escrituras y documentar el error. Si aún no se guardó ningún cambio, un build fallido suele dejar Live la versión previa: comprobar deployment y no repetir cambios de datos.
21. Desde el dashboard elegir **Rollback** al deployment Live capturado, o desplegar su commit exacto si la plataforma lo permite. El último SHA publicado conocido es `8703f93`, pero el objetivo de retorno debe ser el deployment efectivo confirmado por el usuario, no una suposición del repositorio local.
22. Conservar el esquema aditivo y todos los datos/custodias/auditoría. **No hacer Down** de CompaniesAndEquipmentCustodies (su guarda bloquea si hay historia), no restaurar un snapshot antiguo sobre escrituras nuevas y no eliminar columnas/tablas para recuperar un binario.
23. Mantener certificados C+A/B, mismo ApplicationName y llavero; conservar las credenciales seguras vigentes. Rollback de imagen no revierte necesariamente variables ni contraseñas del proveedor: compararlas con la configuración privada capturada, sin reactivar secretos expuestos.
24. El binario anterior no ofrece el módulo nuevo: mantener escrituras suspendidas hasta comprobar su compatibilidad con el esquema/historia y resolver hacia adelante. No prometer rollback funcional completo de asignaciones. Si falla la compatibilidad, mantener ventana de mantenimiento y corregir el arranque/configuración con los datos intactos; ensayar recuperación en una copia autorizada antes de una restauración real.

## Estado de preparación

- La raíz del repositorio es la carpeta que contiene `InventarioCPR.slnx`, `Dockerfile`, `.gitignore` y `.dockerignore`.
- Desarrollo conserva HTTPS en `https://localhost:7228` y User Secrets locales.
- Production detecta Render mediante su variable nativa `RENDER=true`; usa el `PORT` asignado por la plataforma, HTTP interno y HTTPS externo terminado por el proxy.
- La aplicación no ejecuta `EnsureCreated`, `Migrate` ni migraciones automáticamente.
- Las migraciones EF se conservan en el repositorio porque son necesarias para preparar una base autorizada.
- Para el piloto autorizado no faltan migraciones en test-migracion-inicial; faltan confirmación de configuración/seguridad y autorización de publicación. Un destino empresarial distinto necesitará preparación separada, sin tocar production en esta tarea.
- Existe un servicio público en `https://inventariocpr.onrender.com`; la revisión vigente está en este runbook y `RELEASE_READINESS.md`. `RENDER_STAGE_A_READINESS.md` es evidencia histórica y su diseño por disco fue sustituido. No deducir el destino a partir del entorno ASP.NET Core ni de la rama local.

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

## Repositorio existente y revisión antes de un commit autorizado

El repositorio ya existe en `https://github.com/jose12-Alfredo/InventarioCPR.git`; no volver a inicializarlo ni cambiar el remoto. Revisar siempre:

```powershell
git status --short --ignored
git diff --check
git ls-files --others --exclude-standard
```

El stage debe limitarse a archivos revisados. Commit/push requieren instrucción expresa del usuario; ningún comando de este documento es autorización para ejecutarlos.

No colocar tokens en la URL remota. Autenticarse con Git Credential Manager, GitHub CLI o SSH mediante una clave personal protegida y nunca almacenada en este repositorio.

## Variables exactas que contrastar en el servicio actual

Conservar el **Web Service** existente y runtime Docker con raíz del repositorio como contexto. Render construye el `Dockerfile`; no se necesita comando manual de inicio ni migración.

Contrastar las siguientes variables; aplicar diferencias solamente en la actualización autorizada:

| Variable | Finalidad | Secreta |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT=Production` | Entorno ASP.NET Core; también está definido en la imagen | No |
| `Deployment__Provider=Render` | Override opcional fuera de Render; la plataforma ya proporciona `RENDER=true` | No |
| `AllowedHosts=inventariocpr.onrender.com` | Host del servicio actual; puede autodetectarse desde RENDER_EXTERNAL_HOSTNAME. Añadir dominios propios autorizados separados con `;` | No |
| `ConnectionStrings__Database` | Conexión Npgsql al destino confirmado; para este piloto, únicamente test-migracion-inicial; runtime impone VerifyFull | Sí |
| `Security__EnableIdentity=true` | Mantiene Identity obligatoria | No |
| `Inventory__ProductionWritesEnabled=true` | Habilita altas/ediciones solo después de aprobar esquema y operación | No |
| `Inventory__PhotosEnabled=true` | Habilita fotografías solo con EquipmentPhotoHistory aplicada en el destino aprobado | No |
| `DataProtection__UseDatabase=true` | Exige el llavero PostgreSQL y su migración independiente aprobada | No |
| `DataProtection__Certificate__Base64` o `DataProtection__Certificate__Path` | PFX definitivo **C** en configuración segura o archivo secreto; exactamente una fuente | Sí |
| `DataProtection__Certificate__Password` | Contraseña del PFX que cifra el llavero; no es un certificado Kestrel | Sí |
| `DataProtection__PreviousCertificates__0__Base64` | Certificado histórico A con privada; fuente Base64, no combinar con Path para esa entrada | Sí |
| `DataProtection__PreviousCertificates__0__Password` | Contraseña histórica A | Sí |
| `DataProtection__PreviousCertificates__1__Base64` | Certificado histórico B con privada; fuente Base64, no combinar con Path para esa entrada | Sí |
| `DataProtection__PreviousCertificates__1__Password` | Contraseña histórica B | Sí |
| `ConnectionStrings__DataProtection` | Opcional: rol dedicado; si se omite se reutiliza Database | Sí |
| `Cloudinary__CloudName` | Nombre de cuenta Cloudinary | Según política |
| `Cloudinary__ApiKey` | Clave API Cloudinary | Sí |
| `Cloudinary__ApiSecret` | Secreto API Cloudinary | Sí |
| `Cloudinary__TestAssetFolder` | Solo piloto ficticio con carpeta acordada; ausente para operación empresarial habitual salvo decisión explícita | No |
| `Inventory__AllowAnonymousLocalTests` | Ausente o false; nunca true en Render | No |

Para esta actualización se recomienda la fuente Base64 ya custodiada para C. `DataProtection__UseDatabase` y los seis campos C/A/B anteriores son las **siete variables** del archivo privado DP previo; las otras variables se revisan aparte. C no recifra automáticamente las claves A/B, por lo que sus certificados siguen siendo necesarios. No se necesita contraseña del usuario Identity en variables de entorno.

No configurar `Security__Bootstrap__*`, `Security__PasswordReset__*`, `NeonDevelopmentApp__BranchConfirmed`, opt-ins de pruebas ni secretos locales de desarrollo. `DataProtection__KeysPath` pertenece al diseño anterior de disco y **no sustituye** el proveedor PostgreSQL/certificados de la versión actual.

No es necesario configurar `PORT`: Render usa `10000` de forma predeterminada y puede proporcionar otro valor. No configurar un certificado Kestrel: Render termina TLS y reenvía el esquema HTTPS. El certificado Data Protection y su contraseña sí son necesarios para cifrar el llavero en PostgreSQL. La aplicación reconoce `RENDER=true`, confía los encabezados reenviados únicamente en ese modo y escucha HTTP dentro del contenedor. `RENDER_EXTERNAL_HOSTNAME` restringe automáticamente el host nativo; configurar `AllowedHosts` manualmente al añadir un dominio personalizado.

La cadena `ConnectionStrings__Database` se valida con TLS `VerifyFull`. Guardarla solo en el panel de variables de Render y usar una credencial Neon con privilegios mínimos. No reutilizar User Secrets ni pegar la cadena en archivos, commits, incidencias o logs.

## Disco, sesiones y WebSockets

- Render Free no requiere disco persistente para esta implementación: las claves se almacenan cifradas en PostgreSQL y el certificado se suministra externamente.
- Conservar el llavero PostgreSQL actual y los certificados que lo descifran. El incidente de transición desde disco es histórico; no repetir una migración de claves ni borrar el llavero actual durante esta actualización. Ver `InventarioCPR/DATA_PROTECTION.md`.
- Empezar con una sola instancia. El llavero PostgreSQL es compartible; varias instancias también requieren certificados compartidos y una estrategia compatible con los circuitos de Blazor Server.
- Render admite WebSockets; comprobar después del despliegue que el circuito Blazor usa `wss`, reconecta y conserva autorización.
- Configurar el health check de Render en `/healthz`. Devuelve solo estado HTTP y no consulta Neon ni Cloudinary.

## Un eventual destino production — fuera del alcance de este plan

El contenedor no aplica migraciones al iniciar. Esta preparación no conecta ni modifica esa rama. Una futura activación empresarial en otro destino necesitará autorización separada para:

1. Confirmar el endpoint exacto de la rama `production` de Neon.
2. Revisar nuevamente el SQL de inventario, Identity y Data Protection.
3. Diseñar un comando de migración Production que no reutilice los factories limitados a `test-migracion-inicial`.
4. Aplicar únicamente las migraciones pendientes autorizadas y verificar los tres historiales EF.
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

## Configuración inicial de otro servicio — fuera del alcance actual

1. Autorizar la aplicación de Render en GitHub solo para el repositorio privado de InventarioCPR.
2. Crear el Web Service Docker y seleccionar la rama protegida que se vaya a desplegar.
3. Auditar las migraciones del destino antes de cualquier primera activación; la de Data Protection ya está aplicada en la rama verificada y no se repite.
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
