#!/usr/bin/env bash
# Renseigne la liaison de l'agenda Google dans ~/app/config/app.env (GoogleOAuth__ClientId, __ClientSecret, __RedirectUri).
# Les valeurs sont saisies à l'invite (masquées) : elles ne passent ni par l'historique du shell, ni par l'écran, ni par un fichier
# versionné. Le script n'affiche que des longueurs. Aucune valeur n'est enregistrée avant ta confirmation.
#
# Usage, sur le VPS :  bash ~/app/configurer-google-oauth.sh
# Puis recréer le conteneur (les options sont lues au démarrage) :
#   cd ~/app && docker compose -f docker-compose.prod.yml up -d --force-recreate app
set -euo pipefail

FICHIER="${APP_ENV:-$HOME/app/config/app.env}"
ADRESSE_RETOUR="${ADRESSE_RETOUR:-https://monendoapp.fr/Agenda/liaison/callback}"

[ -f "$FICHIER" ] || { echo "Fichier introuvable : $FICHIER" >&2; exit 1; }

# Saisie masquée ; les espaces et retours chariot parasites d'un collage sont retirés.
lire() {
  local invite="$1"
  printf '%s' "$invite" >&2
  IFS= read -r -s VALEUR
  printf '\n' >&2
  VALEUR="$(printf '%s' "$VALEUR" | tr -d '[:space:]')"
}

lire "ID client (se termine par .apps.googleusercontent.com) : "
ID_CLIENT="$VALEUR"
lire "Secret client : "
SECRET_CLIENT="$VALEUR"

if [[ ! "$ID_CLIENT" =~ ^[0-9]+-[A-Za-z0-9_]+\.apps\.googleusercontent\.com$ ]]; then
  echo "ID client inattendu (${#ID_CLIENT} caractères) : rien n'a été modifié." >&2
  exit 1
fi
if [ "${#SECRET_CLIENT}" -lt 20 ]; then
  echo "Secret client trop court (${#SECRET_CLIENT} caractères, collage tronqué ?) : rien n'a été modifié." >&2
  exit 1
fi
if [[ "$SECRET_CLIENT" != GOCSPX-* ]]; then
  echo "Attention : le secret ne commence pas par « GOCSPX- » comme les secrets Google habituels." >&2
fi

echo "ID client : ${#ID_CLIENT} caractères"
echo "Secret    : ${#SECRET_CLIENT} caractères (35 attendus pour un secret Google habituel)"
echo "Retour    : $ADRESSE_RETOUR"
echo "Fichier   : $FICHIER"
read -r -p "Écrire ces valeurs dans le fichier ? [o/N] " REPONSE
[ "$REPONSE" = "o" ] || { echo "Annulé : rien n'a été modifié."; exit 1; }

# Réécriture atomique : les anciennes lignes GoogleOAuth__ sont remplacées, le reste du fichier est conservé tel quel.
TEMPORAIRE="$(mktemp "$FICHIER.XXXXXX")"
trap 'rm -f "$TEMPORAIRE"' EXIT
{ grep -v '^GoogleOAuth__' "$FICHIER" || true; } | sed -e '$a\' > "$TEMPORAIRE"
printf 'GoogleOAuth__ClientId=%s\nGoogleOAuth__ClientSecret=%s\nGoogleOAuth__RedirectUri=%s\n' \
  "$ID_CLIENT" "$SECRET_CLIENT" "$ADRESSE_RETOUR" >> "$TEMPORAIRE"
chmod --reference="$FICHIER" "$TEMPORAIRE"
mv "$TEMPORAIRE" "$FICHIER"
trap - EXIT

echo "Fait : $(grep -c '^GoogleOAuth__' "$FICHIER") lignes GoogleOAuth__ dans $FICHIER (3 attendues)."
echo "Recrée le conteneur : cd ~/app && docker compose -f docker-compose.prod.yml up -d --force-recreate app"
