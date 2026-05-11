# License backend setup

Este backend vive en `server/CVDesktopEditor.Api` y es el intermediario seguro entre la app de escritorio y Supabase.

## Variables

Configura estas variables en el servidor donde corra la API:

```powershell
$env:SUPABASE_CONNECTION_STRING = "postgresql://USER:PASSWORD@HOST:PORT/DATABASE"
$env:ADMIN_API_KEY = "crea-una-clave-larga-privada"
```

No subas estas variables al repo.

## Inicializar tablas en Supabase

1. Ejecuta la API:

```powershell
dotnet run --project .\server\CVDesktopEditor.Api\CVDesktopEditor.Api.csproj
```

2. En otra terminal, inicializa la base:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5282/admin/database/initialize" `
  -Headers @{ "X-Admin-Key" = $env:ADMIN_API_KEY }
```

## Crear una licencia

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:5282/admin/licenses" `
  -Headers @{ "X-Admin-Key" = $env:ADMIN_API_KEY } `
  -ContentType "application/json" `
  -Body '{
    "email": "cliente@example.com",
    "fullName": "Cliente",
    "kind": "premium",
    "maxDevices": 1,
    "expiresAt": "2027-01-01T00:00:00Z"
  }'
```

La respuesta devuelve `licenseKey` una sola vez. Esa es la clave que se pega en la ventana **Activar licencia** de la app.

## Activar desde la app

En la pantalla principal abre **Activar licencia**, coloca:

- URL de API: `http://localhost:5282` durante pruebas.
- Clave de licencia: la clave generada por `/admin/licenses`.

Para produccion, publica esta API en un servidor real y usa HTTPS.

## Seguridad real

Este backend ya evita que la app tenga credenciales directas de Supabase. Para produccion, falta:

- hospedar la API con HTTPS;
- mover `ADMIN_API_KEY` y `SUPABASE_CONNECTION_STRING` a secretos del hosting;
- agregar rate limiting;
- firmar tokens de activacion con una llave privada;
- registrar builds firmados;
- rotar cualquier token o password que se haya pegado en chats o logs.
