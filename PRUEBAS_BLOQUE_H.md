# SpotifyNet: pruebas del Bloque H

Estas pruebas se ejecutan **en tu equipo Windows**, después de aplicar el instalador y arrancar SpotifyNet. No se han realizado desde este entorno.

## Antes de empezar

1. Comprueba en la terminal que `dotnet build` finaliza sin errores.
2. Ejecuta `dotnet run --launch-profile http` y abre `http://localhost:5267`.
3. Inicia sesión con una cuenta que ya tenga archivos MP3 propios guardados.

## Buscador global

- [ ] En «¿Qué quieres reproducir?», escribe parte del título de una canción. Comprueba que se muestran canciones con su artista y álbum.
- [ ] Pulsa ▶ en un resultado. Comprueba que empieza a sonar **sin cambiar de página**.
- [ ] Usa Siguiente y Anterior para recorrer las demás coincidencias de esa búsqueda.
- [ ] Utiliza ↗ para abrir la playlist de origen sin detener el audio.
- [ ] Busca el nombre de una playlist como CANO y abre el resultado.
- [ ] Busca un artista que hayas escrito en los metadatos. Debería ofrecer el acceso a su grupo en «Mi música».
- [ ] Busca un álbum que hayas configurado, distinto de «Sin álbum». Debería ofrecer el acceso al catálogo de álbumes.
- [ ] Usa ↑ y ↓ sobre los resultados para seleccionar, Enter para abrir/reproducir y Esc para cerrar.

## Pantalla completa

- [ ] Escribe dos letras o más y presiona Enter sin seleccionar un resultado; debe aparecer `/Buscar/Explorar?q=...`.
- [ ] Filtra por «Todo», «Canciones», «Playlists», «Artistas» y «Álbumes».
- [ ] Reproduce una canción en la pantalla de resultados y navega a otra playlist. El audio debe continuar.
- [ ] Usa «Buscar» dentro de esa página con otra consulta. Debe actualizar los resultados sin reiniciar el reproductor.
- [ ] Contrae la biblioteca y reduce el ancho de la ventana. Comprueba que las tarjetas y los filtros no se salen de pantalla.

## Separación de cuentas y datos

- [ ] Cierra sesión y entra con otra cuenta distinta. Sus resultados no deben incluir audios privados de la primera cuenta.
- [ ] Comprueba que tus playlists, MP3, portadas y «Tus me gusta» anteriores siguen ahí.
- [ ] Vuelve a entrar con la primera cuenta y confirma que la búsqueda sigue funcionando.

**Importante:** el buscador usa tus canciones subidas. No busca en el catálogo comercial de Spotify ni reproduce música que no hayas importado.
