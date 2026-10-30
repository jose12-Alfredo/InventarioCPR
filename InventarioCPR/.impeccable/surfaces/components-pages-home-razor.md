---
version: 1
slug: "components-pages-home-razor"
primary_target: "Components/Pages/Home.razor"
related_targets: ["Components/Pages/Inventory.razor"]
---

# Inicio e inventario

Mode: Operate
Scope: shell principal, pagina de inicio y estructura visual de inventario.
Audience: personal de Inventario/TI.
Task: orientarse, entrar al inventario y comprender la estructura disponible sin confundirla con funcionalidad persistente.
Constraints: Blazor Web App, paleta CPR vinculante, accesibilidad por teclado, responsive, sin datos empresariales ni operaciones simuladas.

## Direction contract

THESIS: InventarioCPR funciona como una mesa de pruebas precisa: cada activo deja una marca verificable y cada seccion ocupa un lugar inequívoco. Rechaza el dashboard de metricas ficticias y tarjetas repetitivas como estructura principal.

OWN-WORLD: Fondo gris frio, superficies blancas, shell violeta oscuro y acentos violeta/naranja con funciones concretas. Lineas finas, cruces de registro discretas, numerales tabulares y bordes medidos crean la firma; los controles siguen siendo web familiares.

STORY: El usuario reconoce el alcance de esta fase, accede al inventario y encuentra una vista preparada para buscar, filtrar y revisar expedientes cuando existan datos. Los modulos futuros aparecen como plan, no como capacidad activa.

FIRST VIEWPORT: Sidebar fija a la izquierda, topbar blanca y lienzo principal. Inicio abre con un bloque editorial asimetrico: mensaje y accion a la izquierda, un diagrama de trazabilidad sin datos a la derecha; debajo comienza el estado de implementacion. En movil el menu precede al contenido y el diagrama se apila.

FORM: Direccion numero 5 de la lista fundamentada, mesa de control de pruebas de imprenta. Firma: una linea de registro conecta expediente, custodia, condicion e historial sin convertir la interfaz en una imitacion fisica. seed:ffd2b2a2ed3c703d

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance
