using AltaDescargaLCO.Application;
using AltaDescargaLCO.AzureIntegration;
using AltaDescargaLCO.DataAccess;
using AltaDescargaLCO.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AltaDescargaLCO.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            using var provider = BuildServiceProvider();
            System.Windows.Forms.Application.Run(provider.GetRequiredService<MainForm>());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo iniciar la aplicación.{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Alta / Baja de RFC PAC",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var services = new ServiceCollection();
        services.Configure<SqlSettings>(configuration.GetSection(SqlSettings.SectionName));
        services.Configure<AzureSettings>(configuration.GetSection(AzureSettings.SectionName));

        // Everything the pipeline touches is scoped, so each Execute click gets a fresh graph -
        // including a fresh Azure credential that dies with the scope.
        services.AddScoped<ISqlConnectionStringFactory, SqlConnectionStringFactory>();
        services.AddScoped<IPacRepository, SqlPacRepository>();
        services.AddScoped<IWebAppLifecycleService, AzureWebAppLifecycleService>();
        services.AddScoped<IPacProvisioningService, PacProvisioningService>();
        services.AddHttpClient<IWebAppHealthProbe, HttpWebAppHealthProbe>();

        services.AddSingleton<MainForm>();

        return services.BuildServiceProvider();
    }
}
