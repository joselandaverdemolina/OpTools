using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.DataAccess;

public sealed class SqlConnectionStringFactory : ISqlConnectionStringFactory
{
    private readonly SqlSettings _settings;

    public SqlConnectionStringFactory(IOptions<SqlSettings> settings)
    {
        _settings = settings.Value;
    }

    public string Build()
    {
        if (string.IsNullOrWhiteSpace(_settings.Server) || string.IsNullOrWhiteSpace(_settings.Database))
        {
            throw new InvalidOperationException(
                "Las configuraciones Sql:Server y Sql:Database son obligatorias en appsettings.json.");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = _settings.Server,
            InitialCatalog = _settings.Database,
            TrustServerCertificate = _settings.TrustServerCertificate,
            ConnectTimeout = _settings.ConnectTimeoutSeconds,
            ApplicationName = "AltaDescargaLCO"
        };

        if (_settings.UsesIntegratedSecurity)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            if (string.IsNullOrEmpty(_settings.Password))
            {
                throw new InvalidOperationException(
                    "La configuración Sql:Password es obligatoria cuando se especifica Sql:UserId. "
                    + "Deje Sql:UserId vacío para usar autenticación integrada de Windows.");
            }

            builder.UserID = _settings.UserId;
            builder.Password = _settings.Password;
        }

        return builder.ConnectionString;
    }
}
