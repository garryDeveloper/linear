namespace Linear.Web.Features.Labels.Contracts;

/// <summary>
/// Una label tal como la consume la interfaz.
/// </summary>
/// <param name="Color">Color de fondo en hexadecimal, <c>#RRGGBB</c>.</param>
/// <param name="PrefersDarkText">
/// Indica si sobre ese fondo el texto tiene que ir en oscuro. Se resuelve en el servidor
/// para que cada lugar que dibuje una label no repita el cálculo de contraste.
/// </param>
public sealed record LabelResponse(
    Guid Id,
    string Name,
    string? Description,
    string Color,
    bool PrefersDarkText,
    DateTimeOffset CreatedAt);
