using System.Security.Claims;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Planara.Common.Auth.Claims;
using Planara.Common.Enums;
using Planara.Privacy.Data;
using Planara.Privacy.Data.Enums;
using Planara.Privacy.Responses;

namespace Planara.Privacy.GraphQL;

[ExtendObjectType(OperationTypeNames.Query)]
public class Query
{
    /// <summary>
    /// Получение текущей действующей версии согласия указанного типа
    /// </summary>
    [GraphQLDescription("Получение текущей действующей версии согласия указанного типа")]
    public async Task<ConsentVersionResponse?> GetCurrentConsentVersion(
        [GraphQLDescription("Тип согласия")]
        ConsentType type,
        [Service] DataContext dataContext,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await dataContext.ConsentVersions
            .AsNoTracking()
            .Where(x => x.Type == type && x.Status == ConsentVersionStatus.Published && x.EffectiveAt <= now)
            .OrderByDescending(x => x.EffectiveAt)
            .Select(x => new ConsentVersionResponse
            {
                Id = x.Id,
                Type = x.Type,
                Version = x.Version,
                Title = x.Title,
                Content = x.Content,
                HtmlContent = x.HtmlContent,
                Status = x.Status,
                EffectiveAt = x.EffectiveAt,
                PublishedAt = x.PublishedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Получение опубликованных версий документов согласия
    /// </summary>
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    [GraphQLDescription("Получение опубликованных версий документов согласия")]
    public IQueryable<ConsentVersionResponse> GetConsentVersions(
        [Service] DataContext dataContext)
    {
        return dataContext.ConsentVersions
            .AsNoTracking()
            .Where(x => x.Status != ConsentVersionStatus.Draft)
            .Select(x => new ConsentVersionResponse
            {
                Id = x.Id,
                Type = x.Type,
                Version = x.Version,
                Title = x.Title,
                Content = x.Content,
                HtmlContent = x.HtmlContent,
                Status = x.Status,
                EffectiveAt = x.EffectiveAt,
                PublishedAt = x.PublishedAt
            });
    }

    /// <summary>
    /// Получение истории согласий текущего пользователя
    /// </summary>
    [Authorize]
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    [GraphQLDescription("Получение истории согласий текущего пользователя")]
    public IQueryable<UserConsentResponse> GetMyConsents(
        ClaimsPrincipal claimsPrincipal,
        [Service] DataContext dataContext)
    {
        var userId = claimsPrincipal.GetUserId();

        return dataContext.UserConsents
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new UserConsentResponse
            {
                Id = x.Id,
                Type = x.ConsentVersion.Type,
                ConsentVersionId = x.ConsentVersionId,
                Version = x.ConsentVersion.Version,
                Title = x.ConsentVersion.Title,
                GivenAt = x.GivenAt,
                RevokedAt = x.RevokedAt
            });
    }
}