using System.Text;

namespace MonEndoVue.Server.Services.Photos;

/// <summary>
/// Reconnaissance du format d'une image par ses premiers octets. Le nom et le type MIME envoyés par le client ne prouvent
/// rien : seul le contenu décide de ce qui est stocké, et de l'extension et du type servis ensuite.
/// </summary>
public static class SignaturePhoto
{
    /// <summary>Nombre d'octets à lire en tête de fichier pour reconnaître tous les formats acceptés.</summary>
    public const int OctetsNecessaires = 16;

    private static readonly string[] MarquesHeif =
        ["heic", "heix", "hevc", "hevx", "heim", "heis", "hevm", "hevs", "mif1", "msf1"];

    /// <summary>Extension canonique (avec le point) du format reconnu, ou <c>null</c> si ce n'est pas une image acceptée.</summary>
    public static string? Extension(ReadOnlySpan<byte> debut)
    {
        if (debut.Length >= 3 && debut[0] == 0xFF && debut[1] == 0xD8 && debut[2] == 0xFF) return ".jpg";

        if (debut.StartsWith(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) return ".png";

        if (debut.Length >= 12 && debut[..4].SequenceEqual("RIFF"u8) && debut[8..12].SequenceEqual("WEBP"u8)) return ".webp";

        if (debut.Length >= 12 && debut[4..8].SequenceEqual("ftyp"u8))
        {
            var marque = Encoding.ASCII.GetString(debut[8..12]);
            if (MarquesHeif.Contains(marque)) return ".heic";
        }

        return null;
    }

    /// <summary>Type MIME servi pour une extension canonique renvoyée par <see cref="Extension"/>.</summary>
    public static string TypeMime(string extension) => extension switch
    {
        ".jpg" => "image/jpeg",
        ".png" => "image/png",
        ".webp" => "image/webp",
        ".heic" => "image/heic",
        _ => "application/octet-stream",
    };
}
