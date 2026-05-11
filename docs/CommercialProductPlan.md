# CV Desktop Editor - Plan profesional de producto

Este documento define la ruta para convertir el proyecto en una aplicacion de escritorio comercial, instalable, mantenible y preparada para licencias, actualizaciones y exportacion de CVs ATS friendly.

## 1. Enfoque recomendado

La base actual es WPF + .NET 8. Para este proyecto conviene mantener WPF en lugar de migrar a Electron:

- Menor peso de instalacion.
- Mejor integracion con Windows.
- Menos superficie de ataque que una app Electron.
- Mejor rendimiento para editar datos locales y exportar PDFs.
- Distribucion profesional posible con MSIX, Velopack o instalador tradicional.

Arquitectura recomendada:

- `CVDesktopEditor` como cliente desktop WPF.
- `CVDesktopEditor.Core` para modelos, reglas de negocio, exportacion ATS, licencias y validaciones.
- `CVDesktopEditor.Infrastructure` para storage local, logs, update client, license client y PDF import/export.
- `CVDesktopEditor.Admin` como panel web futuro para licencias, usuarios, builds y estado.
- `CVDesktopEditor.Api` como backend seguro para activaciones, licencias y releases.

## 2. Exportacion PDF ATS friendly

El PDF para ATS debe priorizar texto real, seleccionable y simple. La regla principal: buen HTML/documento de origen, luego impresion/exportacion sin convertir a imagen.

Buenas practicas implementadas o recomendadas:

- Texto seleccionable, no imagen.
- Sin columnas complejas para datos esenciales.
- Sin tablas para layout.
- Encabezados estandar: `PROFESSIONAL SUMMARY`, `PROFESSIONAL EXPERIENCE`, `PROJECTS`, `EDUCATION`, `SKILLS`.
- Secciones con nombres detectables por Workday, Greenhouse, Lever y parsers similares.
- Fuente sans-serif comun y legible: Arial/Segoe UI/Calibri.
- Links y correo como texto plano.
- Bullets normales, no iconos exoticos.
- Margenes A4/Letter consistentes.
- Exportacion por WebView2 `PrintToPdfAsync`, manteniendo texto real.

Mejoras futuras:

- Selector de plantilla ATS: `Harvard`, `Modern ATS`, `Compact ATS`.
- Analizador de score ATS local: detectar emails, telefono, links, verbos de accion, skills y secciones faltantes.
- Exportar tambien `.docx`, que muchos ATS procesan muy bien.

## 3. Instalador, versiones y auto updates

Opciones reales:

- MSIX: instalador moderno de Microsoft, buen aislamiento, requiere firma. Ideal si se distribuye por Microsoft Store o canales corporativos.
- Velopack: instalador + auto updater para apps desktop, muy practico para WPF. Puede usar GitHub Releases, S3, servidor propio o carpetas.
- Inno Setup/Wix: instaladores tradicionales, mas control, menos automatico para updates.

Recomendacion:

- Usar Velopack para la primera version comercial con canal `stable` y `beta`.
- Firmar el `.exe` y el instalador con certificado de codigo.
- Publicar releases en GitHub Releases privado/publico o servidor propio.

Flujo de version:

1. Actualizar `Version` en el `.csproj`.
2. Ejecutar publish release.
3. Empaquetar con Velopack.
4. Firmar instalador y ejecutables.
5. Subir release al canal correspondiente.
6. La app detecta update, descarga y aplica en proximo reinicio.

## 4. Licencias y trials

Un sistema serio de licencias no debe depender solo del cliente desktop. El cliente siempre puede ser manipulado. La autoridad debe estar en un backend.

Modelo recomendado:

- Backend API con HTTPS.
- Base de datos: PostgreSQL.
- Tablas: `Users`, `Licenses`, `Devices`, `Activations`, `Builds`, `AuditLogs`.
- Licencias firmadas con clave privada del servidor.
- Cliente valida firma con clave publica embebida.
- Activacion por dispositivo con machine fingerprint no invasivo.
- Trial de 1 dia emitido por servidor, no calculado solo localmente.
- Cache local cifrada con Windows DPAPI para permitir uso offline limitado.

Estados:

- `trial_active`
- `trial_expired`
- `licensed_active`
- `licensed_expired`
- `revoked`
- `tampered`

Flujo de trial:

1. Usuario solicita trial desde la app o panel.
2. Backend emite licencia trial por 24 horas.
3. App guarda token firmado localmente.
4. En cada inicio valida firma, fecha, dispositivo y estado remoto cuando haya internet.
5. Si vence, bloquea funciones premium/exportacion.

Panel admin:

- Generar claves.
- Activar/desactivar licencias.
- Definir duracion.
- Ver dispositivos vinculados.
- Revocar usuarios.
- Ver auditoria de activaciones.
- Generar builds por canal: trial, premium, beta.

## 5. Seguridad y proteccion de codigo

No existe proteccion perfecta para apps desktop. El objetivo profesional es subir el costo de copia/modificacion y mover secretos al servidor.

Medidas reales:

- No guardar secretos privados en la app.
- No incluir claves privadas en el ejecutable.
- Usar backend para emitir licencias y tokens.
- Firmar licencias con criptografia asimetrica.
- Validar integridad de archivos criticos.
- Firmar ejecutable e instalador.
- Ofuscar assemblies .NET antes de distribuir.
- Publicar self-contained y trimmed solo si no rompe WPF/WebView2.
- Guardar configuracion sensible con DPAPI.
- Logs sin datos sensibles.
- TLS obligatorio contra backend.
- Rate limiting y auditoria en API.

Herramientas posibles:

- Obfuscator: Dotfuscator, Eazfuscator.NET, Babel Obfuscator u otra opcion comercial.
- Firma: certificado de Code Signing o Microsoft Trusted Signing/Azure Artifact Signing.
- Empaque: MSIX o Velopack.

## 6. Backend/API segura

Backend recomendado:

- ASP.NET Core Web API.
- PostgreSQL.
- Entity Framework Core.
- JWT para admin.
- API keys internas para build/release automation.
- Refresh tokens rotativos para panel admin.
- Audit log obligatorio.

Endpoints iniciales:

- `POST /licenses/activate`
- `POST /licenses/validate`
- `POST /licenses/deactivate`
- `GET /updates/check`
- `POST /admin/licenses`
- `PATCH /admin/licenses/{id}/revoke`
- `GET /admin/users`
- `GET /admin/audit`

## 7. Builds

Build local:

```powershell
dotnet restore
dotnet build CVDesktopEditor.csproj -c Release
dotnet publish CVDesktopEditor.csproj -c Release -r win-x64 --self-contained true
```

Build comercial recomendado:

1. Clean.
2. Restore.
3. Build Release.
4. Tests.
5. Publish self-contained.
6. Obfuscate.
7. Sign.
8. Package installer.
9. Sign installer.
10. Upload release.

## 8. Fuentes de referencia

- Velopack docs: https://docs.velopack.io/
- Velopack WPF quick start: https://docs.velopack.io/getting-started/wpf
- Microsoft MSIX packaging: https://learn.microsoft.com/en-ie/windows/msix/desktop/vs-package-overview
- Microsoft code signing: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options
- Microsoft MSIX signing: https://learn.microsoft.com/en-us/windows/msix/package/signing-package-overview
