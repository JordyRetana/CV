# GitHub release steps

## Opcion recomendada: GitHub Actions

1. Sube los cambios a `main`.
2. En GitHub abre el repo.
3. Entra a **Actions**.
4. Selecciona **Build and publish desktop release**.
5. Presiona **Run workflow**.
6. Escribe la version, por ejemplo `0.2.2`.
7. Elige `stable` o `beta`.

El workflow compila la app, genera el instalador Velopack y publica la release usando el token interno seguro del repo.

## Opcion local

Si quieres publicar desde tu PC:

```powershell
$env:GITHUB_TOKEN = "tu-token"
.\scripts\upload-velopack-github.ps1 -Version 0.2.1 -Channel stable
```

No guardes ese token en archivos. Cuando termines, borralo de la sesion:

```powershell
Remove-Item Env:\GITHUB_TOKEN
```

## Importante

Si un token fue compartido por chat, consideralo comprometido y revocalo desde GitHub despues de usarlo.
