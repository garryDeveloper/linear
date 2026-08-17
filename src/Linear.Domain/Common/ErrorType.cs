namespace Linear.Domain.Common;

/// <summary>
/// Clasifica un <see cref="Error"/> según su naturaleza.
/// La capa de transporte usa esta clasificación para elegir el status HTTP,
/// de forma que el dominio nunca necesite conocer códigos HTTP.
/// </summary>
public enum ErrorType
{
    /// <summary>Fallo inesperado o no clasificado.</summary>
    Failure = 0,

    /// <summary>La entrada no cumple las reglas de validación.</summary>
    Validation = 1,

    /// <summary>El recurso solicitado no existe o no es visible para el usuario.</summary>
    NotFound = 2,

    /// <summary>La operación choca con el estado actual del recurso.</summary>
    Conflict = 3,

    /// <summary>El usuario no está autenticado.</summary>
    Unauthorized = 4,

    /// <summary>El usuario está autenticado pero no tiene permiso.</summary>
    Forbidden = 5
}
