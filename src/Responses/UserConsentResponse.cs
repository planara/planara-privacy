using HotChocolate;
using Planara.Common.Enums;

namespace Planara.Privacy.Responses;

/// <summary>
/// Информация о согласии пользователя
/// </summary>
[GraphQLDescription("Информация о согласии пользователя")]
public sealed class UserConsentResponse
{
    /// <summary>
    /// Идентификатор согласия
    /// </summary>
    [GraphQLDescription("Идентификатор согласия")]
    public Guid Id { get; init; }

    /// <summary>
    /// Тип согласия
    /// </summary>
    [GraphQLDescription("Тип согласия")]
    public ConsentType Type { get; init; }

    /// <summary>
    /// Идентификатор версии согласия
    /// </summary>
    [GraphQLDescription("Идентификатор версии согласия")]
    public Guid ConsentVersionId { get; init; }

    /// <summary>
    /// Номер версии документа
    /// </summary>
    [GraphQLDescription("Номер версии документа")]
    public required string Version { get; init; }

    /// <summary>
    /// Название документа
    /// </summary>
    [GraphQLDescription("Название документа")]
    public required string Title { get; init; }

    /// <summary>
    /// Время выдачи согласия
    /// </summary>
    [GraphQLDescription("Время выдачи согласия")]
    public DateTime GivenAt { get; init; }

    /// <summary>
    /// Время отзыва согласия
    /// </summary>
    [GraphQLDescription("Время отзыва согласия")]
    public DateTime? RevokedAt { get; init; }

    /// <summary>
    /// Признак отзыва согласия
    /// </summary>
    [GraphQLDescription("Признак отзыва согласия")]
    public bool IsRevoked => RevokedAt.HasValue;
}