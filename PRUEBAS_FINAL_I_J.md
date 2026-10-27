# SpotifyNet · Comprobación final de los bloques I y J

Versión de integración: 27/10/2026. No requiere nuevas migraciones de SQLite.

## Prueba rápida (cuenta `prueba` y luego cuenta `cano2000`)

1. Detén `dotnet run` con **Ctrl+C** y ejecuta el instalador del paquete. Debe aparecer **Compilación correcta**. Después inicia `dotnet run --launch-profile http` desde tu proyecto.
2. Entra en `http://localhost:5267`, inicia sesión y ve a **Hecho para [usuario]**.
3. Abre **Mix diario 1**. **No** se debe abrir el explorador de archivos. Debe abrirse `/Mixes/Detalle?numero=1`, mostrar tus canciones guardadas y comenzar la reproducción.
4. Prueba los **Mixes 2 a 6**, cada uno con orden diferente. En sus páginas pulsa **Reproducir mix**, el botón ▶ de una canción y **+ Añadir a la cola**.
5. Comprueba que el panel de **Fila de reproducción** muestra las canciones y que el botón Siguiente reproduce la siguiente sin importar cuál de los mixes estés escuchando.
6. Mientras suena un tema cambia **Inicio → Mix diario 3 → Playlist CANO → Inicio**. La reproducción no debería detenerse al navegar dentro de la aplicación.
7. Pulsa **Ctrl+K** para enfocar la búsqueda; en un espacio vacío de la página pulsa **barra espaciadora** para pausar o reanudar; pulsa **M** para silenciar y volver a activar el volumen. Al recargar la página, el nivel de volumen se conserva.
8. Comprueba que **+ Añadir canciones** en una playlist continúa abriendo archivos y guardándolos: esta acción SÍ debe funcionar, a diferencia de las tarjetas de mixes.
9. Reduce la anchura de la ventana y comprueba biblioteca lateral, panel derecho, fila de reproducción y página de mixes.
10. Abre `/Mixes/Detalle?numero=7`: debe responder **404**. Cierra sesión y abre `/Mixes/Detalle?numero=1`: debe solicitar iniciar sesión.
11. Repite en otra cuenta: los mixes no deben mostrar MP3 de la primera cuenta.

## No confundir

- **Mix diario** usa canciones que subiste y ya guardaste. No reproduce canciones del catálogo oficial de Spotify.
- **+ Probar audio local** es temporal y abre un selector de archivos como corresponde.
- El contenido en Podcasts y Audiolibros sigue siendo de demostración. No se afirma que esas funciones estén implementadas.

## Comprobar Docker y GitHub

- `dotnet build` debe terminar correctamente en Windows (.NET 10 SDK).
- `docker compose up --build -d` debe crear un contenedor nuevo *con otra biblioteca*. Docker no importa automáticamente tu SQLite local.
- `git status` nunca debe mostrar `spotifynet.db`, `App_Data/audio`, archivos MP3 ni respaldos privados antes de subir a GitHub.

## Si algo falla

El instalador crea una copia de seguridad de cada archivo sustituido en `SpotifyNet/App_Data/respaldos-bloque-j_<fecha>`. También intenta restaurar lo anterior si la compilación falla. Conserva la copia de toda tu carpeta que ya tenías como respaldo principal.
