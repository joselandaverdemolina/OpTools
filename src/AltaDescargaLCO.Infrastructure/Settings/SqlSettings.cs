namespace AltaDescargaLCO.Infrastructure.Settings;

public sealed class SqlSettings
{
    public const string SectionName = "Sql";

    public string Server { get; set; } = string.Empty;

    public string Database { get; set; } = string.Empty;

      public string UserId { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool TrustServerCertificate { get; set; } = true;

    public int ConnectTimeoutSeconds { get; set; } = 15;

     public string StoredProcedureName { get; set; } = "dbo.usp_RFCPac_Movimiento";

    public bool UsesIntegratedSecurity => string.IsNullOrWhiteSpace(UserId);
}
