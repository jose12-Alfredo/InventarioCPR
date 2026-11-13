# Data Protection — implementación local y activación pendiente

Estado: implementado y validado contra Neon real **exclusivamente en `test-migracion-inicial`** el 12 de noviembre de 2026. Migración aplicada con autorización; dos claves de prueba cifradas conservadas. **No publicar hasta completar las acciones de activación.** No se cambiaron credenciales reales ni configuración de Render. Resultados y límites en `DATA_PROTECTION_NEON_VALIDATION.md`. Este documento sustituye las instrucciones antiguas de persistir `/var/data/keys` para el próximo despliegue.

## Arquitectura

- Paquete `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.9.
- `ProtectionDbContext` independiente de `InventoryDbContext` y `AccessDbContext`.
- Tabla `security.data_protection_keys`; historial independiente `security."__DataProtectionMigrationsHistory"`.
- XML del llavero cifrado mediante certificado RSA (mínimo 2048 bits), nunca PFX ni contraseña en PostgreSQL. La clave privada permanece en configuración segura externa a Neon.
- `SetApplicationName("InventarioCPR")` se conserva. No cambian esquema/nombre de cookie Identity, políticas, roles, duración ni validación del security stamp.
- Certificados PFX con clave privada, contraseña y carga `EphemeralKeySet`, compatibles con Linux no-root. El certificado activo debe estar vigente. Los anteriores pueden estar vencidos para poder descifrar datos históricos.
- Render requiere expresamente `UseDatabase=true`. Sin certificado/conexión/configuración válidos, el arranque se rechaza. No hay fallback a llavero efímero ni a disco en Render.
- Al arrancar se comprueba que no hay migraciones pendientes, se descifran las claves históricas no revocadas y se ejecuta una prueba protect/unprotect antes de admitir tráfico. El proveedor puede insertar una clave cifrada si necesita generarla; **no crea esquema ni aplica migraciones**.
- Desarrollo sin opt-in conserva su llavero local actual; no requiere secretos nuevos para seguir programando. Con opt-in reutiliza `NeonDevelopmentApp` y sus guardas existentes cuando no hay conexión independiente. Para leer el llavero de la rama de pruebas hay que cargar ambos certificados históricos; su respaldo de pruebas no activa automáticamente esa configuración.

## Configuración segura

Variables para Render (los valores se introducen en el dashboard/gestor seguro, no en Git ni chat):

| Variable | Uso |
| --- | --- |
| `DataProtection__UseDatabase` | `true`, obligatorio en Render |
| `ConnectionStrings__DataProtection` | Opcional, conexión del rol dedicado. Si se omite, reutiliza `ConnectionStrings__Database`. |
| `DataProtection__Certificate__Base64` | PFX codificado en base64, **sigue siendo secreto**, no cifrado por base64. |
| `DataProtection__Certificate__Password` | Contraseña del PFX, independiente de PostgreSQL. |
| `DataProtection__Certificate__Path` | Alternativa a Base64: ruta absoluta a un archivo secreto PFX, legible por `APP_UID`. Configurar exactamente una fuente. |
| `DataProtection__PreviousCertificates__0__Base64` o `...__Path` | Certificado privado anterior para lectura durante rotación. Índices 0, 1, etc. |
| `DataProtection__PreviousCertificates__0__Password` | Contraseña correspondiente. |

No reutilizar automáticamente el certificado HTTPS de desarrollo ni los certificados efímeros de tests. El certificado definitivo debe generarse fuera del repositorio, custodiarse junto con su contraseña en almacenamiento seguro y tener copia recuperable. Una CA pública no es necesaria para cifrar el llavero; sí es necesaria una clave privada exclusiva y bien custodiada.

Las conexiones de ejecución fuerzan TLS `VerifyFull`. La conexión independiente limita el pool a cinco conexiones. No se necesita disco persistente en Render Free: claves en PostgreSQL, certificado mediante variables seguras o archivo secreto recreado por Render al arrancar. Se mantienen `PORT`, proxy HTTPS, Identity y ejecución no-root existentes.

## Migración aplicada únicamente en la rama de pruebas

Migración: `20261112184907_PersistProtectionKeys`.

Ubicación: `Infrastructure/Security/DataProtection/Migrations/`.

SQL PostgreSQL idempotente revisado: `PersistProtectionKeys.review.sql`. Crea únicamente el esquema `security`, la tabla de claves y su historial. No contiene operaciones sobre `public`, inventario, fotos o usuarios; no contiene datos ni credenciales.

Regeneración offline desde la raíz del repositorio:

```powershell
dotnet ef migrations script --idempotent --project .\InventarioCPR\InventarioCPR.csproj --context ProtectionDbContext --configuration Debug --output .\InventarioCPR\Infrastructure\Security\DataProtection\Migrations\PersistProtectionKeys.review.sql
dotnet ef migrations has-pending-model-changes --project .\InventarioCPR\InventarioCPR.csproj --context ProtectionDbContext --configuration Debug
```

La factoría de diseño usa **solo un destino ficticio localhost**. Sirve para generar y revisar artefactos sin cargar secretos. No usar `dotnet ef database update` con esa factoría para activar Neon.

Aplicada tras autorización y comprobación remota de `neon.branch_id=br-flat-smoke-b4kdmosq` y `neon.endpoint_id=ep-raspy-dream-b476uukh`. El host `ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech` coincide con `NeonMigrationTest`, `ExpectedHost` y `NeonDevelopmentApp`, y difiere de `NeonDirect`. Se verificaron los permisos efectivos sin emitir GRANT ni cambiar roles. Hay una entrada de migración y dos tablas en `security`. **No repetir la migración en otra rama sin autorización independiente.**

El rol de ejecución solo necesita `USAGE` en `security`, `SELECT, INSERT` en la tabla de claves, `USAGE, SELECT` en su secuencia de identidad y `SELECT` en el historial. El rol de mantenimiento crea esquema/tablas. No conceder `DROP` ni permisos adicionales sobre inventario o Identity para esta función.

## Transición del llavero de disco

La conservación demostrada es entre reinicios **una vez usado el llavero PostgreSQL**. No prometer conservar cookies emitidas por el llavero antiguo si sus claves ya desaparecieron en Render Free.

Antes del primer cambio público, decidir operacionalmente según el estado real:

1. Si `/var/data/keys` aún es recuperable, detener escrituras al llavero y respaldarlo de manera segura **antes de cualquier redeploy**. No descargarlo a Git ni a registros. Preservar IDs, fechas, descriptores y revocaciones al importar mediante una utilidad revisada que cifre los secretos con el certificado definitivo. Esa importación necesita autorización de escritura y validación con una cookie previa; no está automatizada en esta implementación.
2. Si las claves ya no existen, las cookies anteriores no son recuperables: programar un único inicio de sesión nuevo y explicarlo antes de activar. Nunca inventar claves de sustitución como si preservaran la cookie antigua.
3. No volver a la imagen anterior con su repositorio de disco esperando leer cookies nuevas: el rollback debe mantener acceso al mismo llavero PostgreSQL y a todos sus certificados de lectura.

No se alteran usuarios ni sus hashes. Cookies válidas siguen sujetas a expiración, cuenta activa, logout y validación de security stamp. El login actual emite cookies de sesión de navegador: persistir claves no convierte esas cookies en «recordarme» ni garantiza conservarlas si el usuario cierra el navegador.

## Rotación del certificado

1. Generar y custodiar certificado B fuera del proyecto; conservar A y sus respaldos.
2. Preparar todos los lectores con A y B disponibles (A activo, B como certificado adicional de lectura) antes de cambiar al emisor B si hubiera varias instancias.
3. Cambiar B a activo y A a `PreviousCertificates:0`; reiniciar solo en ventana autorizada.
4. Verificar lectura de cookies y claves anteriores y generación de **nuevas** claves cifradas con B. Cambiar el certificado activo no recifra por sí solo las filas antiguas ni fuerza rotación inmediata de la clave simétrica.
5. Conservar A mientras existan claves no revocadas cifradas con él. No retirarlo solo porque pasaron ocho horas: puede haber tokens de otros propósitos. Su retirada requiere recifrado revisado o retirada/revocación autorizada del material antiguo y pruebas de impacto. El arranque rechaza claves históricas ilegibles.

## Verificación y límites

Suite `DataProtectionTests`: proveedor EF real con SQLite desechable y certificados ficticios temporales; hosts ASP.NET Core TestServer independientes, endpoints reales `SecurityEndpoints` y almacén Identity SQLite. No son llamadas reales a Neon ni Render. La inicialización SQLite de las tablas/historial es exclusiva del harness; no ejecuta SQL PostgreSQL.

Se comprueban XML cifrado, protect/unprotect y manipulación, propósito distinto, nuevo host, cookies Identity, login/logout, antiforgery anterior al reinicio, rotación con lectura histórica y rechazo sin certificado antiguo, configuración obligatoria, certificado vencido/sin privada/contraseña incorrecta, archivo secreto y ausencia de creación automática de tablas. El modelo y script se verifican con el proveedor Npgsql sin conexión externa.

El harness acelera a cero el intervalo de validación de security stamp para comprobar también el rechazo de una cookie copiada antes del logout; en la aplicación se conserva el intervalo de cinco minutos.

`DataProtectionPostgresTests` permite ejecutar la migración PostgreSQL real **solo** en una base aleatoria `inventariocpr_dp_test_<GUID>` creada y borrada en loopback. Opt-in: variable local `INVENTARIOCPR_DP_LOCAL_POSTGRES` con conexión administrativa a la base `postgres`. No acepta Neon ni hosts externos; no se solicita ni imprime ese valor. Sin servidor configurado, se omite explícitamente.

Resultados de la fase local inicial (12 de noviembre de 2026): suite completa **54 aprobadas, 0 fallidas, 11 omitidas**; 13 casos nuevos de Data Protection aprobados y un caso PostgreSQL local omitido. No hay Docker ni `pg_ctl`, `initdb` o `psql` disponibles en PATH. Publish Release aislado correcto, incluido `blazor.web.js`; modelo de protección sin cambios pendientes respecto de la migración; auditoría NuGet sin vulnerabilidades conocidas en ambos proyectos.

La fase posterior autorizada sí ejecutó PostgreSQL real de Neon: seis fases de validación en procesos distintos, con proveedor EF/Npgsql y arranque Data Protection reales. Pasaron cifrado, persistencia entre procesos, cookies y antiforgery, rotación A→B y lectura histórica. Identity de Neon se usó solo para lecturas y emisión/validación de cookie sin contraseña real; password-login y logout completo usaron Identity SQLite con el mismo llavero Neon. No equivalen a una prueba de password-login contra Neon ni a un redeploy de Render. Ver evidencia detallada y discrepancia inicial de checksum en `DATA_PROTECTION_NEON_VALIDATION.md`.

Los certificados de pruebas A y B están respaldados fuera de Git en User Secrets (`DataProtectionValidation:Neon:CertificateA:Base64`, `...:CertificateB:Base64`, `...:CertificatePassword`). No son la configuración activa de la aplicación ni certificados definitivos. No borrar estos secretos mientras las filas históricas los necesiten.

## Pendientes antes de activar Neon y Render

1. La migración ya está aplicada en la rama autorizada; antes de activar Render, confirmar que su conexión sigue apuntando a esa misma rama. Si se elige otra rama, detener la activación y obtener autorización para ese destino.
2. Proveer certificado definitivo C y contraseña mediante configuración segura y guardar respaldo externo; habilitar `UseDatabase`. Mantener A y B exclusivamente como certificados históricos de lectura, o autorizar su recifrado posterior. Sin ellos el arranque rechaza las filas existentes.
3. Completar el procedimiento de credenciales de `CREDENTIAL_ROTATION.md` con autorización. No se ha rotado ninguna credencial real.
4. Resolver la transición del llavero anterior según disponibilidad, como se explica arriba.
5. Autorizar publicación y despliegue; validar login, foto privada y antiforgery en Render real, redeploy controlado y misma cookie después; verificar logout y ausencia de escrituras fuera de `security` por Data Protection. Estos resultados no están certificados aún.

Configuración paso a paso: `DATA_PROTECTION_NEON_VALIDATION.md`, sección «Render: pasos previos a publicar».
