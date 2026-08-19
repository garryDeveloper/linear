using Linear.Domain.Labels;

namespace Linear.Web.Features.Labels.Contracts;

/// <summary>
/// Convierte labels del dominio en su representación de salida.
/// </summary>
/// <remarks>
/// El contrato se comparte entre los cuatro slices porque todos devuelven exactamente la
/// misma forma; cuatro copias idénticas se desincronizarían al primer campo nuevo.
/// </remarks>
public static class LabelResponseMapper
{
    public static LabelResponse ToResponse(Label label)
    {
        ArgumentNullException.ThrowIfNull(label);

        return new LabelResponse(
            label.Id,
            label.Name,
            label.Description,
            label.Color.Value,
            label.Color.PrefersDarkText,
            label.CreatedAt);
    }
}
