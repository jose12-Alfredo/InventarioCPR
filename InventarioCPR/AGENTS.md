# Instrucciones permanentes de InventarioCPR

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
