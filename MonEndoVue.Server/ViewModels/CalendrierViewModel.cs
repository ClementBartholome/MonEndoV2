namespace MonEndoVue.Server.ViewModels;

/// <summary>Calendrier Google proposé au choix : son identifiant (renvoyé au choix) et son nom affiché.</summary>
public sealed record CalendrierViewModel(string Id, string Nom, bool Principal);
