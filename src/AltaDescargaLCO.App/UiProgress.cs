namespace AltaDescargaLCO.App;

/// <summary>
/// Reporta el progreso de forma síncrona. Progress&lt;T&gt; lo publica en la cola del hilo de
/// interfaz, lo que hace que un proceso que termina sin ceder el hilo escriba su línea de resumen
/// antes que sus líneas de etapa.
/// </summary>
internal sealed class UiProgress<T> : IProgress<T>
{
    private readonly Control _owner;
    private readonly Action<T> _handler;

    public UiProgress(Control owner, Action<T> handler)
    {
        _owner = owner;
        _handler = handler;
    }

    public void Report(T value)
    {
        if (_owner.InvokeRequired)
        {
            _owner.Invoke(_handler, value);
        }
        else
        {
            _handler(value);
        }
    }
}
