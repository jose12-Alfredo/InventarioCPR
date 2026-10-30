---
name: InventarioCPR
description: Precision operativa para expedientes tecnologicos permanentes.
colors:
  cpr-violet: "#6d0dff"
  cpr-violet-dark: "#3f087f"
  cpr-violet-soft: "#eee5ff"
  cpr-orange: "#f39400"
  canvas: "#f6f7fb"
  surface: "#ffffff"
  ink: "#24262b"
  muted: "#626773"
  border: "#dee1e8"
  positive: "#19724c"
  danger: "#b42318"
  violet-shell-text: "#e2d8ee"
  violet-shell-bright: "#e8e0f2"
  violet-shell-muted: "#baa8d1"
  violet-shell-subtle: "#a996c1"
  violet-shell-caption: "#bfaed5"
  violet-brand-caption: "#cdbbe5"
  violet-copy-soft: "#ddd0f5"
  violet-copy-muted: "#d5c8e9"
  violet-line: "#cdb4fa"
  violet-icon-border: "#d6c1fb"
  orange-border: "#e9c47f"
  orange-ink: "#855000"
  orange-icon-border: "#f0c26d"
  orange-icon-ink: "#9a5d00"
  positive-border: "#bde3d0"
  positive-icon-border: "#aedbc6"
  violet-border: "#d2baff"
  disabled-ink: "#858a95"
  error-soft: "#fff0ee"
  error-border: "#f0b5ae"
  error-shell-border: "#e4a9a2"
  error-ink: "#791d14"
typography:
  display:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "clamp(2rem, 4vw, 3.75rem)"
    fontWeight: 720
    lineHeight: 1.02
    letterSpacing: "-0.035em"
  headline:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "clamp(1.35rem, 2vw, 1.85rem)"
    fontWeight: 690
    lineHeight: 1.15
    letterSpacing: "-0.02em"
  hero-detail:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "clamp(2rem, 3.3vw, 3.5rem)"
    fontWeight: 690
    lineHeight: 1.15
  page-title:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "clamp(2rem, 3vw, 3rem)"
    fontWeight: 720
    lineHeight: 1.02
  title:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "1.05rem"
    fontWeight: 680
    lineHeight: 1.25
  panel-title:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "1.1rem"
    fontWeight: 680
    lineHeight: 1.25
  empty-title:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "1.3rem"
    fontWeight: 680
    lineHeight: 1.25
  body:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "1rem"
    fontWeight: 400
    lineHeight: 1.55
  label:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.78rem"
    fontWeight: 680
    lineHeight: 1.3
  body-large:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "clamp(1rem, 1.4vw, 1.15rem)"
    fontWeight: 400
    lineHeight: 1.55
  nav:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.88rem"
    fontWeight: 610
    lineHeight: 1.3
  topbar:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.9rem"
    fontWeight: 700
    lineHeight: 1.3
  small:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.87rem"
    fontWeight: 400
    lineHeight: 1.4
  note:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.82rem"
    fontWeight: 400
    lineHeight: 1.35
  caption:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.72rem"
    fontWeight: 400
    lineHeight: 1.3
  micro:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.68rem"
    fontWeight: 400
    lineHeight: 1.3
  nav-label:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "0.66rem"
    fontWeight: 720
    lineHeight: 1.3
  mobile-display:
    fontFamily: "Aptos, Segoe UI, Arial, sans-serif"
    fontSize: "2.2rem"
    fontWeight: 720
    lineHeight: 1.02
rounded:
  sm: "0.5rem"
  md: "0.75rem"
  lg: "1rem"
  pill: "999px"
spacing:
  xs: "0.5rem"
  sm: "0.75rem"
  md: "1rem"
  lg: "1.5rem"
  xl: "2rem"
components:
  button-primary:
    backgroundColor: "{colors.cpr-violet}"
    textColor: "{colors.surface}"
    rounded: "{rounded.sm}"
    padding: "0.7rem 1.05rem"
    height: "2.875rem"
  button-primary-hover:
    backgroundColor: "{colors.cpr-violet-dark}"
  card:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.md}"
    padding: "1.5rem"
  input:
    backgroundColor: "{colors.canvas}"
    textColor: "{colors.ink}"
    rounded: "{rounded.sm}"
    padding: "0.65rem 0.8rem"
    height: "2.75rem"
---

# Design System: InventarioCPR

## Overview

**Creative North Star: "La Mesa de Pruebas"**

InventarioCPR combina la familiaridad de una herramienta empresarial con la precision de una prueba de imprenta. La interfaz es clara, medida y orientada al trabajo prolongado: el violeta identifica el shell, el naranja marca puntos de registro y el lienzo frio mantiene la lectura descansada.

Las marcas de registro, reticulas finas y numerales tabulares expresan trazabilidad sin convertir la aplicacion en una imitacion fisica. La densidad es moderada; cada area de trabajo conserva jerarquia, aire y estados honestos.

**Key Characteristics:**
- Shell violeta estable sobre un lienzo gris frio.
- Superficies blancas con bordes finos y profundidad ambiental.
- Naranja reservado para hitos, progreso y pequenos puntos de registro.
- Controles web familiares, etiquetas directas y estados redundantes.

## Colors

La paleta CPR usa violeta para identidad y seleccion, naranja para hitos puntuales y neutros frios para las superficies de trabajo.

### Primary
- **Violeta CPR:** shell, acciones primarias, iconos activos y foco.
- **Violeta profundo:** sidebar y superficies de firma con texto blanco.
- **Violeta de prueba:** fondos seleccionados y estados suaves.

### Secondary
- **Naranja de registro:** puntos de posicion, progreso y acentos pequenos; no llena regiones extensas.

### Neutral
- **Lienzo frio:** fondo general de trabajo.
- **Papel blanco:** tarjetas, topbar y campos principales.
- **Tinta:** titulos y texto de alta prioridad.
- **Tinta secundaria:** descripciones y metadatos.
- **Linea tecnica:** bordes, divisores y estructura tabular.

**The Registration Ink Rule.** El naranja senala un hito concreto; nunca compite con la accion primaria violeta.

**The Honest State Rule.** Estado, texto e iconografia deben seguir siendo comprensibles sin depender solo del color.

## Typography

**Display Font:** Aptos, con Segoe UI y Arial como respaldo.
**Body Font:** Aptos, con Segoe UI y Arial como respaldo.

**Character:** Una sola voz sans serif de oficina mantiene continuidad entre titulos, formularios y tablas. El contraste proviene de escala, peso y espacio, no de mezclar familias.

### Hierarchy
- **Display** (720, escala fluida, 1.02): titulos principales cortos, con ajuste compacto.
- **Headline** (690, escala fluida, 1.15): secciones y mensajes operativos.
- **Title** (680, 1.05rem, 1.25): tarjetas, paneles y encabezados de tabla.
- **Body** (400, 1rem, 1.55): contenido con un maximo recomendado de 68 caracteres.
- **Label** (680, 0.78rem): campos, estados y metadatos; mayusculas solo para agrupaciones breves.

**The One Voice Rule.** La jerarquia tipografica se resuelve con peso, escala y espacio; no se agregan fuentes decorativas a pantallas operativas.

## Layout

El shell usa sidebar fija en escritorio, topbar persistente y un lienzo central limitado a 88rem. El contenido aplica separaciones fluidas entre 2rem y 4rem. Inicio puede usar una composicion asimetrica; los modulos operativos priorizan paneles de ancho completo y rejillas alineadas.

A 68rem, las composiciones divididas pasan a una columna. A 52rem, rejillas de tres columnas se apilan y tablas reducen columnas por prioridad. A 48rem, la sidebar se reemplaza por un menu movil nativo con `details`. A 36rem, formularios y acciones ocupan una sola columna.

## Elevation & Depth

La profundidad es ambiental y escasa. Bordes de un pixel definen la mayoria de superficies; las sombras separan paneles principales y menus flotantes sin halos decorativos.

### Shadow Vocabulary
- **Ambient low:** sombra corta para paneles operativos en reposo.
- **Ambient raised:** sombra con desplazamiento y difusion para el bloque principal y el menu movil.

**The Border First Rule.** Una superficie se delimita primero con tono y borde; la sombra solo refuerza jerarquia real.

## Shapes

Los controles usan esquinas compactas de 0.5rem, los paneles 0.75rem y los bloques de portada 1rem. Los estados breves pueden usar capsulas. Las marcas de registro son circulos cruzados de linea fina; aparecen como firma puntual, nunca como patron repetido en todos los componentes.

## Components

### Buttons
- **Shape:** rectangulo compacto con esquinas suaves y altura minima de 2.875rem.
- **Primary:** violeta CPR, texto blanco y sombra corta; se oscurece al pasar el puntero.
- **Focus:** contorno violeta visible fuera del control.
- **Disabled:** fondo gris, texto atenuado y cursor de no disponible; nunca aparenta ejecutar una accion.

### Chips
- **Style:** capsula blanca o tonal con borde de un pixel, punto de estado y etiqueta breve.
- **State:** cada tono conserva contraste y texto explicito.

### Cards / Containers
- **Corner Style:** paneles operativos de 0.75rem; bloques editoriales de 1rem.
- **Background:** papel blanco sobre lienzo frio.
- **Shadow Strategy:** ambiental y subordinada al borde.
- **Internal Padding:** de 1.25rem a 2rem segun densidad.

### Inputs / Fields
- **Style:** borde tecnico, fondo gris muy claro y etiqueta encima.
- **Focus:** contorno violeta de alto contraste.
- **Disabled:** conserva legibilidad, reduce contraste de accion y muestra cursor no disponible.

### Navigation
- La sidebar violeta mantiene agrupaciones cortas. El elemento activo se invierte a blanco, conserva icono violeta y recibe un punto naranja de registro. En movil se usa un `summary` con nombre accesible y un panel vertical de ancho completo.

### Trace Board
- Una linea horizontal enlaza hitos ordenados con puntos naranja y etiquetas numeradas. En movil se convierte en una rejilla de dos columnas sin perder el orden ni la descripcion de cada etapa.

## Do's and Don'ts

### Do:
- **Do** reservar el naranja para hitos de registro y estados con significado.
- **Do** mostrar claramente cuando una funcion aun no esta conectada.
- **Do** reducir columnas por prioridad y apilar formularios en pantallas estrechas.
- **Do** usar SVG de trazo consistente para todos los iconos.

### Don't:
- **Don't** inventar metricas, activos o actividad empresarial para llenar estados vacios.
- **Don't** convertir marcas de imprenta en decoracion abundante o ruido de fondo.
- **Don't** usar el color como unico indicador de estado.
- **Don't** presentar modulos futuros como acciones disponibles.
