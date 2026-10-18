using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MonEndoVue.Server.Models;

namespace MonEndoVue.Server.Services.Comptes;

/// <summary>Durée de vie propre aux jetons de réinitialisation du mot de passe (1 h), plus courte que la confirmation d'adresse (24 h).</summary>
public class OptionsJetonReinitialisation : DataProtectionTokenProviderOptions
{
    public OptionsJetonReinitialisation()
    {
        Name = FournisseurJetonReinitialisation.Nom;
        TokenLifespan = TimeSpan.FromHours(1);
    }
}

/// <summary>Fournisseur de jetons de réinitialisation : celui d'Identity, avec ses propres options (durée de vie).</summary>
public class FournisseurJetonReinitialisation(
    IDataProtectionProvider protection,
    IOptions<OptionsJetonReinitialisation> options,
    ILogger<DataProtectorTokenProvider<ApplicationUser>> logger)
    : DataProtectorTokenProvider<ApplicationUser>(protection, options, logger)
{
    public const string Nom = "ReinitialisationMotDePasse";
}
