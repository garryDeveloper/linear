namespace Linear.Web.Features.Issues.Filtering;

/// <summary>
/// Operadores de comparación de un filtro.
/// </summary>
/// <remarks>
/// <see cref="Is"/>/<see cref="In"/> y <see cref="IsNot"/>/<see cref="NotIn"/> son en el
/// fondo el mismo par —incluir y excluir— y solo se distinguen por la cantidad de valores:
/// "is X" es "in [X]". Se modelan por separado igual, porque es el vocabulario con el que
/// la task los pide y con el que la interfaz los muestra; la traducción a SQL, en cambio,
/// solo mira <see cref="FilterOperatorExtensions.IsNegated"/>.
/// </remarks>
public enum FilterOperator
{
    Is = 0,
    IsNot = 1,
    In = 2,
    NotIn = 3,

    /// <summary>Coincidencia parcial de texto, sin distinguir mayúsculas.</summary>
    Contains = 4
}

public static class FilterOperatorExtensions
{
    /// <summary>Indica si el operador excluye en lugar de incluir.</summary>
    public static bool IsNegated(this FilterOperator op) =>
        op is FilterOperator.IsNot or FilterOperator.NotIn;

    /// <summary>Indica si el operador admite varios valores.</summary>
    public static bool AcceptsManyValues(this FilterOperator op) =>
        op is FilterOperator.In or FilterOperator.NotIn;
}
