# Déploiement de production (VPS)

Ce dossier est la **référence versionnée** de la configuration du VPS (`~/app`). La CI ne le copie pas :
toute modification est appliquée à la main sur le serveur, puis commitée ici.

| Fichier | Rôle | Emplacement sur le VPS |
|---|---|---|
| `docker-compose.prod.yml` | Services `db`, `app`, `nginx`, `dozzle` | `~/app/docker-compose.prod.yml` |
| `nginx.conf` | Proxy HTTPS, redirection HTTP → HTTPS, HSTS | `~/app/nginx.conf` |
| `logrotate-monendo-nginx` | Rotation des logs nginx (30 jours) | `/etc/logrotate.d/monendo-nginx` |

Les secrets ne sont **jamais** dans ce dossier (dépôt public). Ils restent sur le VPS :
- `~/app/.env` : `DB_PASSWORD`, réécrit par la CI à chaque déploiement (ne rien y ajouter d'autre) ;
- `~/app/config/app.env` : variables secrètes de l'application (`AZURE_STORAGE_CONNECTION_STRING=...`, et pour l'agenda
  `Agenda__CleApi=...` et `Agenda__Calendriers__<id du compte>=<id du calendrier>`, clé limitée aux IPv4 et IPv6 du VPS) ;
- `~/app/config/appsettings.Production.json`, `~/app/keys/`, `~/app/ssl/`.

## Créer `config/app.env` (une fois, avant le premier compose qui l'utilise)

Le compose lit les secrets de l'application dans `~/app/config/app.env` (une ligne `CLE=valeur` par variable, sans
guillemets). Pour ne rien recopier à la main, ce script reprend la valeur dans le compose actuel, écrit le fichier en
`600` et n'affiche que des longueurs :

```bash
cat > /tmp/app_env.py <<'EOF'
import os, re, sys
src = os.path.expanduser('~/app/docker-compose.prod.yml')
dst = os.path.expanduser('~/app/config/app.env')
cles = ['AZURE_STORAGE_CONNECTION_STRING']
if os.path.exists(dst):
    sys.exit(f'{dst} existe déjà : rien écrit')
texte = open(src).read()
lignes = []
for cle in cles:
    m = re.search(r'^[ \t-]*["\']?' + cle + r'["\']?[ \t]*[=:][ \t]*(.+?)[ \t\r]*$', texte, re.M)
    if not m:
        sys.exit(f'{cle} introuvable dans {src} : rien écrit')
    valeur = m.group(1).strip('"\'')
    lignes.append(f'{cle}={valeur}')
    print(f'{cle} : {len(valeur)} caractères')
fd = os.open(dst, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, 'w') as f:
    f.write('\n'.join(lignes) + '\n')
print(f'écrit : {dst}')
EOF
python3 /tmp/app_env.py; rm -f /tmp/app_env.py
ls -l ~/app/config/app.env
```

Une connexion Azure Storage fait environ 170 à 200 caractères. Une longueur très différente signale une mauvaise
extraction : supprimer le fichier et recommencer.

## Appliquer une modification du compose

Tant que la modification n'est pas sur `main` (cycle de version en cours), remplacer `main` par le nom de la branche
(par exemple `release/1.2.0`) dans l'URL ci-dessous.

```bash
cd ~/app
cp docker-compose.prod.yml docker-compose.prod.yml.bak
curl -fsSL https://raw.githubusercontent.com/ClementBartholome/MonEndoV2/main/deploy/docker-compose.prod.yml -o docker-compose.prod.yml.new
diff docker-compose.prod.yml docker-compose.prod.yml.new
mv docker-compose.prod.yml.new docker-compose.prod.yml
docker compose -f docker-compose.prod.yml config --quiet && docker compose -f docker-compose.prod.yml up -d
```

Retour arrière : `mv docker-compose.prod.yml.bak docker-compose.prod.yml` puis `docker compose -f docker-compose.prod.yml up -d`.
Une fois le nouveau compose validé, supprimer la sauvegarde si elle contient encore des secrets (`rm docker-compose.prod.yml.bak`).

## Appliquer une modification de nginx

`nginx.conf` est monté comme un fichier seul : après l'avoir remplacé, **recréer** le conteneur (un simple `reload`
verrait encore l'ancien fichier). La configuration est testée avant, dans un conteneur jetable (`app` y est résolu
vers une adresse factice). Même remarque que pour le compose sur le nom de la branche dans l'URL.

```bash
cd ~/app
cp nginx.conf nginx.conf.bak
curl -fsSL https://raw.githubusercontent.com/ClementBartholome/MonEndoV2/main/deploy/nginx.conf -o nginx.conf.new
diff nginx.conf nginx.conf.new
docker run --rm --add-host app:127.0.0.1 -v "$PWD/nginx.conf.new:/etc/nginx/nginx.conf:ro" -v "$PWD/ssl:/etc/nginx/ssl:ro" nginx:alpine nginx -t \
  && mv nginx.conf.new nginx.conf \
  && docker compose -f docker-compose.prod.yml up -d --force-recreate nginx
```

Contrôle : `curl -sI http://monendoapp.fr/` redirige vers `https://monendoapp.fr/`, et `curl -sI https://monendoapp.fr/`
n'envoie qu'une fois chaque en-tête de sécurité. Retour arrière : `mv nginx.conf.bak nginx.conf` puis la même recréation.

Seul `Strict-Transport-Security` est posé par nginx ; tous les autres en-têtes de sécurité viennent de l'application
(`Services/EntetesSecurite.cs`) : ne pas les ajouter dans nginx, un en-tête en double est considéré comme invalide.

## Logs

| Source | Où | Conservation |
|---|---|---|
| Application (Serilog) | `~/app/logs/MonEndoVue-AAAAMMJJ.log` | 30 jours, purge automatique par l'application |
| Sortie console des conteneurs | Docker (`docker logs`, Dozzle) | 3 × 10 Mo par conteneur |
| nginx | `~/app/logs/nginx/` | 30 jours via logrotate |

Niveaux en production : `Information` pour l'application, `Warning` pour le framework (EF Core, ASP.NET Core).
Les logs ne doivent contenir ni email, ni token, ni donnée de santé.

### Consulter les logs avec Dozzle

Dozzle n'écoute que sur `127.0.0.1:8888` du VPS : il n'est pas joignable depuis Internet. Depuis le poste de dev :

```bash
ssh -N -L 8888:127.0.0.1:8888 debian@<hôte-du-vps>
```

(`<hôte-du-vps>` : l'adresse IP ou le nom utilisé pour la connexion SSH habituelle ; le nom affiché dans l'invite du VPS,
`vps-…`, n'est pas résolu depuis le poste). La commande reste ouverte sans rien afficher ; `Ctrl+C` ferme le tunnel. Ouvrir ensuite http://localhost:8888.
Dozzle propose d'activer une connexion et de monter `/data` : volontairement non fait, puisque seul un accès SSH au VPS
permet de l'atteindre (et donne déjà `docker logs`). À revoir si Dozzle devait un jour être exposé. Seuls les conteneurs `monendo_*` sont visibles ; recherche plein texte et filtre par niveau
dans l'interface. Pour l'historique au-delà de ce que garde Docker, lire les fichiers Serilog :

```bash
grep -h " \[ERR\]" ~/app/logs/MonEndoVue-*.log | tail -50
```

### Installer la rotation des logs nginx (une fois)

L'image Debian du VPS ne fournit pas `logrotate` : l'installer d'abord (le paquet active le timer systemd quotidien).

```bash
sudo apt-get update && sudo apt-get install -y logrotate
systemctl list-timers logrotate.timer
sudo curl -fsSL https://raw.githubusercontent.com/ClementBartholome/MonEndoV2/main/deploy/logrotate-monendo-nginx -o /etc/logrotate.d/monendo-nginx
sudo logrotate --debug /etc/logrotate.d/monendo-nginx
```

## Mémoire (VPS 2 Go)

Le VPS héberge aussi d'autres projets. SQL Server est plafonné (`MSSQL_MEMORY_LIMIT_MB=768`, conteneur limité à 1 Go)
et Dozzle à 96 Mo. Pas d'outil de logs lourd (Seq, Loki…) sur ce serveur. Surveiller avec `free -h` et
`docker stats --no-stream` : de la swap utilisée en continu signale un plafond trop haut.
