# Contrato inicial de API de licencias

Este contrato define como debe hablar la app WPF con el backend futuro de licencias.

## Activar licencia

`POST /licenses/activate`

Request:

```json
{
  "licenseKey": "XXXX-XXXX-XXXX-XXXX",
  "deviceId": "SHA256_DEVICE_ID",
  "appVersion": "0.2.0",
  "channel": "stable"
}
```

Response:

```json
{
  "status": "active",
  "licenseId": "lic_123",
  "userId": "usr_123",
  "expiresUtc": "2026-05-12T00:00:00Z",
  "features": ["export_pdf", "ats_templates", "auto_updates"],
  "signedToken": "SERVER_SIGNED_LICENSE_TOKEN"
}
```

## Validar licencia

`POST /licenses/validate`

Request:

```json
{
  "signedToken": "SERVER_SIGNED_LICENSE_TOKEN",
  "deviceId": "SHA256_DEVICE_ID",
  "appVersion": "0.2.0"
}
```

Response:

```json
{
  "status": "active",
  "expiresUtc": "2026-05-12T00:00:00Z",
  "serverTimeUtc": "2026-05-11T12:00:00Z",
  "message": "License active"
}
```

Estados posibles:

- `active`
- `trial`
- `expired`
- `revoked`
- `device_limit_reached`
- `tampered`

## Crear trial

`POST /licenses/trial`

Request:

```json
{
  "email": "user@example.com",
  "deviceId": "SHA256_DEVICE_ID",
  "appVersion": "0.2.0"
}
```

Response:

```json
{
  "status": "trial",
  "expiresUtc": "2026-05-12T12:00:00Z",
  "signedToken": "SERVER_SIGNED_TRIAL_TOKEN"
}
```

## Seguridad obligatoria

- HTTPS obligatorio.
- Rate limiting por IP/dispositivo/email.
- Audit log por cada activacion.
- Token firmado con clave privada del servidor.
- La app solo embebe clave publica.
- No guardar passwords ni secretos en cliente.
