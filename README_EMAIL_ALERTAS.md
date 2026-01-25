# Alertas por correo (nuevo)

Este ZIP ya trae el sistema para **enviar un correo al dueño cuando entra un pedido**.

## Destino
- Por defecto, el destinatario configurado es: **saint.hubcr@gmail.com**

## Importante (para que SÍ envíe)
Para que realmente envíe correos, tenés que configurar SMTP (por ejemplo Gmail) en *variables de entorno / secretos* del hosting.

En el código está hecho para que **NO se caiga** si no hay configuración: si falta Host/User/Password, simplemente **no envía**.

### Ejemplo con Gmail
En secretos/variables de entorno:
- `Email__Host = smtp.gmail.com`
- `Email__Port = 587`
- `Email__User = saint.hubcr@gmail.com`
- `Email__Password = <APP_PASSWORD_DE_GMAIL>`
- `Email__EnableSsl = true`

> Recomendado: usar **App Password** (cuenta con 2FA).

## Activar / Desactivar
En `appsettings.json`:
- `OrderAlerts:Enabled` (true/false)
- `OrderAlerts:Recipients` (lista de correos)
