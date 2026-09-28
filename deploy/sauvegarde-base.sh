#!/usr/bin/env bash
# Sauvegarde nocturne de la base MonEndo (VPS) : sauvegarde complète SQL Server, chiffrement sur l'hôte, copie hors du
# VPS dans un conteneur Azure Blob (suppression automatique après 30 jours par une règle de cycle de vie Azure).
# Installation, restauration et contrôle : deploy/README.md, section « Sauvegardes de la base ».
#
# Configuration, uniquement sur le VPS (jamais dans le dépôt) :
#   ~/app/config/sauvegarde.env  SAUVEGARDE_BASE, SAUVEGARDE_URL_CONTENEUR, SAUVEGARDE_SAS (droit « Créer » seul)
#   ~/app/config/sauvegarde.cle  clé de chiffrement (une copie doit exister hors du VPS)
# N'affiche jamais de secret : seulement des tailles et des statuts.
set -euo pipefail

APP="${HOME}/app"
CONTENEUR_DB="monendo_database_prod"
CONFIG="${APP}/config/sauvegarde.env"
CLE="${APP}/config/sauvegarde.cle"
LOCAL="${APP}/backups"
BAK_CONTENEUR="/var/backups/monendo-sauvegarde.bak"

horodatage() { date -u '+%Y-%m-%dT%H:%M:%SZ'; }
journal() { echo "$(horodatage) $*"; }
echec() { journal "ÉCHEC : $*"; exit 1; }

[[ -r "${CONFIG}" ]] || echec "${CONFIG} introuvable"
[[ -r "${CLE}" ]] || echec "${CLE} introuvable"
# shellcheck source=/dev/null
source "${CONFIG}"
: "${SAUVEGARDE_BASE:?absent de sauvegarde.env}" "${SAUVEGARDE_URL_CONTENEUR:?absent de sauvegarde.env}" "${SAUVEGARDE_SAS:?absent de sauvegarde.env}"

# sqlcmd dans le conteneur (chemin selon la version de l'image) ; le mot de passe reste dans le conteneur (SA_PASSWORD).
# Silencieux en cas de succès ; en cas d'échec, affiche le message de SQL Server (jamais de secret).
sqlcmd() {
  local sortie
  sortie=$(docker exec "${CONTENEUR_DB}" bash -c \
    'SQLCMD=/opt/mssql-tools18/bin/sqlcmd; [ -x "$SQLCMD" ] || SQLCMD=/opt/mssql-tools/bin/sqlcmd;
     SQLCMDPASSWORD="$SA_PASSWORD" "$SQLCMD" -S localhost -U sa -C -b -Q "$1"' _ "$1" 2>&1) \
    || { echo "${sortie}" | tail -n 5; return 1; }
}

nom="monendo-$(date -u '+%Y-%m-%d').bak.enc"
chiffre="${LOCAL}/${nom}"

journal "Sauvegarde de ${SAUVEGARDE_BASE}"
sqlcmd "BACKUP DATABASE [${SAUVEGARDE_BASE}] TO DISK = N'${BAK_CONTENEUR}' WITH INIT, FORMAT, CHECKSUM" \
  || echec "BACKUP DATABASE"
sqlcmd "RESTORE VERIFYONLY FROM DISK = N'${BAK_CONTENEUR}' WITH CHECKSUM" || echec "vérification de la sauvegarde"

# Lecture par le conteneur (propriétaire du fichier), chiffrement sur l'hôte : aucune copie en clair ne reste.
umask 077
docker exec "${CONTENEUR_DB}" cat "${BAK_CONTENEUR}" \
  | openssl enc -aes-256-cbc -pbkdf2 -iter 200000 -salt -pass "file:${CLE}" -out "${chiffre}" \
  || { docker exec "${CONTENEUR_DB}" rm -f "${BAK_CONTENEUR}"; echec "chiffrement"; }
docker exec "${CONTENEUR_DB}" rm -f "${BAK_CONTENEUR}"
journal "Chiffrée : ${nom} ($(du -h "${chiffre}" | cut -f1))"

# Envoi hors du VPS (SAS limité à la création : ce serveur ne peut ni lire, ni remplacer, ni supprimer une sauvegarde
# déjà envoyée ; relancer le même jour échoue donc sur l'envoi, la sauvegarde de la nuit étant déjà en place).
curl -fsS --retry 3 -X PUT -H "x-ms-blob-type: BlockBlob" --upload-file "${chiffre}" \
  "${SAUVEGARDE_URL_CONTENEUR}/${nom}?${SAUVEGARDE_SAS}" -o /dev/null \
  || echec "envoi vers Azure (SAS expiré ?)"
journal "Envoyée vers Azure"

# Copie locale chiffrée gardée 3 jours pour une restauration rapide.
find "${LOCAL}" -maxdepth 1 -name 'monendo-*.bak.enc' -mtime +2 -delete
journal "Terminé"
