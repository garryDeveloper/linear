namespace Linear.Web.Components.Theming;

/// <summary>
/// Estado compartido del tema durante la sesión del usuario.
/// </summary>
/// <remarks>
/// Se registra como <c>Scoped</c>, que en Blazor Server equivale al circuito: el modo
/// elegido vale para todas las pestañas de esa sesión y no se filtra entre usuarios.
/// La preferencia todavía no se persiste; al reconectar se vuelve a tomar la del sistema
/// operativo.
/// </remarks>
public sealed class ThemeState
{
    public bool IsDarkMode { get; private set; } = true;

    /// <summary>Se dispara cuando cambia el modo, para que los componentes suscritos re-rendericen.</summary>
    public event Action? Changed;

    public void SetDarkMode(bool isDarkMode)
    {
        if (IsDarkMode == isDarkMode)
        {
            return;
        }

        IsDarkMode = isDarkMode;
        Changed?.Invoke();
    }

    public void Toggle() => SetDarkMode(!IsDarkMode);
}
