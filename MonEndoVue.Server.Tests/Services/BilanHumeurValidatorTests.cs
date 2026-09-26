using MonEndoVue.Server.Models;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public class BilanHumeurValidatorTests
{
    private static BilanQuotidien Bilan(string? mood = null, params Emotion[] emotions) => new()
    {
        Mood = mood,
        Emotions = emotions.Select(e => new EmotionBilan { Emotion = e }).ToList(),
    };

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Valider_UneATroisEmotions_EstValide(int nombre)
    {
        var bilan = Bilan(null, Enum.GetValues<Emotion>().Take(nombre).ToArray());

        Assert.Equal((true, null), BilanHumeurValidator.Valider(bilan));
    }

    [Fact]
    public void Valider_AncienneHumeurSansEmotion_EstValide()
    {
        Assert.True(BilanHumeurValidator.Valider(Bilan("Neutre")).EstValide);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Valider_NiEmotionNiAncienneHumeur_EstRefuse(string? mood)
    {
        Assert.Equal((false, "Choisis au moins une émotion."), BilanHumeurValidator.Valider(Bilan(mood)));
    }

    [Fact]
    public void Valider_QuatreEmotions_EstRefuse()
    {
        var bilan = Bilan(null, Emotion.Joie, Emotion.Calme, Emotion.Fierte, Emotion.Tristesse);

        Assert.Equal((false, "Tu peux choisir 3 émotions au maximum."), BilanHumeurValidator.Valider(bilan));
    }

    [Fact]
    public void Valider_EmotionInconnue_EstRefuse()
    {
        Assert.Equal((false, "Émotion inconnue."), BilanHumeurValidator.Valider(Bilan(null, (Emotion)42)));
    }

    [Fact]
    public void Valider_EmotionEnDouble_EstRefuse()
    {
        var bilan = Bilan(null, Emotion.Anxiete, Emotion.Anxiete);

        Assert.Equal((false, "Chaque émotion ne peut être choisie qu'une fois."), BilanHumeurValidator.Valider(bilan));
    }

    [Theory]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    public void Valider_LongueurDuCommentaire(int longueur, bool attendu)
    {
        var bilan = Bilan(null, Emotion.Calme);
        bilan.Commentaire = new string('a', longueur);

        Assert.Equal(attendu, BilanHumeurValidator.Valider(bilan).EstValide);
    }
}
