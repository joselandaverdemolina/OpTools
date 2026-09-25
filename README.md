# AltaDescargaLCO

Herramienta de escritorio (WinForms, .NET 8) para dar de alta o descargar el RFC de un PAC en la tabla
de LCO y, enseguida, reiniciar la web app de Azure que consume esa lista y verificar que vuelva a responder.

## Estructura

| Proyecto | Responsabilidad |
|---|---|
| `src/AltaDescargaLCO.Domain` | `Rfc`, `RfcValidator` (estructura, fecha y dígito verificador del SAT), enums. Sin dependencias. |
| `src/AltaDescargaLCO.Infrastructure` | Modelos de configuración y cifrado DPAPI de la contraseña. |
| `src/AltaDescargaLCO.DataAccess` | `IPacRepository` / `SqlPacRepository` y armado de la cadena de conexión. |
| `src/AltaDescargaLCO.AzureIntegration` | Reinicio de la web app y sondeo HTTP. Nada más. |
| `src/AltaDescargaLCO.Application` | `PacProvisioningService`: validar → base de datos → reinicio → verificación. |
| `src/AltaDescargaLCO.App` | Formulario y composición de dependencias. |
| `tools/AltaDescargaLCO.Tools.SecretEncryptor` | Cifra la contraseña de SQL para `appsettings.json`. |
| `tests/*` | Pruebas unitarias (xUnit). |

Las capas de datos y de Azure no dependen de la interfaz, por lo que se pueden reutilizar en otras
herramientas.

## Configuración

1. Compilar: `dotnet build`.
2. Completar `src/AltaDescargaLCO.App/appsettings.json`: servidor, base de datos, el login
   restringido (`Sql:UserId` y `Sql:Password`), el nombre del procedimiento almacenado y los datos de
   Azure (suscripción, grupo de recursos, nombre de la web app y URL a verificar).

   La contraseña se guarda en texto plano. La protección no viene del archivo sino del login: sólo
   puede ejecutar un procedimiento almacenado (ver *Permisos de SQL*). Aun así, restrinja con permisos
   NTFS el directorio de instalación y **no suba este archivo al repositorio**.

   Alternativa sin contraseña: deje `Sql:UserId` vacío y la conexión usará autenticación integrada de
   Windows con la cuenta que ejecuta la herramienta.

3. Crear la tabla y el procedimiento en la base de datos:

```sql
CREATE TABLE dbo.RFCPac
(
    RFCPac VARCHAR(13) NOT NULL PRIMARY KEY,
    Estado INT         NOT NULL
);
```

   Enseguida ejecutar [`db/StoredProcedures.sql`](db/StoredProcedures.sql), que crea
   `dbo.usp_RFCPac_Movimiento`: recibe `@Accion` ('A' alta / 'B' baja) y `@RFCPac`, y devuelve en
   `@Resultado` si aplicó el movimiento (1) o si no hubo cambios (2), es decir, si el RFC ya estaba
   registrado o no se encontró.

`Estado` se escribe siempre con 1 en el alta; la descarga elimina el renglón, por lo que el 0 no
aparece en la práctica.

## Permisos de SQL

La herramienta no ejecuta SQL en línea: todo el movimiento pasa por el procedimiento almacenado. Por
eso el login restringido sólo necesita permiso para ejecutarlo, sin `SELECT`/`INSERT`/`DELETE` directo
sobre la tabla. Esto es lo que hace aceptable que la contraseña viva en texto plano: quien la obtenga
no puede consultar ni modificar nada más que este movimiento.

```sql
CREATE LOGIN svc_altadescarga WITH PASSWORD = '<contraseña>';
CREATE USER  svc_altadescarga FOR LOGIN svc_altadescarga;

GRANT EXECUTE ON dbo.usp_RFCPac_Movimiento TO svc_altadescarga;
```

Conviene confirmar que el login quedó sin más permisos de los necesarios:

```sql
EXECUTE AS USER = 'svc_altadescarga';
SELECT * FROM fn_my_permissions('dbo.RFCPac', 'OBJECT');   -- no debe devolver renglones
REVERT;
```

## Permisos de Azure

La autenticación es interactiva: al reiniciar se abre el navegador y firma el propio usuario que
ejecuta la herramienta. No hay secretos de aplicación en el código ni en la configuración, y el token
se queda sólo en memoria (no se persiste en disco ni se reutiliza entre ejecuciones).

Para que el permiso sirva únicamente para lo que se pide, asigne a los operadores un rol con alcance
**a esa sola web app**, no a la suscripción. Un rol personalizado con estas acciones es suficiente:

- `Microsoft.Web/sites/restart/action`
- `Microsoft.Web/sites/read`

## Pruebas

```bash
dotnet test
```

## Nota

La contraseña se descifra en una variable local justo antes de abrir la conexión. Las cadenas de .NET
son inmutables, así que no se pueden sobrescribir en memoria; es la misma exposición que tiene
cualquier aplicación con ADO.NET.
