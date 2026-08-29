using System.ComponentModel.DataAnnotations;
using Planara.Common.Database.Domain;

namespace Planara.Privacy.Data.Domain;

/// <summary>
/// Зафиксированный факт предоставления пользователем согласия
/// </summary>
public class UserConsent : BaseEntity
{
    /// <summary>
    /// Идентификатор запроса, в результате которого было создано согласие
    /// </summary>
    public Guid GrantRequestId { get; set; }

    /// <summary>
    /// Идентификатор версии документа
    /// </summary>
    public Guid ConsentVersionId { get; set; }

    /// <summary>
    /// Идентификатор регистрационной сессии, в рамках которой было предоставлено согласие
    /// </summary>
    public Guid? RegistrationId { get; set; }

    /// <summary>
    /// Идентификатор постоянного пользователя
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Время фактического предоставления согласия
    /// </summary>
    public DateTime GivenAt { get; set; }

    /// <summary>
    /// Время отзыва согласия
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Время истечения временного согласия
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// IP-адрес клиента в момент предоставления согласия
    /// </summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent клиента в момент предоставления согласия
    /// </summary>
    [MaxLength(1024)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Версия документа, на которую было предоставлено согласие
    /// </summary>
    public ConsentVersion ConsentVersion { get; set; } = null!;
}