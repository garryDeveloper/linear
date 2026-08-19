using Linear.Domain.Common;
using Linear.Domain.Users;
using Linear.Web.Infrastructure.Authentication;
using Linear.Web.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Linear.Web.Features.Authentication.GetCurrentUser;

public sealed class GetCurrentUserHandler(IDbContextFactory<AppDbContext> dbContextFactory, ICurrentUser currentUser)
{
    public async Task<Result<CurrentUserResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var userId = await currentUser.RequireIdAsync(cancellationToken);

        if (userId.IsFailure)
        {
            return Result.Failure<CurrentUserResponse>(userId.Error);
        }

        // Se materializa la entidad en lugar de proyectar en la consulta: Email es un value
        // object mapeado con un conversor, y leer su contenido dentro de un Select no es
        // traducible a SQL. Es una sola fila por clave primaria.
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == userId.Value, cancellationToken);

        return user is null
            ? Result.Failure<CurrentUserResponse>(UserErrors.NotFound(userId.Value))
            : Result.Success(new CurrentUserResponse(
                user.Id,
                user.Email.Value,
                user.Name,
                user.AvatarUrl,
                user.Role.ToString(),
                user.CreatedAt));
    }
}
