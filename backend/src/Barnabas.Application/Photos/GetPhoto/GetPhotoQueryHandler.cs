using Barnabas.Application.Common.Exceptions;
using Barnabas.Application.Photos.Common;
using MediatR;

namespace Barnabas.Application.Photos.GetPhoto;

/// <summary>Reads the bytes, or answers not found.</summary>
public sealed class GetPhotoQueryHandler : IRequestHandler<GetPhotoQuery, StoredPhoto>
{
    private readonly IPhotoStore _photos;

    public GetPhotoQueryHandler(IPhotoStore photos) => _photos = photos;

    public async Task<StoredPhoto> Handle(GetPhotoQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await _photos.ReadAsync(request.PhotoId, request.Size, cancellationToken)
            ?? throw new NotFoundException();
    }
}
