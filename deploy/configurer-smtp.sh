#!/usr/bin/env bash
# Renseigne le relais SMTP des e-mails de compte dans ~/app/config/app.env (Email__Hote, __Port, __Utilisateur, __MotDePasse,
# __Expediteur, __NomExpediteur, __AdresseApplication). Par défaut les valeurs de Brevo (smtp-relay.brevo.com, port 587) ;
# n'importe quel relais SMTP avec STARTTLS convient.
# La clé SMTP est saisie à l'invite (masquée) : elle ne passe ni par l'historique du shell, ni par l'écran, ni par un fichier
# versionné. Le script n'affiche que des longueurs. Aucune valeur n'est enregistrée avant ta confirmation.
#
# Usage, sur le VPS :  bash ~/app/configurer-smtp.sh
# Puis recréer le conteneur (les options sont lues au démarrage) :
#   cd ~/app && docker compose -f docker-compose.prod.yml up -d --force-recreate app
# Vérifier : docker compose -f docker-compose.prod.yml logs app | grep -i "e-mails de compte"  (aucune ligne = envoi activé)
set -euo pipefail

FICHIER="${APP_ENV:-$HOME/app/config/app.env}"

[ -f "$FICHIER" ] || { echo "Fichier introuvable : $FICHIER" >&2; exit 1; }

# Saisie visible avec valeur par défaut (pour ce qui n'est pas secret).
demander() {
  local invite="$1" defaut="$2"
  printf '%s [%s] : ' "$invite" "$defaut" >&2
  IFS= read -r VALEUR
  VALEUR="$(printf '%s' "${VALEUR:-$defaut}" | tr -d '[:space:]')"
}

# Saisie masquée ; les espaces et retours chariot parasites d'un collage sont retirés.
lire_secret() {
  printf '%s' "$1" >&2
  IFS= read -r -s VALEUR
  printf '\n' >&2
  VALEUR="$(printf '%s' "$VALEUR" | tr -d '[:space:]')"
}

demander "Serveur SMTP" "smtp-relay.brevo.com"
HOTE="$VALEUR"
demander "Port (STARTTLS)" "587"
PORT="$VALEUR"
demander "Identifiant SMTP (donné par le service, page « SMTP et API »)" ""
UTILISATEUR="$VALEUR"
lire_secret "Clé SMTP (masquée) : "
CLE="$VALEUR"
demander "Adresse d'expédition (domaine authentifié chez le service)" "no-reply@monendoapp.fr"
EXPEDITEUR="$VALEUR"
demander "Adresse publique de l'application" "https://monendoapp.fr"
ADRESSE_APP="$VALEUR"

[[ "$PORT" =~ ^[0-9]+$ ]] && [ "$PORT" -ge 1 ] && [ "$PORT" -le 65535 ] || { echo "Port invalide : rien n'a été modifié." >&2; exit 1; }
[ -n "$HOTE" ] || { echo "Serveur vide : rien n'a été modifié." >&2; exit 1; }
[ -n "$UTILISATEUR" ] || { echo "Identifiant vide : rien n'a été modifié." >&2; exit 1; }
if [ "${#CLE}" -lt 20 ]; then
  echo "Clé SMTP trop courte (${#CLE} caractères, collage tronqué ?) : rien n'a été modifié." >&2
  exit 1
fi
[[ "$EXPEDITEUR" =~ ^[^@[:space:]]+@[^@[:space:]]+\.[^@[:space:]]+$ ]] || { echo "Adresse d'expédition invalide : rien n'a été modifié." >&2; exit 1; }
[[ "$ADRESSE_APP" =~ ^https://[^[:space:]]+$ ]] || { echo "L'adresse de l'application doit commencer par https:// : rien n'a été modifié." >&2; exit 1; }

echo "Serveur      : $HOTE:$PORT"
echo "Identifiant  : ${#UTILISATEUR} caractères"
echo "Clé SMTP     : ${#CLE} caractères"
echo "Expéditeur   : $EXPEDITEUR"
echo "Application  : $ADRESSE_APP"
echo "Fichier      : $FICHIER"
read -r -p "Écrire ces valeurs dans le fichier ? [o/N] " REPONSE
[ "$REPONSE" = "o" ] || { echo "Annulé : rien n'a été modifié."; exit 1; }

# Réécriture atomique : les anciennes lignes Email__ sont remplacées, le reste du fichier est conservé tel quel.
TEMPORAIRE="$(mktemp "$FICHIER.XXXXXX")"
trap 'rm -f "$TEMPORAIRE"' EXIT
{ grep -v '^Email__' "$FICHIER" || true; } | sed -e '$a\' > "$TEMPORAIRE"
printf 'Email__Hote=%s\nEmail__Port=%s\nEmail__Utilisateur=%s\nEmail__MotDePasse=%s\nEmail__Expediteur=%s\nEmail__NomExpediteur=MonEndo\nEmail__AdresseApplication=%s\n' \
  "$HOTE" "$PORT" "$UTILISATEUR" "$CLE" "$EXPEDITEUR" "$ADRESSE_APP" >> "$TEMPORAIRE"
chmod --reference="$FICHIER" "$TEMPORAIRE"
mv "$TEMPORAIRE" "$FICHIER"
trap - EXIT

echo "Fait : $(grep -c '^Email__' "$FICHIER") lignes Email__ dans $FICHIER (7 attendues)."
echo "Recrée le conteneur : cd ~/app && docker compose -f docker-compose.prod.yml up -d --force-recreate app"
