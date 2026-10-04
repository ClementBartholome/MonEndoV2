namespace MonEndoVue.Server.Services.Photos;

/// <summary>Contrôle d'une photo reçue : taille puis format reconnu par le contenu (voir <see cref="SignaturePhoto"/>).</summary>
public static class ValidationPhoto
{
    public const long TailleMaxOctets = 10 * 1024 * 1024; // 10 MB

    /// <param name="extension">Extension canonique du format reconnu (nom stocké, type servi), <c>null</c> si refusée.</param>
    public static bool Valider(IFormFile photo, out string error, out string? extension)
    {
        extension = null;
        if (photo.Length == 0)
        {
            error = "Le fichier photo est vide.";
            return false;
        }

        if (photo.Length > TailleMaxOctets)
        {
            error = "La photo dépasse la taille maximale autorisée (10 MB).";
            return false;
        }

        Span<byte> debut = stackalloc byte[SignaturePhoto.OctetsNecessaires];
        using (var flux = photo.OpenReadStream())
        {
            var lus = flux.ReadAtLeast(debut, debut.Length, throwOnEndOfStream: false);
            extension = SignaturePhoto.Extension(debut[..lus]);
        }

        if (extension is null)
        {
            error = "Format de photo non supporté.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
