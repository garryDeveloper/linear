namespace Linear.Domain.Common;

/// <summary>
/// Describe por qué falló una operación.
/// </summary>
/// <param name="Code">
/// Identificador estable y legible por máquina, en formato <c>Recurso.Motivo</c>
/// (por ejemplo <c>Team.KeyAlreadyExists</c>). La UI puede usarlo para traducir.
/// </param>
/// <param name="Description">Mensaje legible por una persona.</param>
/// <param name="Type">Naturaleza del error.</param>
public sealed record Error(string Code, string Description, ErrorType Type)
{
    /// <summary>Ausencia de error. Es el <see cref="Error"/> de todo resultado exitoso.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public override string ToString() => $"{Code}: {Description}";
}
