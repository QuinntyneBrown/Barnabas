using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Members.ExportMyData;

/// <summary>
/// Everything Barnabas holds about the caller.
/// </summary>
/// <remarks>
/// It names no member. The one it exports comes from the verified session, so there is no
/// identifier a caller could change in order to export somebody else — which is the only way to
/// build this endpoint safely.
/// <para>
/// Allowed to a member awaiting approval and to one who has left, because somebody who was never
/// let in and somebody who has gone are exactly the people most likely to ask what is held about
/// them.
/// </para>
/// </remarks>
public sealed record ExportMyDataQuery : IRequest<MyDataExport>, IAllowUnapproved;
