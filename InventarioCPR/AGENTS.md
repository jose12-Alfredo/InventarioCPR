# Instrucciones permanentes de InventarioCPR

## Preparación final de publicación — 2026-11-17

- Autorización posterior vigente: el usuario pidió «desplegar» y confirmó expresamente **Sí, commit y push** y **Sí, todo confirmado** para destino test-migracion-inicial, C+A/B y resolución de la credencial expuesta. Se permite publicar los archivos revisados y comprobar Render. Las prohibiciones de publicación siguientes describen la fase de preparación anterior; siguen prohibidos borrar fixtures, cambiar production o modificar secretos sin autorización específica. La configuración efectiva fue confirmada por el titular, no inspeccionada por el agente.
- Alcance vigente: auditoría y documentación; **no commit/push/despliegue, eliminación, cambio de variables/credenciales ni modificación de production**. No seguir modificando módulos ya validados sin una necesidad concreta. No iniciar muebles/electrodomésticos.
- La revisión READ ONLY confirmó 4 migraciones Inventory + 1 Identity + 1 Data Protection aplicadas en test-migracion-inicial, cero pendientes y cero cambios de modelo; filas previas de 36 tablas conservadas. No repetir migraciones para publicar contra esa rama. Otro destino necesita comprobación/autorización propia.
- Informe vigente: `../RELEASE_READINESS.md`; pasos exactos de Render y reversión en `../DEPLOYMENT.md`. C definitivo ya existe; conservar A/B históricos. Configuración efectiva de Render y rotación Neon expuesta siguen pendientes de intervención/autorización del titular; no mostrar secretos ni generar certificados por rutina.
- Nueve fixtures y ocho imágenes nuevos identificados en `TEST_DATA_RETIREMENT.md`; purga **solo preparada**, requiere respaldo actual, ensayo y autorización explícita de excepción a historia inmutable. No deshabilitar triggers, usar CASCADE ni borrar por prefijos como efecto de publicar.
- Hay 46 registros anteriores, **41 con marcadores de pruebas previas**; no llamarlos 46 activos empresariales confirmados ni incluirlos automáticamente en la purga de nueve. Hoja privada de 46 filas y flujo existente en `CONCILIATION_GUIDE.md`. No inventar custodios, propietarios o denominaciones fiscales.
- Última suite completa autorizada: 89/0/0. En esta preparación solo se ejecutó suite segura 54/0/35 y publish Release, sin crear fixtures; auditoría NuGet sin vulnerabilidades conocidas en ambos proyectos. Docker/Linux y comprobación autenticada del nuevo despliegue siguen pendientes.

## Empresas, custodias y disponibilidad

- La disponibilidad tiene **solo Libre y Ocupado**, derivada de la ausencia/existencia de custodia principal activa. No crear selector manual ni tercer estado; conciliación y condición técnica son dimensiones separadas.
- Indicadores Total/Libres/Ocupados cuentan solo definitivos, respetan ámbito de empresa/filtros y deben mantener Libres+Ocupados=Total, antes de paginar. La disponibilidad filtra filas después del desglose.
- Empresa propietaria, empresa usuaria, área administrativa del activo, área del trabajador, ubicación y responsable son independientes. No inferir ni cambiar una dimensión a partir de otra. Catálogos dinámicos; no precargar CPRSA/DMA/MAA sin denominaciones confirmadas.
- Reutilizar StaffMember/Area/Department/Site/Location. Un trabajador no es una cuenta Identity; inspectores anteriores no se convierten en custodios automáticamente. Conservar IDs y snapshots históricos en los movimientos.
- Asignar/transferir/devolver requiere actor Identity autorizado, transacción, revisión/custodia esperada y recibo idempotente. Una transferencia cierra/abre atómicamente; no borrar auditoría ni modificar periodos cerrados.
- Conservar registro rápido/completo, fotografía obligatoria y expediente en ventana amplia; no introducir panel lateral permanente. Asignaciones se gestiona en flujo separado `/asignaciones`.
- Migración `20261116181150_CompaniesAndEquipmentCustodies` aplicada y validada con autorización el 2026-11-17 **solo en `test-migracion-inicial`** (`br-flat-smoke-b4kdmosq`, endpoint `ep-raspy-dream-b476uukh`). No repetirla ni ejecutarla en otra rama sin autorización. No hay autorización de commit/push/despliegue ni conciliación de activos anteriores. Procedimiento/evidencia en `ASSIGNMENTS.md` y `ASSIGNMENTS_NEON_VALIDATION.md`.
- Futuras categorías se apoyan en Equipment general y perfil técnico separado. No renombrar tablas ni rehacer informática funcional como efecto secundario. No afirmar implementados formularios de muebles/electrodomésticos.

## UI, UX y responsive

Estas reglas son obligatorias para cualquier trabajo que cree o modifique UI/UX, CSS, componentes Razor, diseño responsive o accesibilidad:

- Antes de comenzar el trabajo visual, cargar y consultar realmente la skill global `Impeccable` mediante la herramienta de skills. Mencionarla en la documentación o afirmar que se siguió sin haberla cargado no cumple esta regla.
- Aplicar durante el trabajo las instrucciones pertinentes de `Impeccable`. Mantener la skill como instalación global de OpenCode; no copiarla ni instalarla dentro del proyecto salvo que exista una necesidad técnica explícita.
- Antes de declarar terminado el trabajo, ejecutar las verificaciones pertinentes indicadas por `Impeccable`, validar visualmente el resultado e informar de forma concreta qué verificaciones se realizaron y cuál fue el resultado de cada una.
- Diseñar toda modificación visual de forma responsive para computadora, tablet y celular.

- Revisar `DESIGN.md` y la implementación existente antes de modificar Razor o CSS.
- Adaptar estructura e interacciones por contexto; no limitarse a reducir una composición de escritorio.
- Validar como mínimo 360x800, 390x844, 768x1024, 1024x768, 1440x900 y 1920x1080.
- Comprobar teclado, foco, controles táctiles, formularios, estados, ausencia de scroll horizontal no previsto y capturas reales.
- No modificar base de datos ni lógica empresarial solo para satisfacer requisitos visuales.

## Registro de equipos y fotografías

- RN-111/117: las altas nuevas son Borradores hasta tener mínimos válidos y una fotografía principal real confirmada. La autoridad es el servicio del servidor, no el botón de Razor.
- Reutilizar ID de borrador y reservas de subida en todos los reintentos. Los borradores se consultan aparte y no se incluyen en el conteo/listado normal de definitivos.
- Conservar equipos anteriores sin foto mediante la excepción legacy de la migración; no inventar evidencias ni fechas. No eliminar fotos ni periodos históricos, ni permitir democión de definitivos o retirada de la exigencia de foto.
- La migración `20261112200209_EquipmentDraftFinalization` se aplicó con autorización únicamente en `test-migracion-inicial` (`br-flat-smoke-b4kdmosq`). No repetirla en otra rama sin autorización. Las pruebas automatizadas ordinarias siguen usando bases aleatorias loopback; la validación remota controlada autorizada está en `MANDATORY_PHOTO_REAL_VALIDATION.md`.
- Data Protection ya validado se conserva. No mezclar su contexto, migración o certificados con esta funcionalidad. Mantener Identity y políticas vigentes.
- Estado y límites comprobados en `MANDATORY_PHOTO_REGISTRATION.md` y `MANDATORY_PHOTO_REAL_VALIDATION.md`; distinguir la fase simulada anterior del flujo real aprobado de Neon/Cloudinary. Chromium emulado o `setInputFiles` no certifican una cámara física. No hubo publicación por aplicar la migración.
