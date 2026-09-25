using System.Data;
using AltaDescargaLCO.Domain;
using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.DataAccess;

public sealed class SqlPacRepository : IPacRepository
{
    private const int ResultadoAplicado = 1;

    private readonly ISqlConnectionStringFactory _connectionStrings;
    private readonly SqlSettings _settings;

    public SqlPacRepository(ISqlConnectionStringFactory connectionStrings, IOptions<SqlSettings> settings)
    {
        _connectionStrings = connectionStrings;
        _settings = settings.Value;
    }

    public async Task<PacChangeOutcome> ApplyAsync(
        PacAction action,
        Rfc rfc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionStrings.Build());
        await using var command = new SqlCommand(_settings.StoredProcedureName, connection)
        {
            CommandType = CommandType.StoredProcedure
        };

        command.Parameters.Add("@Accion", SqlDbType.Char, 1).Value = action == PacAction.Add ? "A" : "B";
        command.Parameters.Add("@RFCPac", SqlDbType.VarChar, 13).Value = rfc.Value;
        var resultado = command.Parameters.Add("@Resultado", SqlDbType.Int);
        resultado.Direction = ParameterDirection.Output;

        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);

        return (int)resultado.Value == ResultadoAplicado
            ? PacChangeOutcome.Applied
            : PacChangeOutcome.NoChangeNeeded;
    }
}
