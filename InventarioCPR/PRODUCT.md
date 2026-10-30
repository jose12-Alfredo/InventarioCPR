# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

El usuario principal de la primera interfaz es el personal de Inventario/TI de Consorcio Publicitario. Trabaja en un sistema interno para registrar, consultar y controlar activos tecnologicos y necesita orientarse rapidamente hacia el inventario.

## Product Purpose

InventarioCPR controla los activos tecnologicos, sus responsables, condiciones, mantenimientos, costos y evidencias durante toda su vida util. El exito consiste en mantener un expediente digital permanente y una trazabilidad historica confiable de cada equipo.

## Positioning

Cada activo se trata como un expediente permanente: las nuevas asignaciones, condiciones, fotografias, movimientos, mantenimientos y gastos agregan historia sin reemplazar los registros anteriores.

## Operating Context

Es una herramienta empresarial interna utilizada principalmente desde escritorio y laptop, con acceso adaptable a tablet y celular. La primera fase visual orienta al usuario y ofrece acceso claro al modulo de inventario; no implementa persistencia ni operaciones de base de datos.

## Capabilities and Constraints

- Blazor Web App con C#, ASP.NET Core e interactividad Interactive Server.
- Arquitectura monolitica modular; PostgreSQL y Entity Framework Core estan previstos, pero no forman parte de esta fase.
- Las validaciones criticas futuras deben ejecutarse en el servidor.
- Los modulos se desarrollan progresivamente; solo Inicio e Inventario se habilitan en esta fase.
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
- No existen datos empresariales, metricas, fotografias ni testimonios reales para mostrar; no deben fabricarse.

## Product Principles

- Preservar la historia antes que simplificarla mediante sobrescritura.
- Hacer visible el estado real de cada equipo sin mezclar dimensiones distintas.
- Priorizar claridad operativa y trazabilidad sobre ornamentacion.
- Desarrollar capacidades por etapas sin aparentar funciones aun inexistentes.
- Mantener accesibles las tareas frecuentes en cualquier tamano de pantalla.

## Accessibility & Inclusion

La interfaz debe permitir navegacion por teclado, foco visible, contraste suficiente, controles con nombres accesibles y adaptacion a escritorio, laptop, tablet y celular.
