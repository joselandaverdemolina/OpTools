using AltaDescargaLCO.DataAccess;
using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace AltaDescargaLCO.DataAccess.Tests;

public class SqlConnectionStringFactoryTests
{
    [Fact]
    public void Uses_the_restricted_sql_login_when_a_user_is_configured()
    {
        var connectionString = Build(new SqlSettings
        {
            Server = "SQLSRV01",
            Database = "LCODb",
            UserId = "svc_altadescarga",
            Password = "s3cr3t"
        });

        var parsed = new SqlConnectionStringBuilder(connectionString);
        Assert.False(parsed.IntegratedSecurity);
        Assert.Equal("svc_altadescarga", parsed.UserID);
        Assert.Equal("s3cr3t", parsed.Password);
        Assert.Equal("SQLSRV01", parsed.DataSource);
        Assert.Equal("LCODb", parsed.InitialCatalog);
    }

    [Fact]
    public void Falls_back_to_integrated_security_when_no_user_is_configured()
    {
        var parsed = new SqlConnectionStringBuilder(
            Build(new SqlSettings { Server = "SQLSRV01", Database = "LCODb" }));

        Assert.True(parsed.IntegratedSecurity);
        Assert.Empty(parsed.UserID);
        Assert.Empty(parsed.Password);
    }

    [Fact]
    public void Explains_a_user_without_a_password()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Build(new SqlSettings
        {
            Server = "SQLSRV01",
            Database = "LCODb",
            UserId = "svc_altadescarga"
        }));

        Assert.Contains("Sql:Password", ex.Message);
    }

    [Theory]
    [InlineData("", "LCODb")]
    [InlineData("SQLSRV01", "")]
    public void Explains_missing_server_or_database(string server, string database)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => Build(new SqlSettings { Server = server, Database = database }));

        Assert.Contains("obligatorias", ex.Message);
    }

    private static string Build(SqlSettings settings) =>
        new SqlConnectionStringFactory(Options.Create(settings)).Build();
}
