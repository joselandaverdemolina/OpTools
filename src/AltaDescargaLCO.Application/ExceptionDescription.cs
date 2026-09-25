namespace AltaDescargaLCO.Application;

internal static class ExceptionDescription
{
    /// <summary>Encadena el mensaje de la excepción con los de sus internas.</summary>
    public static string Describe(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add($"{current.GetType().Name}: {current.Message}");
        }

        return string.Join(" -> ", messages);
    }
}
