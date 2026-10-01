using Microsoft.AspNetCore.Http;
using MonEndoVue.Server.Services;

namespace MonEndoVue.Server.Tests.Services;

public class NavigationVersSpaTests
{
    private const string AcceptNavigateur = "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,*/*;q=0.8";

    private static HttpRequest Requete(string chemin, string accept, string methode = "GET")
    {
        var requete = new DefaultHttpContext().Request;
        requete.Method = methode;
        requete.Path = chemin;
        requete.Headers.Accept = accept;
        return requete;
    }

    [Theory]
    [InlineData("/cycle")]
    [InlineData("/Cycle")]
    [InlineData("/activite")]
    [InlineData("/douleurs")]
    [InlineData("/medicaments/12")]
    public void Appliquer_NavigationDuNavigateur_VaVersLaPageDEntree(string chemin)
    {
        var requete = Requete(chemin, AcceptNavigateur);

        NavigationVersSpa.Appliquer(requete);

        Assert.Equal("/index.html", requete.Path.Value);
    }

    [Theory]
    [InlineData("/Cycle", "application/json, text/plain, */*")]
    [InlineData("/Activite", "*/*")]
    [InlineData("/Acne/photos/3", "image/avif,image/webp,image/*,*/*;q=0.8")]
    [InlineData("/Cycle", "")]
    public void Appliquer_AppelDeLApi_NeChangeRien(string chemin, string accept)
    {
        var requete = Requete(chemin, accept);

        NavigationVersSpa.Appliquer(requete);

        Assert.Equal(chemin, requete.Path.Value);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/Agenda/liaison/callback")]
    [InlineData("/agenda/liaison/callback")]
    [InlineData("/swagger/index.html")]
    [InlineData("/assets/index-abc.js")]
    public void Appliquer_CheminDuServeurOuFichier_NeChangeRien(string chemin)
    {
        var requete = Requete(chemin, AcceptNavigateur);

        NavigationVersSpa.Appliquer(requete);

        Assert.Equal(chemin, requete.Path.Value);
    }

    [Fact]
    public void Appliquer_RequeteNonGet_NeChangeRien()
    {
        var requete = Requete("/Cycle/regles/2026-09-30", AcceptNavigateur, "PUT");

        NavigationVersSpa.Appliquer(requete);

        Assert.Equal("/Cycle/regles/2026-09-30", requete.Path.Value);
    }
}
