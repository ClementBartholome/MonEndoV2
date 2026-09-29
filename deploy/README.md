# Déploiement de production (VPS)

Ce dossier est la **référence versionnée** de la configuration du VPS (`~/app`). La CI ne le copie pas :
toute modification est appliquée à la main sur le serveur, puis commitée ici.

| Fichier | Rôle | Emplacement sur le VPS |
|---|---|---|
| `docker-compose.prod.yml` | Services `db`, `app`, `nginx`, `dozzle` | `~/app/docker-compose.prod.yml` |
| `nginx.conf` | Proxy HTTPS, redirection HTTP → HTTPS, HSTS, défis Let's Encrypt | `~/app/nginx.conf` |
| `certbot-deploy-hook.sh` | Copie du certificat renouvelé et rechargement de nginx | `/etc/letsencrypt/renewal-hooks/deploy/monendo-nginx.sh` |
| `logrotate-monendo-nginx` | Rotation des logs nginx (30 jours) | `/etc/logrotate.d/monendo-nginx` |
| `sauvegarde-base.sh` | Sauvegarde chiffrée de la base vers Azure, chaque nuit (cron) | `~/app/sauvegarde-base.sh` |

Les secrets ne sont **jamais** dans ce dossier (dépôt public). Ils restent sur le VPS :
- `~/app/.env` : `DB_PASSWORD`, réécrit par la CI à chaque déploiement (ne rien y ajouter d'autre) ;
- `~/app/config/app.env` : variables secrètes de l'application (`AZURE_STORAGE_CONNECTION_STRING=...`, et pour l'agenda
  `Agenda__CleApi=...` et `Agenda__Calendriers__<id du compte>=<id du calendrier>`, clé limitée aux IPv4 et IPv6 du VPS) ;
- `~/app/config/sauvegarde.env` et `~/app/config/sauvegarde.cle` : jeton SAS et clé de chiffrement des sauvegardes ;
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
TLS 1.2 doit aussi aboutir : `echo | openssl s_client -connect monendoapp.fr:443 -tls1_2 2>&1 | grep 'Cipher is'`.

Seul `Strict-Transport-Security` est posé par nginx ; tous les autres en-têtes de sécurité viennent de l'application
(`Services/EntetesSecurite.cs`) : ne pas les ajouter dans nginx, un en-tête en double est considéré comme invalide.

## Certificat TLS (Let's Encrypt)

Certificat `monendoapp.fr` (+ `www`), clé ECDSA, géré par certbot sur l'hôte (`certbot.timer`, deux fois par jour).
certbot fonctionne en mode **webroot** : il écrit ses défis dans `~/app/certbot-www`, que nginx sert sur le port 80
(`/.well-known/acme-challenge/`). Le mode `standalone` est inutilisable, nginx occupant déjà le port 80.
Après chaque renouvellement, le hook `certbot-deploy-hook.sh` copie le certificat dans `~/app/ssl` et recharge nginx.

Mise en place (une fois) : créer le dossier des défis, appliquer le compose et `nginx.conf` (sections ci-dessus), puis :

```bash
sudo curl -fsSL https://raw.githubusercontent.com/ClementBartholome/MonEndoV2/main/deploy/certbot-deploy-hook.sh -o /etc/letsencrypt/renewal-hooks/deploy/monendo-nginx.sh
sudo chmod 755 /etc/letsencrypt/renewal-hooks/deploy/monendo-nginx.sh
sudo certbot certonly --webroot -w /home/debian/app/certbot-www --cert-name monendoapp.fr -d monendoapp.fr -d www.monendoapp.fr --dry-run
sudo certbot certonly --webroot -w /home/debian/app/certbot-www --cert-name monendoapp.fr -d monendoapp.fr -d www.monendoapp.fr --force-renewal --non-interactive
sudo certbot renew --dry-run
```

Le premier `certonly` vérifie le défi sans rien changer ; le second renouvelle réellement et enregistre le mode webroot
dans `/etc/letsencrypt/renewal/monendoapp.fr.conf` ; certbot lance lui-même le hook du dossier après ce renouvellement
(sortie « Hook 'deploy-hook' ran »). Pour le relancer à la main :
`sudo RENEWED_LINEAGE=/etc/letsencrypt/live/monendoapp.fr /etc/letsencrypt/renewal-hooks/deploy/monendo-nginx.sh`. Contrôle : `certbot renew --dry-run` réussit, et la date d'expiration servie
(`echo | openssl s_client -connect monendoapp.fr:443 2>/dev/null | openssl x509 -noout -enddate`) est repoussée.

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

## Sauvegardes de la base

Chaque nuit, `sauvegarde-base.sh` (cron de l'utilisateur `debian`) :
1. fait une sauvegarde complète de la base (`BACKUP DATABASE … WITH CHECKSUM`, puis `RESTORE VERIFYONLY`) ;
2. la chiffre sur l'hôte (`openssl`, AES-256, clé `~/app/config/sauvegarde.cle`) sans laisser de copie en clair ;
3. l'envoie dans un conteneur Azure Blob privé, avec un jeton SAS qui ne permet **que la création** : le VPS ne peut ni
   lire, ni remplacer, ni supprimer une sauvegarde déjà envoyée ;
4. garde 3 jours de copies chiffrées dans `~/app/backups/`.

Azure supprime les sauvegardes de plus de **30 jours** (règle de cycle de vie) : c'est la durée annoncée par la politique
de confidentialité. Une donnée effacée (compte supprimé) disparaît donc des sauvegardes en 30 jours au plus.
SQL Server Express n'a ni Agent ni chiffrement des sauvegardes : d'où le cron et `openssl` sur l'hôte.

### Mise en place (une fois)

**Dans le portail Azure** (compte de stockage des photos, en Europe) :
1. Conteneurs → **+ Conteneur** : nom `sauvegardes`, niveau d'accès **Privé**.
2. Gestion du cycle de vie → **Ajouter une règle** : nom `sauvegardes-30-jours`, objets blob de bloc, filtre de préfixe
   `sauvegardes/`, condition « créé il y a plus de **30** jours » → **Supprimer l'objet blob**.
3. Conteneur `sauvegardes` → Jetons d'accès partagé : autorisations **Créer uniquement**, expiration dans 1 an
   (à noter dans l'agenda : le renouveler avant), protocole HTTPS uniquement → **Générer**. Garder la page ouverte.

**Sur le VPS** : la clé de chiffrement est générée sur place ; le jeton SAS est saisi une fois (seules des longueurs
sont affichées). `backups/` doit être accessible en écriture à SQL Server (utilisateur `mssql`, uid 10001, qui y écrit
la sauvegarde brute) et à `debian` (copie chiffrée) : sinon `BACKUP DATABASE` échoue (accès refusé).

```bash
cd ~/app
sudo chown 10001:debian backups && sudo chmod 770 backups
( umask 077; openssl rand -base64 48 > config/sauvegarde.cle ); wc -c config/sauvegarde.cle
docker exec monendo_database_prod bash -c 'S=/opt/mssql-tools18/bin/sqlcmd; [ -x $S ] || S=/opt/mssql-tools/bin/sqlcmd; SQLCMDPASSWORD="$SA_PASSWORD" $S -S localhost -U sa -C -h -1 -Q "SET NOCOUNT ON; SELECT name FROM sys.databases WHERE database_id > 4"'
cat > /tmp/sauvegarde_env.py <<'EOF'
import os, sys, urllib.parse
dst = os.path.expanduser('~/app/config/sauvegarde.env')
if os.path.exists(dst):
    sys.exit(f'{dst} existe déjà : rien écrit')
base = input('Nom de la base (affiché ci-dessus) : ').strip()
url = input('URL SAS du conteneur (Azure, « URL SAS d\'objet blob ») : ').strip()
conteneur, _, sas = url.partition('?')
params = urllib.parse.parse_qs(sas)
if not conteneur.endswith('/sauvegardes') or 'sig' not in params or params.get('sp') != ['c']:
    sys.exit('URL inattendue (conteneur « sauvegardes », droit « Créer » seul) : rien écrit')
fd = os.open(dst, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
with os.fdopen(fd, 'w') as f:
    f.write(f'SAUVEGARDE_BASE={base}\nSAUVEGARDE_URL_CONTENEUR={conteneur}\nSAUVEGARDE_SAS="{sas}"\n')
print(f'écrit : {dst} (base : {len(base)} car., SAS : {len(sas)} car.)')
EOF
python3 /tmp/sauvegarde_env.py; rm -f /tmp/sauvegarde_env.py
curl -fsSL https://raw.githubusercontent.com/ClementBartholome/MonEndoV2/main/deploy/sauvegarde-base.sh -o sauvegarde-base.sh
chmod 700 sauvegarde-base.sh
./sauvegarde-base.sh
```

Le dernier appel doit finir par « Terminé » et la sauvegarde apparaître dans le conteneur `sauvegardes` du portail.
Tant que la section n'est pas sur `main`, remplacer `main` par la branche de version dans l'URL (`release/1.3.0`).

**Copier la clé hors du VPS** (gestionnaire de mots de passe) : sans elle, les sauvegardes sont illisibles, et un VPS perdu
emporte la clé. `cat ~/app/config/sauvegarde.cle` l'affiche une seule fois pour la copier, puis effacer le terminal.

Programmer la sauvegarde chaque nuit à 2 h 30 (avant la purge des comptes inactifs de 3 h 30). Le VPS (Debian minimale)
n'a pas `cron` d'origine : l'installer d'abord.

```bash
sudo apt-get update && sudo apt-get install -y cron && sudo systemctl enable --now cron
( crontab -l 2>/dev/null; echo '30 2 * * * $HOME/app/sauvegarde-base.sh >> $HOME/app/logs/sauvegarde.log 2>&1' ) | crontab -
crontab -l
```

Contrôle : `tail ~/app/logs/sauvegarde.log` (une ligne « Terminé » par nuit ; « ÉCHEC : envoi vers Azure (SAS expiré ?) »
signale un jeton à renouveler : regénérer le SAS puis remplacer la ligne `SAUVEGARDE_SAS` avec le même script, après
avoir supprimé `config/sauvegarde.env`).

### Restaurer (et tester la restauration une fois par trimestre)

Télécharger la sauvegarde voulue depuis le portail (conteneur `sauvegardes` → fichier → Télécharger), la copier dans
`~/app/backups/` (`scp`), ou reprendre une copie locale de moins de 3 jours. Puis, sur le VPS :

```bash
cd ~/app
F=backups/monendo-AAAA-MM-JJ.bak.enc
openssl enc -d -aes-256-cbc -pbkdf2 -iter 200000 -pass file:config/sauvegarde.cle -in "$F" -out backups/restauration.bak
chmod 644 backups/restauration.bak
SQL='S=/opt/mssql-tools18/bin/sqlcmd; [ -x $S ] || S=/opt/mssql-tools/bin/sqlcmd; SQLCMDPASSWORD="$SA_PASSWORD" $S -S localhost -U sa -C -Q "$1"'
docker exec monendo_database_prod bash -c "$SQL" _ "RESTORE FILELISTONLY FROM DISK = N'/var/backups/restauration.bak'"
```

**Test** (sans toucher à la production) : restaurer dans une base à part, avec les noms logiques affichés par
`FILELISTONLY` (colonne `LogicalName`), vérifier quelques comptes, puis la supprimer.

```bash
docker exec monendo_database_prod bash -c "$SQL" _ "RESTORE DATABASE [MonEndo_Test] FROM DISK = N'/var/backups/restauration.bak' WITH MOVE N'<données>' TO N'/var/opt/mssql/data/MonEndo_Test.mdf', MOVE N'<journal>' TO N'/var/opt/mssql/data/MonEndo_Test_log.ldf'"
docker exec monendo_database_prod bash -c "$SQL" _ "SELECT COUNT(*) AS comptes FROM [MonEndo_Test].dbo.AspNetUsers; SELECT COUNT(*) AS bilans FROM [MonEndo_Test].dbo.BilansQuotidiens"
docker exec monendo_database_prod bash -c "$SQL" _ "DROP DATABASE [MonEndo_Test]"
rm -f backups/restauration.bak
```

**Restauration réelle** (après un incident) : arrêter l'application, restaurer par-dessus la base, redémarrer.
L'application réapplique au démarrage les migrations manquantes.

```bash
docker compose -f docker-compose.prod.yml stop app
docker exec monendo_database_prod bash -c "$SQL" _ "RESTORE DATABASE [<base>] FROM DISK = N'/var/backups/restauration.bak' WITH REPLACE"
docker compose -f docker-compose.prod.yml start app
rm -f backups/restauration.bak
```

## Mémoire (VPS 2 Go)

Le VPS héberge aussi d'autres projets. SQL Server est plafonné (`MSSQL_MEMORY_LIMIT_MB=768`, conteneur limité à 1 Go)
et Dozzle à 96 Mo. Pas d'outil de logs lourd (Seq, Loki…) sur ce serveur. Surveiller avec `free -h` et
`docker stats --no-stream` : de la swap utilisée en continu signale un plafond trop haut.
