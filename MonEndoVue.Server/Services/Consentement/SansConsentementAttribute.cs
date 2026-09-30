namespace MonEndoVue.Server.Services.Consentement;

/// <summary>
/// Endpoint accessible sans consentement à jour : authentification, recueil du consentement et, à terme,
/// suppression du compte (retirer son consentement ne doit jamais être bloqué).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SansConsentementAttribute : Attribute;
