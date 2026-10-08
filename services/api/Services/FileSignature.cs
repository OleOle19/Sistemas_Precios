namespace SistemasPrecios.Api.Services;

public static class FileSignature
{
    public static bool IsValid(ReadOnlySpan<byte> header, string mime) => mime switch
    {
        "image/jpeg" => header.StartsWith(new byte[] { 0xff, 0xd8, 0xff }),
        "image/png" => header.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "application/pdf" => header.StartsWith("%PDF-"u8),
        "image/webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };
}
