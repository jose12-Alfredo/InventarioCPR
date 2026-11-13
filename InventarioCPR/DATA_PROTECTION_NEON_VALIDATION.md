# Data Protection — validación real en Neon

Fecha: 12 de noviembre de 2026. Alcance autorizado: migración `20261112184907_PersistProtectionKeys` y pruebas del llavero únicamente en `test-migracion-inicial`. Sin commit, push, despliegue ni rotación de credenciales reales.

## 1. Destino verificado antes de escribir

- Rama autorizada: `test-migracion-inicial`.
- ID devuelto **por PostgreSQL** con `current_setting('neon.branch_id', true)`: `br-flat-smoke-b4kdmosq`.
- Endpoint devuelto con `current_setting('neon.endpoint_id', true)`: `ep-raspy-dream-b476uukh`.
- Host utilizado: `ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech`; base `neondb`.
- Coincide con `NeonMigrationTest`, su `ExpectedHost` y `NeonDevelopmentApp`; marcador local `BranchConfirmed=test-migracion-inicial`. Difiere del host de `NeonDirect`, al que no se abrió conexión.
- Npgsql con TLS `VerifyFull`; valores de credenciales omitidos. Las guardas remotas se repitieron en cada proceso antes de cualquier operación.
- Antes de migrar no existían el esquema `security`, su tabla de claves ni su historial. El rol tenía permiso `CREATE` en la base.

## 2. Migración y permisos

Se ejecutó el SQL idempotente revisado `Infrastructure/Security/DataProtection/Migrations/PersistProtectionKeys.review.sql`. SHA-256 del archivo ejecutado: `073DA3AEDD0528C899A980353AB66A25B3567BA8C310C4AE3B229949F9AA45D2`.

Solo contiene DDL/INSERT de `security`, `security.data_protection_keys` y `security."__DataProtectionMigrationsHistory"`. No contiene operaciones sobre `public`, inventario, fotografías ni Identity. No se cambiaron roles ni permisos; se comprobaron los permisos efectivos `USAGE` en esquema, `SELECT/INSERT` en claves y `USAGE/SELECT` en `security."data_protection_keys_Id_seq"`. El rol existente pudo leer el historial y el llavero mediante EF.

Postflight: **una entrada** de esa migración, **dos tablas** en `security`, inicialmente cero claves. El script idempotente se reejecutó una vez al revisar la alerta de checksum descrita abajo; el historial sigue teniendo una sola entrada. No hubo nuevas migraciones ni operaciones de rollback/borrado.

### Control de tablas existentes y límite observado

Se tomaron conteos y checksums de las 31 tablas de `public`, sin imprimir filas ni hashes de usuarios. La comparación inicial previa/posterior a la migración coincidió en 30 tablas y difirió en `access_users`. El SQL ejecutado no escribe esa tabla; **no se determinó la causa de esa diferencia y no se afirma invariancia completa durante ese intervalo**. No se restauró ni modificó la cuenta para ocultar la discrepancia.

Se conservó el control inicial y se tomó un nuevo baseline después de migrar. La auditoría final de todas las pruebas posteriores coincidió en las **31/31 tablas**, incluida `access_users`; sigue existiendo una cuenta Identity. Los únicos INSERT remotos del harness fueron las dos claves cifradas de `security`. Las escrituras del password-login/logout ficticios fueron en SQLite local.

## 3. Pruebas realizadas y resultados

Harness temporal fuera del repositorio: `C:\Users\jose1\AppData\Local\Temp\opencode\dp-neon-validation`. Usa el código actual de `AddInventoryDataProtection`, el startup check real, EF/Npgsql y ASP.NET Core TestServer. Cada fase se ejecutó como **un proceso distinto**, no solo reconstruyendo un ServiceProvider.

| Fase | PID | Resultado comprobado |
| --- | --- | --- |
| `phase1` | 27688 | Creación en Neon de clave cifrada con certificado A; protect/unprotect; password-login ficticio con Identity SQLite y llavero Neon; cookie de la cuenta existente Neon emitida mediante SignInManager sin verificar contraseña ni escribir usuarios; antiforgery inválido rechazado. |
| `phase2` | 12488 | Lectura del mismo llavero y descifrado tras terminar el proceso anterior; misma cookie válida para Identity local y para cuenta Neon; formulario antiforgery anónimo anterior al reinicio aceptado; logout completo local y rechazo anónimo posterior. |
| `rotate` | 3388 | B activo y A histórico; nueva clave creada con IKeyManager, XML cifrado con certificado B verificado; payload y cookie histórica de Neon siguen legibles; ninguna clave borrada. |
| `historical` | 14076 | Otro proceso recupera payloads anteriores y posteriores a la rotación; arranque sin A rechazado al descifrar la clave histórica; cuatro configuraciones inseguras rechazadas (desactivado, certificado ausente, contraseña incorrecta y fuentes ambiguas). |
| `remote-antiforgery-first` | 32832 | Token antiforgery ligado a la cuenta Identity real de pruebas y cookie emitidos usando Neon; solo lecturas en Identity. |
| `remote-antiforgery-restart` | 12440 | Cookie y token autenticado anteriores aceptados tras terminar el proceso anterior; token inválido devuelve 400; SignOutAsync elimina la cookie y la ruta protegida vuelve a redirigir al login, sin escribir Identity. |

Resultado: **las seis fases terminaron correctamente**. Postflight final: dos claves, una entrada de migración, dos tablas en `security`, permisos efectivos correctos y cero diferencias en `public` respecto del baseline posterior a la migración.

Se verificó `encryptedSecret` y ausencia de `masterKey` en claro en ambas filas. No se imprimieron XML, certificados privados, contraseñas, cookies, tokens ni cadenas de conexión. Los certificados de pruebas son RSA 3072 y están identificados como `NEON-TEST-ONLY`; se conservaron ambos, sin revocar ni eliminar claves históricas.

### Qué no demuestran estas pruebas

- El inicio de sesión con la **contraseña real** del administrador no se probó. Para evitar cambios en `access_users`, la emisión de su cookie fue un sign-in controlado en TestServer, no una validación de su contraseña.
- El endpoint real `/salir` sobre Identity Neon actualiza el security stamp; **no se ejecutó contra Neon**. Se comprobó íntegramente sobre SQLite con llavero Neon y se comprobó por separado el borrado de cookie sobre la identidad real sin escribir la cuenta. El harness acelera la validación del stamp a cero; la app conserva cinco minutos.
- No se ejecutó el sitio completo en Kestrel/Render, ni un circuito Blazor, ni un contenedor Linux o un teléfono físico. No se certifica un reinicio/redeploy de Render.
- El rechazo de configuraciones incompletas es validación de arranque; no significa que exista ya un certificado definitivo configurado.

## 4. Custodia de certificados de pruebas

El directorio temporal tiene ACL restringida al usuario local. Como sus claves cifran filas históricas que no se borrarán, se respaldaron además por entrada estándar, sin imprimir valores, en User Secrets `InventarioCPR-local`:

- `DataProtectionValidation:Neon:CertificateA:Base64`
- `DataProtectionValidation:Neon:CertificateB:Base64`
- `DataProtectionValidation:Neon:CertificatePassword`

La comprobación de recuperación del respaldo pasó. Estas claves **no son** `DataProtection:Certificate:*` y no activan ni cambian el proveedor habitual del desarrollo. User Secrets es almacenamiento de desarrollo fuera de Git, no una bóveda empresarial. Respaldar en el gestor seguro antes de depender operativamente de estos certificados. No borrar ese respaldo mientras existan filas que lo necesiten.

No usar A ni B como certificado activo definitivo de Render. Si Render usa este mismo llavero, ambos deben estar disponibles **solo como certificados históricos de descifrado** hasta un recifrado autorizado; configurar solo un certificado C nuevo hará fallar el arranque.

## 5. Render: pasos previos a publicar

Todavía no se debe guardar configuración ni reiniciar el servicio sin coordinar la actualización: guardar variables en Render puede provocar un redeploy. No se han realizado estas acciones.

1. **Confirmar destino.** En el servicio existente, revisar que `ConnectionStrings__Database` sigue apuntando al endpoint de pruebas indicado arriba. En ese destino la migración ya está aplicada; no hay que repetirla. Si cambia de rama, esta validación no autoriza migrarla.
2. **Preparar certificado definitivo C.** Generar/custodiar PFX RSA con clave privada y contraseña fuera de Git; guardar copia en un gestor seguro. No usar el certificado HTTPS de desarrollo. Preparar el PFX como Base64 sin imprimirlo en chat o logs.
3. **Preparar estas variables seguras** en el dashboard, para aplicarlas en una ventana autorizada:

   | Variable | Valor/origen |
   | --- | --- |
   | `DataProtection__UseDatabase` | `true` |
   | `DataProtection__Certificate__Base64` | PFX definitivo C en Base64 |
   | `DataProtection__Certificate__Password` | Contraseña de C |
   | `DataProtection__PreviousCertificates__0__Base64` | Certificado histórico A del respaldo local seguro |
   | `DataProtection__PreviousCertificates__0__Password` | Contraseña del respaldo de pruebas |
   | `DataProtection__PreviousCertificates__1__Base64` | Certificado histórico B del respaldo local seguro |
   | `DataProtection__PreviousCertificates__1__Password` | Contraseña del respaldo de pruebas |

   Abrir los secretos con **Manage User Secrets** del IDE y transferirlos solo a los campos seguros/gestor de secretos; no usar `user-secrets list`, capturas ni pegar valores aquí. Alternativamente usar archivos secretos PFX y `...__Path` con ruta absoluta; nunca configurar Path y Base64 juntos.

4. **Conexión y disco.** Puede omitirse `ConnectionStrings__DataProtection` para reutilizar `ConnectionStrings__Database`. Si se usa un rol dedicado, comprobar permisos mínimos de `DATA_PROTECTION.md` antes de activarlo. `DataProtection__KeysPath` ya no se usa; Render Free no necesita disco para el llavero. Mantener Identity y el resto de variables actuales; no añadir un certificado Kestrel.
5. **Preparar transición y credenciales.** Resolver la recuperación/importación segura de `/var/data/keys` antes de cambiar variables o imagen. Si esas claves ya desaparecieron, habrá un nuevo login inicial; las pruebas Neon no recuperan las cookies del llavero de disco. Las rotaciones reales siguen pendientes de autorización según `CREDENTIAL_ROTATION.md`.
6. **Activación autorizada.** Solo después de autorización de publicación/despliegue, activar C con A/B de lectura y comprobar la generación de una nueva clave cifrada con C antes de dar por completada la transición. Cambiar el certificado activo no recifra las filas antiguas ni fuerza por sí solo una nueva clave simétrica; se conserva esa operación de activación pendiente. No borrar A/B ni las filas para eludirla.
7. **Prueba final Render.** Login manual real, formulario protegido, token antiforgery válido/inválido, logout, nueva sesión y redeploy controlado conservando la misma cookie. Confirmar `/healthz`, boot Blazor, `wss`, lectura de inventario y foto privada. Hasta entonces Render permanece **pendiente de validar** con esta versión.

## Conclusión

**Sí: el almacenamiento Data Protection funciona realmente en PostgreSQL de Neon en la rama autorizada**, incluyendo procesos reiniciados y rotación con historia. Faltan configuración/certificado definitivos, transición del llavero anterior, acciones de credenciales autorizadas y validación/despliegue reales de Render. No se publicó ninguna modificación.
