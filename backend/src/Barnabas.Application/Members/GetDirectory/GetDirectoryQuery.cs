using MediatR;

namespace Barnabas.Application.Members.GetDirectory;

/// <summary>
/// The approved members of the caller's own congregation.
/// </summary>
/// <remarks>
/// It names no congregation. The global filter supplies that predicate, which is what makes
/// <c>L2-079 AC2</c> true without a comparison a handler could forget.
/// </remarks>
public sealed record GetDirectoryQuery(string? Term, string? HelpTag) : IRequest<IReadOnlyList<DirectoryMemberDto>>;
