# SpotifyNet

Aplicacion academica independiente, inspirada en la experiencia de un reproductor musical.
Desarrollada con ASP.NET Core MVC (.NET 10), Entity Framework Core, SQLite y JavaScript.
No es un producto oficial de Spotify ni integra su catalogo musical.

## Funciones

- Registro e inicio de sesion con ASP.NET Core Identity.
- Biblioteca de playlists por usuario; crear, renombrar y eliminar listas.
- Subir y reproducir archivos MP3/WAV/OGG/M4A propios.
- Favoritos y canciones compartidas entre playlists sin duplicar MP3.
- Búsqueda unificada de canciones, playlists, artistas y álbumes con reproducción directa.
- Pantalla completa de resultados en `/Buscar/Explorar`.
- Edición de metadatos de canciones, perfiles y playlists compartidas.
- Reproductor que mantiene el audio durante la navegacion interna.

## Bloques I y J (integración de octubre de 2026)

- Seis mixes diarios de tu biblioteca privada en `/Mixes/Detalle?numero=1` a `6`.
- Un mix abre su lista de canciones y las reproduce sin seleccionar archivos de nuevo.
- Navegación interna preservando el audio, añadir a cola y reproducir pistas del mix.
- Ctrl+K para buscar, barra espaciadora para pausar/reanudar cuando la página tiene el foco y M para silenciar.
- Volumen persistente por navegador; controles de teclado y mejoras de accesibilidad.
- Revisa `PRUEBAS_FINAL_I_J.md` y `ESTADO_FINAL.md` para comprobar funciones y limitaciones.

## Windows / Visual Studio Code

Requiere .NET SDK 10.

```powershell
dotnet restore
dotnet build
dotnet run --launch-profile http
```

Abrir http://localhost:5267. La base de datos local se guarda en `spotifynet.db`
y los audios en `App_Data/audio`.

## Docker (nueva biblioteca separada)

Requiere Docker Desktop y motor Linux funcionando.

```powershell
docker compose up --build -d
docker compose ps
```

Abrir http://localhost:5267. Si el programa local usa el puerto 5267,
detener `dotnet run` antes de iniciar Docker.

Docker crea su **propia** base SQLite desde las migraciones EF Core al primer arranque
y una biblioteca de audio vacia. No importa automaticamente los usuarios ni los MP3
de Windows. Los volumenes `spotifynet_sqlite`, `spotifynet_audio` y `spotifynet_covers` sobreviven
a `docker compose down` y a recrear el contenedor.

Comandos utiles:

```powershell
docker compose logs -f web
docker compose down
docker compose up --build -d
```

**NO ejecutar `docker compose down -v`** si deseas conservar la base y la musica.

## Preparacion para GitHub

El `.gitignore` excluye los datos privados: `spotifynet.db`, sus respaldos,
los archivos en `App_Data`, binarios y configuraciones locales. No se deben
subir MP3 comerciales ni credenciales. Revisa los archivos de imagen y sus
derechos de redistribucion antes de publicar el repositorio.

```powershell
git init
git add .
git status
```

Comprueba que `git status` **no incluya** bases `.db`, directorios `App_Data/audio`
ni archivos personales antes de hacer un commit.

## Seguridad y despliegue

Este entorno Docker de demostracion expone HTTP local para pruebas.
Antes de publicar el servicio en Internet, se requiere HTTPS mediante
proxy inverso, proteccion de secretos, limites de almacenamiento, politicas
de privacidad y pruebas de seguridad adicionales.

La configuracion `ApplyMigrationsOnStartup=true` aplica migraciones existentes
solo al arrancar con Docker. No ejecuta `EnsureCreated()` ni reinicializa
bases de datos. El proyecto de Windows conserva la configuracion previa.

## Comprobaciones del Bloque H

Revisa `PRUEBAS_BLOQUE_H.md` para un recorrido de búsqueda con dos cuentas y reproducción continua.
`PUBLICACION.md` explica cómo preparar un repositorio de GitHub y verificar Docker sin subir datos personales.
