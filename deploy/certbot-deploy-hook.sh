#!/bin/sh
# Hook certbot exécuté après chaque renouvellement réussi du certificat (installé sur le VPS dans
# /etc/letsencrypt/renewal-hooks/deploy/monendo-nginx.sh, voir deploy/README.md).
# nginx lit une copie du certificat dans ~/app/ssl : la mettre à jour, puis recharger nginx sans coupure.
set -eu

LIGNEE="${RENEWED_LINEAGE:-/etc/letsencrypt/live/monendoapp.fr}"
DESTINATION=/home/debian/app/ssl

install -m 644 -o debian -g debian "$LIGNEE/fullchain.pem" "$DESTINATION/fullchain.pem"
install -m 600 -o debian -g debian "$LIGNEE/privkey.pem" "$DESTINATION/privkey.pem"
docker exec monendo_nginx_prod nginx -s reload
