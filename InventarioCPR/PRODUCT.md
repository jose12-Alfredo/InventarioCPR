# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

El usuario principal es el personal de Inventario/TI del consorcio publicitario. Necesita conocer activos registrados, disponibilidad segun custodias, responsables y empresas/areas sin confundir esos conceptos. Las empresas se configuran en catalogo; no se presumen sus denominaciones legales ni relaciones societarias.

## Product Purpose

InventarioCPR controla los activos tecnologicos, sus responsables, condiciones, mantenimientos, costos y evidencias durante toda su vida util. El exito consiste en mantener un expediente digital permanente y una trazabilidad historica confiable de cada equipo.

## Positioning

Cada activo se trata como un expediente permanente: las nuevas asignaciones, condiciones, fotografias, movimientos, mantenimientos y gastos agregan historia sin reemplazar los registros anteriores.

## Operating Context

Es una herramienta empresarial interna utilizada desde escritorio, tablet y celular. Inventario ya registra expedientes y fotografias privadas; la fase local de empresas/asignaciones permite consultar Libre/Ocupado y gestionar custodia auditada. La publicacion de cada fase y su esquema se autorizan por separado; las capturas locales de prueba no se presentan como datos empresariales.

## Capabilities and Constraints

- Blazor Web App con C#, ASP.NET Core e interactividad Interactive Server.
- Arquitectura monolitica modular con PostgreSQL/Entity Framework Core; Identity separado de los trabajadores y fotografias privadas en Cloudinary.
- Validaciones criticas en servidor, transacciones y conservacion del historial. Disponibilidad exclusivamente Libre/Ocupado derivada de custodia principal; empresas propietaria/usuaria y areas son independientes.
- Inicio e Inventario existentes; Asignaciones y catalogos empresariales implementados y validados con migracion autorizada solo en Neon test-migracion-inicial. El modulo sigue pendiente de publicacion autorizada. Mantenimientos y formularios de otros tipos de activos siguen siendo fases futuras.
- No deben presentarse acciones simuladas como si guardaran informacion.
- Las reglas confirmadas, propuestas y pendientes conservan la clasificacion de `BUSINESS_RULES.md`.

## Brand Commitments

- Nombre: InventarioCPR, sistema interno de Consorcio Publicitario.
- Voz profesional, directa y sobria.
- Paleta vinculante: violeta principal `#6D0DFF`, violeta oscuro `#3F087F`, naranja `#F39400`, fondo `#F6F7FB`, superficies `#FFFFFF`, texto `#24262B` y bordes `#DEE1E8`.
- El resultado no debe sentirse decorativo ni saturado.

## Evidence on Hand

- Reglas y alcance: `BUSINESS_RULES.md`.
- Estado y decisiones del proyecto: `.opencode/MEMORY.md`.
- Las pruebas usan fixtures etiquetados: PostgreSQL local desechable y validacion remota autorizada exclusivamente en test-migracion-inicial. No inventar indicadores ni extrapolar esos fixtures a catalogos empresariales reales. Resultados y limites: `ASSIGNMENTS.md` y `ASSIGNMENTS_NEON_VALIDATION.md`.

## Product Principles

- Preservar la historia antes que simplificarla mediante sobrescritura.
- Hacer visible el estado real de cada equipo sin mezclar dimensiones distintas.
- Priorizar claridad operativa y trazabilidad sobre ornamentacion.
- Desarrollar capacidades por etapas sin aparentar funciones aun inexistentes.
- Mantener accesibles las tareas frecuentes en cualquier tamano de pantalla.

## Accessibility & Inclusion

La interfaz debe permitir navegacion por teclado, foco visible, contraste suficiente, controles con nombres accesibles y adaptacion a escritorio, laptop, tablet y celular.
