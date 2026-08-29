using HotChocolate;
using Planara.Common.Enums;
using Planara.Privacy.Data.Enums;

namespace Planara.Privacy.Responses;

/// <summary>
/// Информация о версии согласия
/// </summary>
[GraphQLDescription("Информация о версии согласия")]
public class ConsentVersionResponse
{
    /// <summary>
    /// Идентификатор версии согласия
    /// </summary>
    [GraphQLDescription("Идентификатор версии согласия")]
    public Guid Id { get; init; }

    /// <summary>
    /// Тип согласия.
    /// </summary>
    [GraphQLDescription("Тип согласия")]
    public ConsentType Type { get; init; }

    /// <summary>
    /// Номер версии документа
    /// </summary>
    [GraphQLDescription("Номер версии документа")]
    public required string Version { get; init; }

    /// <summary>
    /// Название документа.
    /// </summary>
    [GraphQLDescription("Название документа")]
    public required string Title { get; init; }

    /// <summary>
    /// Текстовое содержимое документа
    /// </summary>
    [GraphQLDescription("Текстовое содержимое документа")]
    public required string Content { get; init; }

    /// <summary>
    /// HTML-представление документа
    /// </summary>
    [GraphQLDescription("HTML-представление документа")]
    public required string HtmlContent { get; init; }

    /// <summary>
    /// Состояние версии документа
    /// </summary>
    [GraphQLDescription("Состояние версии документа")]
    public ConsentVersionStatus Status { get; init; }

    /// <summary>
    /// Время вступления версии в силу
    /// </summary>
    [GraphQLDescription("Время вступления версии в силу")]
    public DateTime EffectiveAt { get; init; }

    /// <summary>
    /// Время публикации версии
    /// </summary>
    [GraphQLDescription("Время публикации версии")]
    public DateTime? PublishedAt { get; init; }
}