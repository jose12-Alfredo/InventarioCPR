---
version: 1
slug: "assignments-and-availability"
primary_target: "Components/Pages/Assignments.razor"
related_targets: ["Components/Assignments/DirectoryEditor.razor", "Components/Assignments/AssignmentHistory.razor"]
---

# Empresas, custodias y disponibilidad

Mode: Operate
Scope: extensión aprobada de Inventario y activación del módulo Asignaciones. Conservar shell violeta, formularios existentes y detalle en ventana amplia; no agregar un lateral permanente.

## Direction contract

THESIS: conocer inmediatamente cuántos definitivos están libres/ocupados y quién los tiene, sin convertir ausencia de conciliación o condición técnica en un tercer estado.

OWN-WORLD: conservar paleta CPR, tipografía Aptos/Segoe UI, superficies blancas, bordes finos y violetas de selección del sistema. Exactamente tres indicadores compactos solicitados por el usuario, sin números de demostración en producción.

STORY: filtrar por empresa propietaria o usuaria de manera explícita, consultar responsable/área real, entrar al expediente y gestionar una operación en flujo separado. Administrar catálogos sin borrar su historia.

FIRST VIEWPORT: tres indicadores, aviso de conciliación si corresponde y listado existente con filtros. Desktop mantiene nueve columnas; tablet/celular usa tarjetas legibles. Asignaciones abre búsqueda y resumen del activo, clasificación independiente, comando e historial; catálogos en vista separada dentro del módulo.

FORM: extensión de la composición existente, fijada por el requerimiento del usuario, sin nueva identidad visual ni torneo de conceptos. La auditoría se consulta progresivamente; formularios responsivos con controles nativos y etiquetas específicas.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Verificación

Pruebas locales y capturas iniciales en `.impeccable/review/assignments-*`. Validación autorizada posterior con Neon/Cloudinary reales y fixtures nuevos en `.impeccable/review/assignments-neon-*`: 78 checks (13 estados ×6 viewports), Axe0/overflow0/pageErrors0, teclado/tacto sintetizado, visor contain y privacidad aprobados; detector `[]`. Ninguna de esas capturas es una publicación Render. Esquema en `ASSIGNMENTS.md`; evidencia y límites de ambas fases separados en `ASSIGNMENTS_NEON_VALIDATION.md`.

66 comprobaciones de viewport/estado, Axe0/overflow0/errores0, teclado y tacto sintetizado aprobados, incluidas Borradores y las tres formas de movimiento con condiciones explícitas. El revisor independiente pidió corregir selección visual; se ajustaron aria-pressed y CSS y el verdict pass la calificó resuelta, disposition ship. También revisó los campos de inspección y la ausencia de disponibilidad en borradores, sin defectos materiales en ese delta. Documentador confirmó continuidad de tokens CPR; se actualizó solo prosa de Layout/indicadores/rutas, sin nuevos tokens ni rasters para publicar.
