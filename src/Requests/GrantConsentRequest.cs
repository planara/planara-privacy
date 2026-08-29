using HotChocolate;

namespace Planara.Privacy.Requests;

/// <summary>
/// Запрос на выдачу согласия
/// </summary>
[GraphQLDescription("Запрос на выдачу согласия")]
public class GrantConsentRequest
{
    /// <summary>
    /// Идентификатор версии согласия
    /// </summary>
    [GraphQLDescription("Идентификатор версии согласия")]
    public Guid ConsentVersionId { get; init; }
}