# CV Desktop Editor

Source-available portfolio project by Jordy Retana. This repository is visible for technical evaluation only; it is not open source and is not licensed for reuse or resale.

Aplicacion de escritorio WPF para importar, editar, previsualizar y exportar CVs profesionales compatibles con ATS.

## Portfolio Scope

This project demonstrates a commercial-style desktop product:

- WPF/.NET desktop UI.
- PDF import and parsing.
- ATS-friendly PDF export.
- Local usage-based trial.
- Online license activation API.
- Supabase/PostgreSQL persistence.
- Render deployment configuration.
- Velopack installer generation.
- Separate admin and client build flow.

## Caracteristicas

- Importacion de CV desde PDF.
- Editor por secciones: datos, experiencia, proyectos, educacion y habilidades.
- Vista previa HTML/PDF.
- Exportacion PDF con texto seleccionable y estructura ATS friendly.
- Trial local por usos: 4 exportaciones PDF en total.
- Base de licencias preparada para backend.
- Logging local.
- Splash screen.
- Empaquetado con Velopack.
- Documentacion para Supabase, licencias, updates y despliegue comercial.

## Legal Notice

All rights reserved. This code is published only so companies and reviewers can evaluate the author's engineering work.

You may read and review the code. You may not copy, resell, redistribute, rebrand, sublicense, or use this project or substantial portions of it in another product without written permission.

See `LICENSE`, `NOTICE`, `SECURITY.md`, and `docs/PortfolioProtection.md`.

## Compilar

```powershell
dotnet restore
dotnet build CVDesktopEditor.csproj
```

## Crear build Velopack

```powershell
.\scripts\build-velopack.ps1 -Version 0.4.0 -Channel stable
```

El instalador queda en:

```text
artifacts\velopack\stable\CVDesktopEditor-win-Setup.exe
```

El nombre exacto puede variar por canal; normalmente queda como `CVDesktopEditor-stable-Setup.exe`.

## Subir release a GitHub

Configura un token de GitHub en `GITHUB_TOKEN` y ejecuta:

```powershell
.\scripts\upload-velopack-github.ps1 -Version 0.4.0 -Channel stable
```

Tambien puedes publicarlo desde GitHub Actions con el workflow `Build and publish desktop release`.

## License API

El backend de licencias esta en `server/CVDesktopEditor.Api`.
La guia esta en `docs/LicenseBackendSetup.md`.

## Seguridad

No guardar credenciales reales en el repositorio. Usa variables de entorno o secretos de GitHub Actions.

Generated installers, local CV data, local logs, build output, and environment files are excluded from git.

Ver:

- `docs/CommercialProductPlan.md`
- `docs/ExternalSetupSteps.md`
- `docs/SupabaseSetup.md`
- `docs/LicenseApiContract.md`
- `docs/LicenseBackendSetup.md`
- `docs/GitHubReleaseSteps.md`
