# SpotifyNet · Estado y límites de la versión entregada

## Implementado anteriormente y conservado

- ASP.NET Core MVC .NET 10, Identity y SQLite.
- Inicio de sesión y registro por correo, perfil y avatar.
- Playlists individuales y colaboraciones; portadas, descripciones y orden personalizado.
- Archivos locales guardados por cuenta, reproducción HTML5, cola, favoritos, volumen, aleatorio y repetición.
- Búsqueda local de canciones, playlists, álbumes y artistas; edición de metadatos.
- Compartir playlists, contenido público, perfiles y comunidad.
- Vistas adaptativas, panel derecho y biblioteca lateral.
- Dockerfile, compose, documentación básica para GitHub.

## Integración I + J de este paquete

- Corrección del selector de archivos al pulsar en los mixes: estos dejan de ser botones de carga.
- Página protegida `/Mixes/Detalle?numero=1..6` con seis mixes derivados de la biblioteca privada.
- Reproducir lista, reproducir canción, añadir canción/lista a la cola y navegar preservando el audio.
- Estados vacíos si la cuenta no tiene canciones.
- Accesibilidad básica (etiquetas, estados y foco visible), Ctrl+K para buscar, pausa con espacio y mute con M.
- Volumen guardado por navegador e integración Media Session para teclas multimedia compatibles.
- Instalación en un paso con copia de seguridad, sin reescribir `spotifynet.db`, audios ni portadas.

## Fuera de alcance: no afirmar que esté completo como Spotify comercial

- No hay streaming del catálogo de Spotify ni conexión con sus servidores.
- No están implementadas la autenticación real de Google/Apple, recomendaciones por IA basadas en hábitos o la sincronización entre múltiples dispositivos.
- Los apartados de podcasts y audiolibros son maquetas.
- Publicación pública de producción requiere HTTPS, secretos seguros, permisos y auditoría de seguridad. Este paquete contiene preparación Docker/GitHub pero no publica automáticamente ningún repositorio.

## Validación

Se revisó estáticamente la integración de JavaScript y las rutas; el resultado debe compilarse y comprobarse en el equipo Windows con .NET 10 usando el instalador y `PRUEBAS_FINAL_I_J.md`. No se ha ejecutado el programa en el equipo del usuario.
