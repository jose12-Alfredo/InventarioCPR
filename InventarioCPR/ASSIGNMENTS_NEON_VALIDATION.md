# Empresas y asignaciones — validación Neon autorizada

Fecha: **17 de noviembre de 2026**. La migración y las pruebas se ejecutaron únicamente en `test-migracion-inicial`. Código del módulo todavía sin commit/push/despliegue; el navegador utilizó un host HTTPS local con Neon y Cloudinary reales.

Preparación final posterior: `../RELEASE_READINESS.md` y `TEST_DATA_RETIREMENT.md` contienen la auditoría READ ONLY actual y el procedimiento de retiro, sin ejecutarlo. La hoja de `CONCILIATION_GUIDE.md` distingue que 41 de los 46 registros anteriores tienen marcadores de pruebas de fases previas; «anteriores» no significa «activos empresariales confirmados».

## Destino y preflight

- Rama comprobada por PostgreSQL: `br-flat-smoke-b4kdmosq`.
- Endpoint comprobado: `ep-raspy-dream-b476uukh`.
- Host: `ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech`; base `neondb`; TLS `VerifyFull`.
- Se reutilizaron User Secrets existentes. No se conectó a `NeonDirect` ni se modificaron credenciales, roles o certificados.
- Inventario tenía exactamente InitialInventorySchema, EquipmentPhotoHistory y EquipmentDraftFinalization; la única pendiente era **`20261116181150_CompaniesAndEquipmentCustodies`**. Identity y Data Protection no tenían migraciones pendientes.
- SQL idempotente revisado igual al generado por EF; SHA-256 del archivo: `714DEDBADBCA817083B96B20807696ABABF9705637C857D1D72623320F0C79E3`. Sin operaciones sobre Identity, Data Protection o tablas de fotografías.
- Se comprobaron permisos y ausencia de tablas/funciones nuevas. Baseline: **33 tablas, 399 filas, 46 equipos, 13 fotos, una cuenta Identity y dos claves Data Protection**.

## Respaldo y recuperación comprobados antes de migrar

El harness capturó una instantánea RepeatableRead de las tablas de `public` y `security`, con filas JSON, columnas almacenadas/generadas, restricciones, dependencias y tres secuencias. Adjuntó los tres scripts EF del esquema anterior. El respaldo contiene datos sensibles de Identity y claves cifradas: permanece fuera del repositorio, con herencia ACL retirada y acceso del usuario local.

Se restauró en una base aleatoria `inventariocpr_recovery_<GUID>`, exclusivamente en PostgreSQL local `127.0.0.1:55439`. Se regeneraron columnas calculadas, insertaron filas respetando FKs y restauraron secuencias. Coincidieron las **33 tablas, 399 filas almacenadas y definiciones de restricciones**. La base de ensayo se eliminó al terminar.

Huella del respaldo ensayado: `B168685160F6C805EFA3DCAC88C5F656169CA0CA242AA62719A633DCE2AF3A6E`.

Esto acredita una recuperación lógica de los datos/esquema de aplicación respaldados; no es una comprobación del PITR, roles/ACL de servidor ni restauración física de Neon. No se ejecutó una restauración sobre la rama remota. El certificado para descifrar las claves se conserva en su custodia habitual, fuera del respaldo JSON.

## Migración y conservación

La aplicación del SQL se hizo en transacción explícita, con timeout de locks de 5 segundos y de sentencias de 60 segundos. Antes de modificar se bloquearon las tablas anteriores contra escrituras y se verificó que el respaldo ensayado, el SQL y los datos seguían coincidiendo.

Antes del commit se comprobaron todas las columnas almacenadas y filas anteriores, el nuevo registro de migración, backfill, ausencia de seeds y los tres triggers. El comando superó el límite externo de 120 segundos durante la captura posterior al commit; **no se repitió la migración**. Una consulta independiente confirmó la aplicación, los datos conservados, ninguna migración pendiente y las guardas esperadas.

- Se añadieron `companies`, `equipment_custodies` y `equipment_operations`: 36 tablas en total.
- Los 46 equipos anteriores quedaron sin empresa/área inventadas, con conciliación pendiente, sin crearles custodias.
- Personal anterior activo pero no habilitado automáticamente para recibir equipos.
- Ninguna compañía/custodia/persona precargada por la migración.
- Historial de Identity y Data Protection intactos. La aplicación sigue sin migraciones automáticas al arrancar.

La auditoría final volvió a comparar las filas originales de las **36 tablas del baseline posterior a la migración** mediante multiconjuntos de huellas de las columnas almacenadas: ninguna fila anterior perdida o modificada. Identity, claves y sus historiales sin adiciones ni cambios. Las adiciones son fixtures ficticios; las ocho fotos nuevas pertenecen exclusivamente a los equipos nuevos.

## Pruebas automatizadas

**Suite completa Release: 89 aprobadas, 0 fallidas, 0 omitidas.** Se habilitaron las diez pruebas Neon antes omitidas y los tres opt-ins PostgreSQL locales:

| Ámbito | Resultado |
|---|---|
| InitialInventoryMigrationTests | 4 aprobadas contra Neon; escrituras transaccionales revertidas |
| PhotoPostgresTests | 2 aprobadas contra Neon; FK compuestas y principal único |
| IdentityAccessConcurrencyTests / IdentityLoginLookupTests | 2 aprobadas; Identity de Neon solo lectura |
| InventoryServiceTests persistente / RegistrationPreservationTests | 2 aprobadas; fotos reales y fixtures nuevos |
| AssignmentTests | 14 aprobadas en bases locales desechables |
| Resto de suite, incluidas regresiones foto obligatoria/DP/restore Blazor | Incluido en las 89 aprobadas |

Se actualizaron los dos tests persistentes antiguos para finalizar Borrador → Definitiva mediante fotografía Cloudinary real y conservar almacenamiento real al editar. `RemotePhotoFixture` comprueba host/TLS y branch/endpoint remotos antes de escribir, limita los códigos admitidos y no crea excepciones legacy para altas nuevas. La subida utiliza `inventariocpr-pruebas/asignaciones-20261117`, solo en configuración de pruebas.

Registro de intentos: primera selección, 10 aprobadas y 2 fallidas (confirmación inmediata de una foto rechazada y almacenamiento fotográfico ausente en el servicio de edición del test). Tras corregir el harness y añadir reintentos acotados de la misma finalización con verificación real de bytes/hash, los dos tests pasaron; después pasó la suite completa 89/0/0. No se atribuyó una causa remota demostrada al rechazo puntual de lectura. Se conservaron los fixtures de esos intentos, sin borrar evidencia ni convertir un borrador fallido en definitivo artificialmente.

Resultado TRX privado: `assignments-neon-all.trx`, dentro del directorio temporal indicado al final. Los opt-ins siguen deshabilitados por defecto; su reejecución persistente requiere autorización y deja nuevos fixtures.

## Flujos de servicio reales

Pasaron **23 comprobaciones** sobre los fixtures del módulo, con actor obtenido/validado mediante Identity existente, sin modificar la cuenta:

- Exclusión de borradores y rechazo de entrega sin condiciones observadas.
- Asignar, reintentar sin duplicar y rechazar reutilización del RequestId con contenido distinto.
- Transferencia compartida, un único custodio principal y periodos históricos; transferencia inválida conserva la custodia.
- Propietaria/usuaria y área administrativa/laboral independientes; cambiar usuaria conserva propietario/custodia.
- Devolución, filtro libres funcionales y exclusión del libre no operativo.
- Dos asignaciones concurrentes: exactamente una confirma. Reintentar devolución antigua conserva la custodia nueva.
- Desactivar/renombrar trabajador conserva custodia y snapshots; nuevas entregas a inactivo rechazadas.
- Filtros empresa/usuaria/área/responsable combinados, desglose antes de disponibilidad y datos técnicos conservados.

Resultado del ámbito empresarial ficticio: **Total 2 = Libre 1 + Ocupado 1**. La conciliación de activos antiguos sigue pendiente: no se modificaron sus responsables ni se declaró disponibilidad física comprobada. Los casos de conciliación/backfill y rollback por fallo de auditoría se probaron en PostgreSQL local dentro de AssignmentTests; no se provocó un fallo de trigger de auditoría en Neon.

## Navegador, fotografías y Data Protection

- **78 comprobaciones = 13 estados × 6 viewports**: 360×800, 390×844, 768×1024, 1024×768, 1440×900 y 1920×1080.
- Estados: inventario por empresa, filtros desplegados, ocupados, libres, expediente, visor fotográfico, asignar, formulario de transferencia, historia compartida, devolución, empresas, trabajadores y borradores.
- **Axe: 0 incidencias; overflow horizontal: 0; errores de página: 0**. Detector Impeccable final `[]`; inspección visual de capturas de escritorio, formulario móvil y visor tablet realizada.
- Navegador registró clasificación independiente, asignación y devolución sobre el monitor ficticio. La transferencia se ejecutó en el servicio real; en navegador se verificó su formulario y la historia ya registrada, sin afirmar otra transferencia web confirmada.
- Escape anidado y foco del expediente, imagen `contain`, controles y apertura/cierre por tacto sintetizado Chromium aprobados.
- Foto sin sesión: 302 a acceso. Sesión controlada Consulta: botón de asignación deshabilitado. Ninguna petición del navegador a `res.cloudinary.com`: la entrega privada pasó por la aplicación.
- Se leyeron **seis imágenes privadas reales**, cuatro históricas y dos nuevas del módulo, con bytes y SHA-256 correctos.
- **Dos claves y dos cargas históricas Data Protection descifradas** con certificado activo C y A/B de lectura. Generación automática deshabilitada en esta comprobación; ninguna clave nueva, borrada o recifrada.

El host local emitió cookies controladas a partir de la cuenta Identity existente y usó DP efímero solo para ese transporte de pruebas; la comprobación del llavero Neon se realizó separadamente con el proveedor EF real. No es prueba de contraseña real, cámara física, Safari, dispositivo físico ni arranque/despliegue Render. No se cambió código visual durante esta validación; se conserva el diseño/revisión de la fase local.

Los primeros intentos del script web requirieron esperar las opciones interactivas de trabajadores y reaplicar búsqueda tras cambiar a Borradores, que reinicia esos filtros. Se corrigió el script; la pasada final de 78 comprobaciones completó todos los controles. Las operaciones ficticias adicionales permanecen auditadas.

## Fixtures conservados y evidencia

Prefijo principal: **`VALIDA_ASIG_DFB5860FEE`**.

| Código | ID | Estado final |
|---|---|---|
| `VALIDA_ASIG_DFB5860FEE_LAPTOP` | `c0cb6690-56c7-4e8a-988c-09c302d5b4c8` | Definitivo, ocupado; custodia conservada tras inactivar al trabajador ficticio |
| `VALIDA_ASIG_DFB5860FEE_MONITOR` | `a82aa720-4190-455b-aebb-09716134bd08` | Definitivo, libre, última condición No operativo |
| `VALIDA_ASIG_DFB5860FEE_BORRADOR` | `69c00dd2-4255-4452-a370-9144d486a72a` | Borrador, sin foto; excluido de totales |

Incluyendo los intentos y las pruebas persistentes: **9 equipos nuevos (7 definitivos y 2 borradores), 8 fotos reales nuevas, 2 empresas ficticias, 6 periodos de custodia y 30 operaciones auditadas**. Equipos totales 46→55; fotos 13→21. Todos los definitivos nuevos mantienen foto obligatoria. El otro borrador es `PRUEBA_2c7b9db0b221`, con su foto conservada tras el intento fallido. El detalle de los nueve IDs está en `fixtures-report.result.json`. No se limpiaron filas ni recursos Cloudinary.

- Evidencia privada y recuperación: `C:\Users\jose1\AppData\Local\Temp\opencode\assignments-neon-validation\`.
- Informes: `preflight.result.json`, `recovery.result.json`, `migration.result.json`, `flows.result.json`, `infrastructure.result.json`, `preservation.result.json`, `fixtures-report.result.json`, `browser.result.json`, TRX.
- Respaldos `before.private.json` y `after-migration.private.json`, cookies y diagnósticos privados: **fuera de Git**, sin valores en documentación.
- Capturas ignoradas: `.impeccable/review/assignments-neon-<estado>-<ancho>x<alto>.png`; resumen `assignments-neon-browser-results.json`.

Al cerrar se detuvieron el host HTTPS temporal y el PostgreSQL local iniciado para el ensayo/pruebas. El respaldo y los informes se conservaron. `git diff --check` pasó; solo se informaron normalizaciones LF/CRLF de Git.

## Pendientes empresariales y publicación

Confirmar denominaciones legales, registrar catálogos reales y revisar los 46 registros anteriores son trabajos distintos de estos fixtures; solo se concilian como activos empresariales los confirmados, sin inventar custodios. No se precargaron CPRSA/DMA/MAA. Commit, push, despliegue y comprobación posterior en Render requieren autorización independiente. La migración ya aplicada no se repite ni se ejecuta en otra rama sin autorización.
