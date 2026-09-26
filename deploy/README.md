# Déploiement de production (VPS)

Ce dossier est la **référence versionnée** de la configuration du VPS (`~/app`). La CI ne le copie pas :
toute modification est appliquée à la main sur le serveur, puis commitée ici.

| Fichier | Rôle | Emplacement sur le VPS |
|---|---|---|
| `docker-compose.prod.yml` | Services `db`, `app`, `nginx`, `dozzle` | `~/app/docker-compose.prod.yml` |
| `logrotate-monendo-nginx` | Rotation des logs nginx (30 jours) | `/etc/logrotate.d/monendo-nginx` |

Les secrets ne sont **jamais** dans ce dossier (dépôt public). Ils restent sur le VPS :
- `~/app/.env` : `DB_PASSWORD`, réécrit par la CI à chaque déploiement (ne rien y ajouter d'autre) ;
- `~/app/config/app.env` : variables secrètes de l'application (`AZURE_STORAGE_CONNECTION_STRING=...`) ;
- `~/app/config/appsettings.Production.json`, `~/app/keys/`, `~/app/ssl/`.

## Appliquer une modification du compose

```bash
cd ~/app
cp docker-compose.prod.yml docker-compose.prod.yml.bak
curl -fsSL https://raw.githubusercontent.com/ClementBartholome/MonEndoV2/main/deploy/docker-compose.prod.yml -o docker-compose.prod.yml.new
diff docker-compose.prod.yml docker-compose.prod.yml.new
mv docker-compose.prod.yml.new docker-compose.prod.yml
docker compose -f docker-compose.prod.yml config --quiet && docker compose -f docker-compose.prod.yml up -d
```

Retour arrière : `mv docker-compose.prod.yml.bak docker-compose.prod.yml` puis `docker compose -f docker-compose.prod.yml up -d`.

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

puis ouvrir http://localhost:8888. Seuls les conteneurs `monendo_*` sont visibles ; recherche plein texte et filtre par niveau
dans l'interface. Pour l'historique au-delà de ce que garde Docker, lire les fichiers Serilog :

```bash
grep -h " \[ERR\]" ~/app/logs/MonEndoVue-*.log | tail -50
```

### Installer la rotation des logs nginx (une fois)

```bash
sudo curl -fsSL https://raw.githubusercontent.com/ClementBartholome/MonEndoV2/main/deploy/logrotate-monendo-nginx -o /etc/logrotate.d/monendo-nginx
sudo logrotate --debug /etc/logrotate.d/monendo-nginx
```

## Mémoire (VPS 2 Go)

Le VPS héberge aussi d'autres projets. SQL Server est plafonné (`MSSQL_MEMORY_LIMIT_MB=768`, conteneur limité à 1 Go)
et Dozzle à 96 Mo. Pas d'outil de logs lourd (Seq, Loki…) sur ce serveur. Surveiller avec `free -h` et
`docker stats --no-stream` : de la swap utilisée en continu signale un plafond trop haut.
