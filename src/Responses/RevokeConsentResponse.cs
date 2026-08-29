using HotChocolate;
using Planara.Common.Enums;

namespace Planara.Privacy.Responses;

/// <summary>
/// Результат изменения состояния согласия пользователя
/// </summary>
[GraphQLDescription("Результат изменения состояния согласия пользователя")]
public class ConsentMutationResponse
{
    /// <summary>
    /// Идентификатор согласия
    /// </summary>
    [GraphQLDescription("Идентификатор согласия")]
    public Guid ConsentId { get; init; }

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
    /// Время изменения состояния согласия
    /// </summary>
    [GraphQLDescription("Время изменения состояния согласия")]
    public DateTime ChangedAt { get; init; }
}