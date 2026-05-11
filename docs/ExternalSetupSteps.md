# Pasos externos para terminar el producto comercial

Estas tareas no se pueden completar solo desde esta conversacion porque requieren cuentas, certificados, servidores o decisiones comerciales. Estan ordenadas por prioridad.

## 1. Elegir publicacion e instalador

Recomendado para empezar: Velopack.

1. Crea una cuenta/repositorio donde vas a publicar releases.
2. Decide canales:
   - `stable`: publico normal.
   - `beta`: pruebas.
3. Instala Velopack CLI en tu maquina de build.
4. Publica la app con:

```powershell
.\scripts\publish-release.ps1
```

5. Empaqueta la carpeta `artifacts\publish` con Velopack.
6. Sube los paquetes a GitHub Releases o servidor propio.

Comandos preparados en este repo:

```powershell
.\scripts\build-velopack.ps1 -Version 0.2.1 -Channel stable
```

Para subir a GitHub Releases:

```powershell
$env:GITHUB_TOKEN = "TU_TOKEN_DE_GITHUB"
.\scripts\upload-velopack-github.ps1 -Version 0.2.1 -Channel stable
```

El token debe tener permiso de `Contents: Read and write` sobre el repo.

Alternativa Microsoft: MSIX si quieres instalador mas corporativo o Microsoft Store.

## 2. Certificado de firma digital

Para que Windows confie en el instalador:

1. Comprar o configurar un certificado Code Signing.
2. Opciones:
   - Certificado OV/EV de proveedor comercial.
   - Microsoft Trusted Signing si tienes acceso a Azure.
3. Firmar:
   - `.exe`
   - `.dll` si aplica
   - instalador
4. Verificar firma antes de publicar.

Sin firma, Windows SmartScreen puede mostrar advertencias.

## 3. Backend de licencias

Crear proyecto recomendado:

```text
CVDesktopEditor.Api
CVDesktopEditor.Admin
CVDesktopEditor.Core
```

Stack recomendado:

- ASP.NET Core Web API.
- PostgreSQL.
- Entity Framework Core.
- JWT para admin.
- Licencias firmadas con clave privada del servidor.

Pasos:

1. Crear base de datos PostgreSQL.
2. Crear tablas:
   - `Users`
   - `Licenses`
   - `Devices`
   - `Activations`
   - `Builds`
   - `AuditLogs`
3. Crear endpoints:
   - `POST /licenses/activate`
   - `POST /licenses/validate`
   - `POST /licenses/deactivate`
   - `GET /updates/check`
4. Crear panel admin:
   - generar licencias
   - activar/desactivar usuarios
   - definir duracion
   - ver dispositivos activos
   - revocar licencia

Importante: la clave privada nunca debe estar dentro de la app WPF.

## 4. Trial de 1 dia

La app ya tiene una base local de trial. Para hacerlo comercial:

1. El usuario solicita trial.
2. Backend crea licencia trial por 24 horas.
3. Backend firma licencia.
4. App guarda licencia firmada localmente.
5. App valida:
   - firma
   - fecha de vencimiento
   - dispositivo
   - estado remoto cuando haya internet
6. Si vence, bloquear exportacion premium y funciones comerciales.

## 5. Auto updates

La app ya tiene un `UpdateService` preparado para leer un manifiesto.

Manifiesto ejemplo:

```json
{
  "version": "0.3.0",
  "channel": "stable",
  "releaseNotesUrl": "https://example.com/releases/0.3.0",
  "installerUrl": "https://example.com/downloads/CVDesktopEditor-0.3.0.exe",
  "sha256": "HASH_DEL_INSTALADOR"
}
```

Siguiente paso real:

1. Elegir Velopack o updater propio.
2. Si usas Velopack, reemplazar el `UpdateService` por el flujo oficial de Velopack.
3. Si usas servidor propio, completar:
   - descarga
   - validacion SHA-256
   - firma
   - instalacion silenciosa o reinicio.

## 6. Obfuscacion y proteccion

Pasos reales:

1. Publicar Release.
2. Pasar assemblies por obfuscador comercial.
3. Firmar despues de obfuscar.
4. No meter secretos en el cliente.
5. Validar licencia contra backend.
6. Usar logs y auditoria.

Herramientas:

- Eazfuscator.NET.
- Dotfuscator.
- Babel Obfuscator.

Objetivo realista: dificultar copia y modificacion, no hacerla imposible.

## 7. Checklist antes de vender

- App compila en Release.
- Instalador firmado.
- Auto updater probado en canal beta.
- Logs activos sin datos sensibles.
- Licencia validada contra servidor.
- Trial vence correctamente.
- PDF ATS probado en Workday/Greenhouse/Lever o parsers similares.
- Politica de privacidad.
- Terminos de licencia.
- Backup de base de datos.
- Versionado semantico.
