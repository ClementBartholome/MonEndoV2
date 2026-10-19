using System.Text;
using Microsoft.AspNetCore.Http;
using MonEndoVue.Server.Services.Photos;

namespace MonEndoVue.Server.Tests.Services;

public sealed class ValidationPhotoTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0, 1, 1, 0, 0, 1];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, (byte)'I', (byte)'H', (byte)'D', (byte)'R'];

    private static byte[] Webp() => [.. "RIFF"u8, 0x24, 0, 0, 0, .. "WEBPVP8 "u8];

    private static byte[] Heif(string marque) => [0, 0, 0, 0x18, .. "ftyp"u8, .. Encoding.ASCII.GetBytes(marque), 0, 0, 0, 0];

    private static FormFile Fichier(byte[] contenu, string nom = "photo.jpg", string typeMime = "image/jpeg") =>
        new(new MemoryStream(contenu), 0, contenu.Length, "photo", nom) { Headers = new HeaderDictionary(), ContentType = typeMime };

    [Theory]
    [InlineData("jpeg", ".jpg")]
    [InlineData("png", ".png")]
    [InlineData("webp", ".webp")]
    [InlineData("heic", ".heic")]
    [InlineData("mif1", ".heic")]
    public void Valider_FormatReconnuParLeContenu_RenvoieLExtensionCanonique(string format, string attendue)
    {
        byte[] contenu = format switch
        {
            "jpeg" => Jpeg,
            "png" => Png,
            "webp" => Webp(),
            _ => Heif(format),
        };

        var valide = ValidationPhoto.Valider(Fichier(contenu), out _, out var extension);

        Assert.True(valide);
        Assert.Equal(attendue, extension);
    }

    [Fact]
    public void Valider_ExtensionEtTypeMimeIgnores_ContenuDecide()
    {
        // Un PNG déclaré comme .jpg / image/jpeg est stocké comme PNG.
        var valide = ValidationPhoto.Valider(Fichier(Png, "photo.jpg", "image/jpeg"), out _, out var extension);

        Assert.True(valide);
        Assert.Equal(".png", extension);
    }

    [Theory]
    [InlineData("<html><script>alert(1)</script></html>")]
    [InlineData("<svg xmlns=\"http://www.w3.org/2000/svg\"/>")]
    [InlineData("MZ executable")]
    public void Valider_ContenuQuiNEstPasUneImage_EstRefuseMemeAvecNomEtTypeMimeCrédibles(string texte)
    {
        var valide = ValidationPhoto.Valider(Fichier(Encoding.UTF8.GetBytes(texte), "photo.jpg", "image/jpeg"), out var erreur, out var extension);

        Assert.False(valide);
        Assert.Null(extension);
        Assert.Equal("Format de photo non supporté.", erreur);
    }

    [Fact]
    public void Valider_FichierTresCourt_EstRefuseSansLever()
    {
        Assert.False(ValidationPhoto.Valider(Fichier([0xFF, 0xD8]), out _, out _));
    }

    [Fact]
    public void Valider_FichierVide_EstRefuse()
    {
        Assert.False(ValidationPhoto.Valider(Fichier([]), out var erreur, out _));
        Assert.Equal("Le fichier photo est vide.", erreur);
    }

    [Fact]
    public void Valider_FichierTropGros_EstRefuseSansLireLeContenu()
    {
        var gros = new FormFile(new MemoryStream(Jpeg), 0, ValidationPhoto.TailleMaxOctets + 1, "photo", "photo.jpg");

        Assert.False(ValidationPhoto.Valider(gros, out var erreur, out _));
        Assert.Contains("taille maximale", erreur);
    }

    [Fact]
    public void Valider_FormatHeifSansMarqueConnue_EstRefuse()
    {
        Assert.False(ValidationPhoto.Valider(Fichier(Heif("avif")), out _, out _));
    }
}
