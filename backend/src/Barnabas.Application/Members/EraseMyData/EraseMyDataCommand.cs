using Barnabas.Application.Common.Authorisation;
using MediatR;

namespace Barnabas.Application.Members.EraseMyData;

/// <summary>
/// The caller asks to be forgotten.
/// </summary>
/// <remarks>
/// It names no member, for the same reason the export does not: the one erased comes from the
/// verified session, so there is no identifier a caller could change in order to erase somebody
/// else. Of every endpoint in the product this is the one where that matters most.
/// <para>
/// Allowed to a member who has already left, because leaving and being forgotten are different
/// acts and most people do them in that order.
/// </para>
/// </remarks>
public sealed record EraseMyDataCommand : IRequest<ErasedDataResult>, IAllowUnapproved;
