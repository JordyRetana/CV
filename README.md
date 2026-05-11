# CV Desktop Editor

Aplicacion de escritorio WPF para importar, editar, previsualizar y exportar CVs profesionales compatibles con ATS.

## Caracteristicas

- Importacion de CV desde PDF.
- Editor por secciones: datos, experiencia, proyectos, educacion y habilidades.
- Vista previa HTML/PDF.
- Exportacion PDF con texto seleccionable y estructura ATS friendly.
- Trial local por usos: 4 exportaciones PDF para CV en espanol y 4 para CV en ingles.
- Base de licencias preparada para backend.
- Logging local.
- Splash screen.
- Empaquetado con Velopack.
- Documentacion para Supabase, licencias, updates y despliegue comercial.

## Compilar

```powershell
dotnet restore
dotnet build CVDesktopEditor.csproj
```

## Crear build Velopack

```powershell
.\scripts\build-velopack.ps1 -Version 0.2.7 -Channel stable
```

El instalador queda en:

```text
artifacts\velopack\stable\CVDesktopEditor-win-Setup.exe
```

El nombre exacto puede variar por canal; normalmente queda como `CVDesktopEditor-stable-Setup.exe`.

## Subir release a GitHub

Configura un token de GitHub en `GITHUB_TOKEN` y ejecuta:

```powershell
.\scripts\upload-velopack-github.ps1 -Version 0.2.7 -Channel stable
```

Tambien puedes publicarlo desde GitHub Actions con el workflow `Build and publish desktop release`.

## License API

El backend de licencias esta en `server/CVDesktopEditor.Api`.
La guia esta en `docs/LicenseBackendSetup.md`.

## Seguridad

No guardar credenciales reales en el repositorio. Usa variables de entorno o secretos de GitHub Actions.

Ver:

- `docs/CommercialProductPlan.md`
- `docs/ExternalSetupSteps.md`
- `docs/SupabaseSetup.md`
- `docs/LicenseApiContract.md`
- `docs/LicenseBackendSetup.md`
- `docs/GitHubReleaseSteps.md`
