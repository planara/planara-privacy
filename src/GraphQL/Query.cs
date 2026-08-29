using System.Security.Claims;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Planara.Common.Auth.Claims;
using Planara.Common.Enums;
using Planara.Privacy.Data;
using Planara.Privacy.Data.Domain;
using Planara.Privacy.Data.Enums;

namespace Planara.Privacy.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public class Query
{
    /// <summary>
    /// Получение текущей действующей версии согласия указанного типа
    /// </summary>
    [GraphQLDescription("Получение текущую действующей версии согласия указанного типа")]
    public async Task<ConsentVersion?> GetCurrentConsentVersion(
        [GraphQLDescription("Тип согласия")]
        ConsentType type,
        [Service] DataContext dataContext,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await dataContext.ConsentVersions
            .AsNoTracking()
            .Where(x =>
                x.Type == type &&
                x.Status == ConsentVersionStatus.Published &&
                x.EffectiveAt <= now)
            .OrderByDescending(x => x.EffectiveAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
    
    [Authorize]
    public async Task<IQueryable<UserConsent>> GetMyConsents(
        ClaimsPrincipal claimsPrincipal,
        [Service] DataContext dataContext)
    {
        var userId = claimsPrincipal.GetUserId();

        return dataContext.UserConsents
            .AsNoTracking()
            .Include(x => x.ConsentVersion)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.GivenAt);
    }
}