# Prueba física de fotografías — Android / iPhone

Ejecutar después de autorizar y completar la publicación en Render, con el destino Neon aislado confirmado. URL: `https://inventariocpr.onrender.com/inventario`. Usar únicamente la cuenta y el equipo de pruebas; nunca fotografías empresariales.

Registrar teléfono, versión Android/iOS, navegador, red y resultado de cada paso. En Android usar Chrome; en iPhone probar Safari. JPEG/PNG, hasta 10 MB y 25 MP; si el teléfono produce HEIC o capturas de muy alta resolución, elegir JPEG/«Más compatible» y resolución estándar para esta prueba.

1. **Entrar:** abrir HTTPS e iniciar sesión manualmente. No compartir contraseñas, tokens ni cookies.
2. **Elegir equipo:** buscar `VALIDA_FOTOS_A_MW9RWNV6` o el equipo ficticio designado para Render. Verificar que el destino aprobado contiene sus seis fotos antes de añadir más.
3. **Cámara:** en Evidencias → Tomar foto, fotografiar una hoja que diga «PRUEBA CPR». Revisar orientación y vista previa; no enfocar documentos o personas de la empresa. Si el navegador ofrece solo selector, registrar esa limitación.
4. **Varias imágenes:** añadir dos JPEG/PNG ficticios desde la galería. Comprobar que se conservan las selecciones y observaciones anteriores.
5. **Subir:** comprobar progreso y confirmación de cada archivo; no considerar finalizado solo por ver 100 % de transferencia. Probar un archivo inválido/excesivo y verificar el mensaje sin perder las fotos válidas.
6. **Principal:** elegir una foto y después otra. Comprobar una única principal y que ambas siguen en la galería/historial.
7. **Listado:** volver al inventario; comprobar miniatura en tarjeta/listado y la principal en el panel.
8. **Ampliar:** abrir varias imágenes, navegar y cerrar el visor; comprobar controles táctiles, orientación y ausencia de scroll horizontal inesperado.
9. **Persistencia/reconexión:** recargar y volver al equipo; fotos, principal y observaciones deben permanecer. Cambiar brevemente de red o bloquear/desbloquear el teléfono y comprobar reconexión. Confirmar que Blazor usa `wss` en las herramientas de inspección cuando estén disponibles.
10. **Salir y privacidad:** guardar la ruta protegida de una imagen de la aplicación, cerrar sesión y abrir esa ruta en una pestaña nueva/incógnita: debe pedir login o denegar, sin entregar la fotografía. No usar únicamente Atrás, que puede mostrar píxeles ya renderizados en memoria.

En una subida interrumpida, volver al mismo expediente, revisar la reserva pendiente y reintentar el mismo archivo. Debe confirmarse una sola evidencia. Registrar hora, tamaño, paso y mensaje del error; ocultar credenciales y no copiar URLs firmadas de Cloudinary en capturas o incidencias.

La supervivencia de cookies tras un reinicio del servicio y el comportamiento de WebSockets son comprobaciones adicionales del operador en Render; no reiniciar ni desplegar desde el teléfono sin autorización.
