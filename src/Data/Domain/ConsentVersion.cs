using System.ComponentModel.DataAnnotations;
using Planara.Common.Database.Domain;
using Planara.Common.Enums;
using Planara.Privacy.Data.Enums;

namespace Planara.Privacy.Data.Domain;

/// <summary>
/// Версия документа, на который пользователь может предоставить согласие
/// </summary>
public class ConsentVersion : BaseEntity
{
    /// <summary>
    /// Тип согласия
    /// </summary>
    public ConsentType Type { get; set; }

    /// <summary>
    /// Версия документа
    /// </summary>
    [MaxLength(50)]
    public required string Version { get; set; }

    /// <summary>
    /// Отображаемое название документа
    /// </summary>
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Текстовое содержимое документа
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// HTML-представление документа
    /// </summary>
    public required string HtmlContent { get; set; }

    /// <summary>
    /// Текущее состояние версии документа
    /// </summary>
    public ConsentVersionStatus Status { get; set; }

    /// <summary>
    /// Время вступления версии документа в силу
    /// </summary>
    public DateTime EffectiveAt { get; set; }

    /// <summary>
    /// Время публикации версии документа
    /// </summary>
    public DateTime? PublishedAt { get; set; }
}