namespace Barnabas.Application.Photos.Common;

/// <summary>One rendition of one photo, as bytes and the type they actually are.</summary>
public sealed record StoredPhoto(byte[] Bytes, string ContentType);
