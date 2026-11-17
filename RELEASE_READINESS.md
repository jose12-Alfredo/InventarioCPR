# INVENTARIO CPR — informe final de preparación para publicación

**Fecha: 17 de noviembre de 2026. Estado: código preparado; publicación condicionada a seguridad/configuración y confirmación del destino.** No se hizo commit, push, despliegue, eliminación, rotación ni cambio de variables. No se modificó production.

**Autorización posterior de publicación (2026-11-17):** el usuario solicitó desplegar y autorizó expresamente commit y push a main. Confirmó que Database/Data Protection usan test-migracion-inicial, que C+A/B están configurados y que resolvió la credencial Neon expuesta. Son confirmaciones del titular, no una inspección del dashboard por el agente. Se autoriza publicar los cambios revisados y comprobar el despliegue; la purga de fixtures y cambios de production siguen fuera de alcance. El estado descrito en el informe siguiente corresponde al cierre previo a esa autorización; el push y la verificación pública se registrarán en el resultado de publicación.

## 1. Código y base de datos

La auditoría utilizó transacción PostgreSQL **RepeatableRead READ ONLY** y confirmó TLS VerifyFull, rama `test-migracion-inicial` (`br-flat-smoke-b4kdmosq`), endpoint `ep-raspy-dream-b476uukh`, base `neondb`.

| Contexto / historial | Migraciones aplicadas | Pendientes / cambios modelo |
|---|---|---|
| InventoryDbContext / `public.__EFMigrationsHistory` | `20261028145612_InitialInventorySchema`; `20261109161923_EquipmentPhotoHistory`; `20261112200209_EquipmentDraftFinalization`; `20261116181150_CompaniesAndEquipmentCustodies` | **0 / ninguno** |
| AccessDbContext / `public.__AuthMigrationsHistory` | `20261028212902_AddIdentityAccess` | **0 / ninguno** |
| ProtectionDbContext / `security.__DataProtectionMigrationsHistory` | `20261112184907_PersistProtectionKeys` | **0 / ninguno** |

Las filas anteriores de las **36 tablas** siguen conservadas frente al baseline postmigración; Identity, claves y sus historiales sin cambios. Estado: **55 equipos / 21 fotos**, incluidos los nueve equipos/ocho fotos nuevos ficticios. No hace falta otra migración para publicar este código **contra esta rama**. Esto no certifica ninguna otra rama ni el destino interno actual de Render.

También se comprobaron los **tres triggers de asignaciones habilitados** y cuatro índices únicos críticos válidos (código, empresa, principal fotográfica y custodia activa), mediante otra transacción READ ONLY.

| Módulo | Estado comprobado | Pendiente |
|---|---|---|
| Empresas y áreas | Catálogos dinámicos, revisiones y auditoría | Confirmar nombres legales y datos reales |
| Trabajadores | Sin requerir Identity; área/habilitación controladas | Revisar personas reales y homónimos |
| Custodias | Asignar/transferir/devolver, historial, concurrencia/idempotencia validados | Conciliación física |
| Disponibilidad | Solo Libre/Ocupado; definitivos y totales coherentes | No confundir Libre digital con comprobación física |
| Fotos | Principal real obligatoria; ocho recursos nuevos aún existentes authenticated con versiones correctas | Retiro autorizado de fixtures, teléfono físico pendiente |
| Data Protection | Llavero preservado; certificado C y A/B disponibles localmente, válidos y con privada | Verificar configuración efectiva del servicio Render |

### Comprobaciones de esta preparación

- Publish Release aislado correcto; runtime `wwwroot/_framework/blazor.web.js` y bibliotecas Skia Linux incluidos. Sin certificados, archivos privados, secretos o configuración Development en el paquete revisado.
- Suite segura sin opt-ins: **54 aprobadas, 0 fallidas, 35 omitidas**. No se repitieron pruebas persistentes que crearían más fixtures.
- Auditoría NuGet, incluidos paquetes transitivos: **sin vulnerabilidades conocidas** en ambos proyectos según los orígenes consultados; no sustituye la revisión de credenciales/configuración.
- La validación inmediatamente anterior conserva su resultado **89 aprobadas / 0 fallidas / 0 omitidas**, 23 checks reales de servicio y 78 web, documentado en `InventarioCPR/ASSIGNMENTS_NEON_VALIDATION.md`.
- Docker no está instalado en esta sesión; el build/arranque Linux y bibliotecas nativas se deberán comprobar en Render. No se certifica por un publish de Windows.
- GET público actual: `/healthz` **204**, `/acceso` **200**, `/_framework/blazor.web.js` **200**, con TLS verificado. No demuestra qué commit, variables o conexión Neon tiene Render; no se usó una sesión autenticada de producción.

## 2. Seguridad y Render: condiciones pendientes

1. **Confirmar en el dashboard el servicio, rama Git, deployment/commit y endpoint Neon efectivos.** Si no coincide con la rama autorizada, detener este plan; no migrar ni cambiar production. ASPNETCORE_ENVIRONMENT=Production no identifica la rama PostgreSQL.
2. **Rotar la credencial PostgreSQL expuesta**, después de identificar rol/consumidores y descartar impacto en production. No se cambió en esta preparación. Procedimiento en `InventarioCPR/CREDENTIAL_ROTATION.md`.
3. La contraseña Identity expuesta anteriormente ya fue sustituida según la evidencia de `SECURITY.md`; no existen secretos locales de bootstrap/reset. Confirmar custodia de la contraseña vigente; no repetir el reset por rutina. Inventariar exposición/rotación Cloudinary y revocar credenciales anteriores si corresponde, sin cambiar evidencias.
4. Verificar el certificado activo **C** (RSA3072, válido hasta **2028-11-13 16:16:56 UTC**) y los históricos **A/B** (hasta 2027-11-12, necesarios para leer las dos claves existentes). Ya existen; no pedir/generar otro certificado como requisito rutinario.
5. Comparar las variables exactas y respaldar configuración/versión de reversión. Detener auto-deploy antes de un futuro push autorizado; no se cambió ahora.

La actualización exacta, responsables, variables y reversión están en **`DEPLOYMENT.md`**. Seguridad pendiente no significa que haya una migración nueva por desarrollar.

## 3. Datos reales y fixtures

- Nueve equipos y ocho fotos identificados por IDs en **`InventarioCPR/TEST_DATA_RETIREMENT.md`**; procedimiento con respaldo, relaciones, excepción auditada y fases PostgreSQL/Cloudinary. **No se eliminó nada.**
- Hoja de 46 filas exportada y guía operativa en **`InventarioCPR/CONCILIATION_GUIDE.md`**. No hay importación automática ni cambios masivos de responsables.
- **Hallazgo de calidad de datos:** 41 de esos 46 registros anteriores tienen marcadores de pruebas de fases previas. No equivalen a 46 activos empresariales certificados. Están señalados para revisión y fuera de la purga de nueve equipos; los otros cinco también requieren confirmación.
- CPRSA/DMA/MAA pueden registrarse en el catálogo existente con nombre visible libre; razón social/identificador fiscal opcionales. No se cargaron sin confirmación.

## 4. Orden para una publicación futura autorizada

Confirmar destino/configuración y recuperación → resolver credenciales expuestas → decidir tratamiento de fixtures/preparar catálogos confirmados → revisar diff y autorizar commit/push → desplegar el commit exacto desde Render → smoke tests y reinicio controlado → habilitar uso empresarial después de conciliar los activos reales.

El retiro de fixtures no se mezcla con el despliegue ni se ejecuta por arrancar. Se puede evaluar un piloto con fixtures claramente reconocidos, pero no presentarlo como inventario empresarial limpio.

## Evidencia de esta auditoría

Directorio privado: `C:\Users\jose1\AppData\Local\Temp\opencode\assignments-neon-validation\`.

- `release-audit.result.json`: historiales, conservación, conteos y metadata de certificados, sin secretos.
- `release-schema.result.json`: triggers habilitados e índices únicos críticos válidos.
- `release-fixture-manifest.json`: nueve equipos, ocho fotos y recursos exactos, más marcadores previos fuera de alcance.
- `release-related.private.json`: relaciones y FKs para revisión de mantenimiento.
- `conciliacion-46-equipos.csv`: hoja UTF-8 separada por `;`, editable localmente, sin importador.

Los respaldos, hojas de datos, certificados y cookies permanecen fuera de Git. La preparación solo cambió documentación y herramientas temporales de auditoría; no modificó módulos ni empezó formularios de muebles/electrodomésticos.
