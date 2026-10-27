# SpotifyNet: preparación para GitHub y Docker

## 1. Qué se puede publicar

Este proyecto es una aplicación académica independiente, no Spotify oficial. No publiques datos personales, correos de tus cuentas de prueba, bases SQLite, MP3 comerciales ni secretos. Revisa los derechos de uso de las portadas presentes en `wwwroot/images/referencias` antes de hacer público un repositorio: el `.gitignore` no oculta esas imágenes.

## 2. GitHub (después de probar la actualización)

Abre PowerShell desde la carpeta `SpotifyNet` y ejecuta:

```powershell
git init
git add .
git status
```

Comprueba visualmente que NO aparezcan archivos `.db`, `App_Data/audio`, `App_Data/covers`, `bin` ni `obj`. Si aparece cualquiera, no continúes hasta ajustar `.gitignore`.

```powershell
git commit -m "SpotifyNet: biblioteca, perfiles y busqueda unificada"
```

Crea un repositorio vacío en GitHub con el nombre que prefieras y, únicamente después, enlaza la URL auténtica que GitHub te muestre:

```powershell
git branch -M main
git remote add origin URL_REAL_DE_TU_REPOSITORIO
git push -u origin main
```

Si aún no configuraste Git, `git commit` puede pedir `user.name` y `user.email`. No pegues contraseñas ni tokens en el código fuente.

## 3. Docker local

Necesitas Docker Desktop ejecutándose con contenedores Linux. Apaga `dotnet run` primero para liberar el puerto 5267.

```powershell
docker compose up --build -d
docker compose ps
docker compose logs --tail=100 web
```

Abre `http://localhost:5267`. Este contenedor tiene su propia base de datos y se inicia con cuentas y canciones vacías, **no copia** la biblioteca de tu PC.

Los volúmenes `spotifynet_sqlite`, `spotifynet_audio` y `spotifynet_covers` guardan la base, MP3 y portadas respectivamente. Apagar sin borrarlos:

```powershell
docker compose down
```

No ejecutes `docker compose down -v` si quieres mantener esos datos.

## 4. Antes de subir a un servidor público

- Configurar HTTPS real, cookies seguras y autenticación.
- Establecer límites de tamaño y almacenamiento para archivos de audio y portada.
- Gestionar secretos por variables de entorno, no en el repositorio.
- Revisar permisos sobre MP3, playlists compartidas y accesos de colaboradores.
- Separar copias de seguridad de la base y de los volúmenes de audio/portadas.
- Confirmar que tienes permiso para alojar o distribuir cada archivo multimedia.

**Estado:** configuración preparada para pruebas locales. No significa que Docker se haya ejecutado ni que GitHub ya tenga tu código.
