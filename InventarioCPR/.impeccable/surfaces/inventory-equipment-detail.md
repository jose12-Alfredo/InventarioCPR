---
version: 1
slug: "inventory-equipment-detail"
primary_target: "Components/Inventory/TechnicalFormPreview.razor"
related_targets: ["Components/Inventory/EquipmentPhotoViewer.razor"]
---

# Detalle del equipo y fotografía completa

Mode: Operate

Usuario: personal Inventario/TI que consulta identidad, condición, técnica e imágenes privadas de un expediente. Petición confirmada: pulsar la principal para verla completa y reemplazar el lateral por una **ventana amplia**, elegida expresamente por el usuario.

## Direction contract

THESIS: un expediente legible en una ventana centrada, con la fotografía como acceso a evidencia y no como un recorte decorativo; evitar la columna lateral larga de datos duplicados.

OWN-WORLD: heredar Aptos/Segoe UI, violeta CPR #6D0DFF/#3F087F, tinta #24262B, superficies blancas y líneas #DEE1E8. Sin nueva identidad, nuevas fuentes ni métricas inventadas.

STORY: identificar equipo y condiciones, consultar secciones, abrir su principal completa y volver al mismo contexto. Mantener permisos y entrega privada existentes.

FIRST VIEWPORT: cabecera y cierre fijos, fotografía contenida a la izquierda, nombre/código/categoría/condiciones a la derecha, secciones debajo y acción de edición en pie persistente. En móvil, una columna y cuerpo desplazable dentro del diálogo.

FORM: composición amplia centrada aprobada por respuesta del usuario; extensión local de la interfaz existente, no reemplazo del mundo visual; sin sorteo de concepto por tratarse de una extensión elegida por el usuario. Diálogos nativos anidados para detalle y ampliación con foco/Escape independientes.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Verificación final

Revisión independiente de fuentes y capturas: disposition **ship**, sin correcciones materiales. Detector final `[]`; 19 comprobaciones de captura con Axe 0 más interacción táctil sintetizada. Resultado y límites en `../../DETAIL_VIEW_VALIDATION.md`. No se añadieron rasters estáticos al producto ni se modificaron tokens de la identidad visual.
