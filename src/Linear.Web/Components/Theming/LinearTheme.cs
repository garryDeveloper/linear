using MudBlazor;

namespace Linear.Web.Components.Theming;

/// <summary>
/// Tema visual de la aplicación: compacto, de alto contraste y con radios chicos,
/// según la estética pedida en la task 001.
/// </summary>
/// <remarks>
/// No se usan fuentes externas a propósito: una tipografía de sistema evita una
/// dependencia de red en cada carga y rinde igual en el objetivo de densidad visual.
/// </remarks>
public static class LinearTheme
{
    private static readonly string[] FontStack =
    [
        "Inter",
        "Segoe UI Variable Text",
        "Segoe UI",
        "system-ui",
        "-apple-system",
        "Helvetica Neue",
        "sans-serif"
    ];

    public static MudTheme Instance { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5B5BD6",
            Secondary = "#6E6E80",
            Background = "#FFFFFF",
            BackgroundGray = "#F6F6F7",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1C1C1F",
            DrawerBackground = "#FAFAFA",
            DrawerText = "#41414A",
            DrawerIcon = "#6E6E80",
            TextPrimary = "#1C1C1F",
            TextSecondary = "#6E6E80",
            ActionDefault = "#6E6E80",
            Divider = "#E6E6E9",
            LinesDefault = "#E6E6E9",
            TableLines = "#E6E6E9",
            Success = "#2E7D5B",
            Warning = "#B0741A",
            Error = "#C63D3D",
            Info = "#3D6FC6"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7B7BF0",
            Secondary = "#8A8F98",
            Background = "#0F0F11",
            BackgroundGray = "#151517",
            Surface = "#151517",
            AppbarBackground = "#0F0F11",
            AppbarText = "#E8E8EA",
            DrawerBackground = "#141416",
            DrawerText = "#B4B4BB",
            DrawerIcon = "#8A8F98",
            TextPrimary = "#E8E8EA",
            TextSecondary = "#8A8F98",
            ActionDefault = "#8A8F98",
            Divider = "#26262B",
            LinesDefault = "#26262B",
            TableLines = "#26262B",
            Success = "#4CB782",
            Warning = "#E0A03D",
            Error = "#E5484D",
            Info = "#6E9BF0"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = FontStack,
                FontSize = "0.8125rem",
                LineHeight = "1.5"
            },
            H1 = new H1Typography { FontFamily = FontStack, FontSize = "1.5rem", FontWeight = "600" },
            H2 = new H2Typography { FontFamily = FontStack, FontSize = "1.25rem", FontWeight = "600" },
            H3 = new H3Typography { FontFamily = FontStack, FontSize = "1.125rem", FontWeight = "600" },
            H4 = new H4Typography { FontFamily = FontStack, FontSize = "1rem", FontWeight = "600" },
            H5 = new H5Typography { FontFamily = FontStack, FontSize = "0.9375rem", FontWeight = "600" },
            H6 = new H6Typography { FontFamily = FontStack, FontSize = "0.875rem", FontWeight = "600" },
            Body1 = new Body1Typography { FontFamily = FontStack, FontSize = "0.8125rem" },
            Body2 = new Body2Typography { FontFamily = FontStack, FontSize = "0.75rem" },
            Button = new ButtonTypography { FontFamily = FontStack, FontSize = "0.8125rem", TextTransform = "none" },
            Caption = new CaptionTypography { FontFamily = FontStack, FontSize = "0.75rem" },
            Subtitle1 = new Subtitle1Typography { FontFamily = FontStack, FontSize = "0.8125rem" },
            Subtitle2 = new Subtitle2Typography { FontFamily = FontStack, FontSize = "0.75rem", FontWeight = "600" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "6px",

            // La barra lateral se angosta a propósito: cuanto menos espacio ocupe el marco,
            // más queda para el contenido que la persona vino a ver. La barra superior no
            // tiene un equivalente acá porque AppHeader ya la fija en modo Dense, que produce
            // una barra más angosta (34px) que cualquier valor que se pudiera fijar aquí.
            DrawerWidthLeft = "224px"
        }
    };
}
