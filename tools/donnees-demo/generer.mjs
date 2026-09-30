// Jeu de données de démonstration proche du réel pour les tests manuels en local (jamais en production).
// Génère le SQL d'environ douze mois de suivi pour un carnet de la base de dev : règles, bilans complets (avec oublis),
// douleurs, symptômes, épisodes d'acné, activités, traitements et prises. Données génériques, rien de personnel.
// Ajoute sans rien effacer : un jour qui a déjà un bilan ou un jour de règles est laissé tel quel.
//
//   node tools/donnees-demo/generer.mjs --carnet 1 [--mois 12] [--jusqua 2026-09-29] > demo.sql
//   sqlcmd -S "localhost\MSSQLSERVER01" -E -C -I -f 65001 -d MonEndo -i demo.sql
//
// -f 65001 : le fichier est lu en UTF-8 (sans, les accents sont abîmés : « KinÃ© »). Lancer sqlcmd depuis le dossier
// du fichier : un chemin qui contient « -U… » (dossier temporaire de Claude) est pris pour une option.
// Le carnet de l'utilisatrice la plus récemment active en local :
//   SELECT TOP 1 c.Id FROM CarnetSantes c JOIN AspNetUsers u ON u.Id = c.UserId ORDER BY u.DerniereActiviteLe DESC

const args = Object.fromEntries(process.argv.slice(2).reduce((paires, a, i, t) => (a.startsWith('--') ? [...paires, [a.slice(2), t[i + 1]]] : paires), []));
const carnet = Number(args.carnet);
if (!Number.isInteger(carnet) || carnet <= 0) {
  console.error('Usage : node generer.mjs --carnet <id> [--mois 12] [--jusqua AAAA-MM-JJ]');
  process.exit(1);
}
const mois = Number(args.mois ?? 12);
const fin = args.jusqua ? new Date(`${args.jusqua}T12:00:00`) : new Date();
fin.setHours(12, 0, 0, 0);
const debut = new Date(fin);
debut.setMonth(debut.getMonth() - mois);

// mulberry32 : tirages reproductibles d'une exécution à l'autre.
let etat = 20260929;
const hasard = () => {
  etat = (etat + 0x6d2b79f5) >>> 0;
  let t = etat;
  t = Math.imul(t ^ (t >>> 15), t | 1);
  t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
  return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
};
const entre = (min, max) => min + Math.floor(hasard() * (max - min + 1));
const parmi = (liste) => liste[Math.floor(hasard() * liste.length)];
const ajouter = (date, jours) => { const d = new Date(date); d.setDate(d.getDate() + jours); return d; };
const deuxChiffres = (n) => String(n).padStart(2, '0');
const jourSql = (d) => `${d.getFullYear()}${deuxChiffres(d.getMonth() + 1)}${deuxChiffres(d.getDate())}`;
const dateSql = (d, h, m = 0) => `'${jourSql(d)} ${deuxChiffres(h)}:${deuxChiffres(m)}'`;
const texte = (s) => (s === null ? 'NULL' : `N'${s.replace(/'/g, "''")}'`);
const nombre = (n) => (n === null ? 'NULL' : String(n));
const bit = (b) => (b === null ? 'NULL' : b ? '1' : '0');

const sql = ['SET NOCOUNT ON;', 'SET XACT_ABORT ON;', 'BEGIN TRANSACTION;', 'DECLARE @id int;'];

// --- Règles : cycles de 26 à 32 jours, règles de 4 à 6 jours ---
const jourDeRegles = new Map();
for (let d = ajouter(debut, entre(0, 10)); d <= fin; d = ajouter(d, entre(26, 32))) {
  const duree = entre(4, 6);
  for (let i = 0; i < duree; i++) {
    const jour = ajouter(d, i);
    if (jour > fin) break;
    jourDeRegles.set(jourSql(jour), i);
    sql.push(`IF NOT EXISTS (SELECT 1 FROM JourRegles WHERE CarnetSanteId = ${carnet} AND CAST(Date AS date) = '${jourSql(jour)}') INSERT INTO JourRegles (CarnetSanteId, Date) VALUES (${carnet}, '${jourSql(jour)}');`);
  }
}
const joursAvantRegles = (d) => {
  for (let i = 1; i <= 3; i++) if (jourDeRegles.has(jourSql(ajouter(d, i)))) return i;
  return null;
};

// --- Traitements ---
const traitement = (nom, posologie, type, frequence, joursSemaine, debutT, finT, horaires) => {
  sql.push(`INSERT INTO Medicaments (CarnetSanteId, Nom, Posologie, TraitementEnCours, DateDebutTraitement, DateFinTraitement, Type, Frequence, IntervalleJours, JoursSemaine) VALUES (${carnet}, ${texte(nom)}, ${texte(posologie)}, ${finT ? 0 : 1}, '${jourSql(debutT)}', ${finT ? `'${jourSql(finT)}'` : 'NULL'}, ${type}, N'${frequence}', NULL, ${joursSemaine});`);
  sql.push('SET @id = SCOPE_IDENTITY();');
  const variable = `@t${sql.length}`;
  sql.push(`DECLARE ${variable} int = @id;`);
  for (const h of horaires) sql.push(`INSERT INTO HorairesPrise (Heure, MedicamentId) VALUES ('${h}', ${variable});`);
  return variable;
};
const debutPilule = debut;
const finPilule = ajouter(debut, 150);
const pilule = traitement('Pilule (ancienne)', '1 comprimé', 0, 'ChaqueJour', 0, debutPilule, finPilule, ['21:00']);
const dienogest = traitement('Diénogest 2 mg', '1 comprimé', 0, 'ChaqueJour', 0, ajouter(finPilule, 1), null, ['08:00']);
// Lundi (1), mercredi (4), vendredi (16).
const debutMagnesium = ajouter(fin, -150);
const magnesium = traitement('Magnésium', '2 gélules', 0, 'CertainsJours', 21, debutMagnesium, null, ['20:00']);
const ibuprofene = traitement('Ibuprofène 400 mg', '1 comprimé', 0, 'AuBesoin', 0, debut, null, []);
const debutKine = ajouter(fin, -120);
const kine = traitement('Kiné pelvienne', null, 1, 'AuBesoin', 0, debutKine, null, []);

const prise = (medicament, jour, heurePrevue, statut, h, m) => sql.push(
  `INSERT INTO DonneesMedicaments (CarnetSanteId, MedicamentId, Date, Commentaire, NombreComprimes, HeurePrevue, Statut) VALUES (${carnet}, ${medicament}, ${dateSql(jour, h, m)}, NULL, ${statut === 'Pris' ? 1 : 0}, ${heurePrevue ? `'${heurePrevue}'` : 'NULL'}, N'${statut}');`);

// --- Épisodes d'acné : longs, avec des accalmies ; le dernier en cours ---
let finEpisode = null;
for (let d = ajouter(debut, entre(5, 30)); d <= fin; d = ajouter(finEpisode, entre(10, 40))) {
  finEpisode = ajouter(d, entre(20, 70));
  const enCours = finEpisode >= fin;
  sql.push(`IF NOT EXISTS (SELECT 1 FROM EpisodesAcne WHERE CarnetSanteId = ${carnet} AND Debut <= '${jourSql(enCours ? fin : finEpisode)}' AND (Fin IS NULL OR Fin >= '${jourSql(d)}')) INSERT INTO EpisodesAcne (CarnetSanteId, Debut, Fin) VALUES (${carnet}, '${jourSql(d)}', ${enCours ? 'NULL' : `'${jourSql(finEpisode)}'`});`);
  if (enCours) break;
}

// --- Jour par jour ---
const AGREABLES = ['Joie', 'Calme', 'Soulagement', 'Motivation', 'Fierte'];
const DIFFICILES = ['Tristesse', 'Anxiete', 'Irritabilite', 'Frustration', 'Decouragement'];
const TYPES_DOULEUR = ['Douleur pelvienne', 'Douleur pelvienne', 'Douleur pelvienne', 'Douleur lombaire', 'Douleur abdominale', 'Douleur projetée', 'Douleur neuropathique'];
const ACTIVITES = ['Marche', 'Marche', 'Yoga', 'Étirements', 'Natation', 'Vélo'];
const NOTES = ['Journée chargée au travail', 'Bien dormi', 'Soirée tranquille', 'Rendez-vous gynéco', 'Beaucoup marché', 'Nuit agitée'];

for (let jour = new Date(debut); jour <= fin; jour = ajouter(jour, 1)) {
  const cle = jourSql(jour);
  const regles = jourDeRegles.get(cle);
  const avant = joursAvantRegles(jour);
  const pic = regles !== undefined ? 4 - regles * 0.8 : avant ? 1.5 : 0;
  const douleurJour = Math.max(0, Math.min(10, Math.round(1.5 + pic + hasard() * 3)));

  // Bilan (un oubli environ un jour sur neuf).
  if (hasard() > 0.11) {
    const difficile = douleurJour >= 6 || hasard() < 0.25;
    const emotions = [...new Set([difficile ? parmi(DIFFICILES) : parmi(AGREABLES), ...(hasard() < 0.45 ? [parmi(hasard() < 0.6 ? AGREABLES : DIFFICILES)] : [])])];
    const transit = hasard() < 0.45;
    const crampes = transit && hasard() < 0.25;
    const ballonnements = transit && hasard() < 0.35;
    sql.push(`IF NOT EXISTS (SELECT 1 FROM BilansQuotidiens WHERE CarnetSanteId = ${carnet} AND CAST(Date AS date) = '${cle}') BEGIN`);
    sql.push(`INSERT INTO BilansQuotidiens (CarnetSanteId, Date, Mood, StressPro, StressPerso, Fatigue, Pas, Hydratation, Gluten, Lactose, Grignotage, Commentaire, DouleurMoyenne, Ballonnements, CrampesEstomac, IntensiteBallonnements, IntensiteCrampes, Selles, TypeBristol) VALUES (${carnet}, ${dateSql(jour, 21, entre(0, 50))}, NULL, ${nombre(hasard() < 0.2 ? null : entre(0, 4))}, ${nombre(hasard() < 0.2 ? null : entre(0, 3))}, ${nombre(hasard() < 0.08 ? null : Math.min(5, Math.round(1 + douleurJour / 3 + hasard() * 1.5)))}, ${nombre(hasard() < 0.15 ? null : entre(2500, 11000))}, ${nombre(hasard() < 0.15 ? null : Math.round((0.8 + hasard() * 1.4) * 10) / 10)}, ${bit(hasard() < 0.3)}, ${bit(hasard() < 0.25)}, ${bit(hasard() < 0.35)}, ${texte(hasard() < 0.12 ? parmi(NOTES) : null)}, ${douleurJour}, ${bit(transit ? ballonnements : null)}, ${bit(transit ? crampes : null)}, ${texte(ballonnements ? parmi(['Légère', 'Modérée', 'Forte']) : null)}, ${texte(crampes ? parmi(['Légère', 'Modérée', 'Forte']) : null)}, ${bit(transit ? hasard() < 0.85 : null)}, ${nombre(transit ? entre(2, 6) : null)});`);
    sql.push('SET @id = SCOPE_IDENTITY();');
    for (const emotion of emotions) sql.push(`INSERT INTO EmotionsBilan (Emotion, BilanQuotidienId) VALUES (N'${emotion}', @id);`);
    sql.push('END');
  }

  // Douleurs notées dans la journée.
  const nombreDouleurs = douleurJour >= 6 ? entre(1, 3) : hasard() < 0.35 ? 1 : 0;
  for (let i = 0; i < nombreDouleurs; i++) {
    sql.push(`INSERT INTO DonneesDouleurs (CarnetSanteId, Intensite, TypeDouleur, Date, Commentaire) VALUES (${carnet}, ${Math.max(1, Math.min(10, douleurJour + entre(-1, 2)))}, N'${parmi(TYPES_DOULEUR)}', ${dateSql(jour, entre(7, 22), entre(0, 59))}, ${texte(hasard() < 0.2 ? parmi(['au réveil', 'après le repas', 'en fin de journée', 'bouillotte, ça soulage']) : null)});`);
  }

  // Symptômes.
  if (avant && hasard() < 0.4) sql.push(`INSERT INTO SymptomesCycles (CarnetSanteId, TypeSymptome, Date, Intensite, Commentaire, PhotoUrl) VALUES (${carnet}, N'Spotting', ${dateSql(jour, entre(8, 20))}, ${entre(1, 3)}, NULL, NULL);`);
  if (hasard() < 0.08) sql.push(`INSERT INTO SymptomesCycles (CarnetSanteId, TypeSymptome, Date, Intensite, Commentaire, PhotoUrl) VALUES (${carnet}, N'${parmi(['Fatigue', 'Nausée', 'Autre'])}', ${dateSql(jour, entre(8, 22))}, ${entre(2, 7)}, NULL, NULL);`);

  // Activité (trois à quatre fois par semaine).
  if (hasard() < 0.45 && douleurJour < 7) {
    sql.push(`INSERT INTO DonneesActivitePhysique (CarnetSanteId, TypeActivite, Date, Duree, Intensite, Commentaire, EffetDouleur, NiveauIntensite) VALUES (${carnet}, N'${parmi(ACTIVITES)}', ${dateSql(jour, entre(7, 19), parmi([0, 15, 30, 45]))}, ${parmi([15, 20, 30, 30, 45, 60])}, ${parmi([2, 5, 5, 8])}, NULL, ${parmi([0, 0, 1, 1, 2, 3])}, NULL);`);
  }

  // Traitements : prises prévues faites (souvent un peu en retard), parfois ignorées ou oubliées.
  const repondre = (medicament, heure, h) => {
    const tirage = hasard();
    if (tirage < 0.9) prise(medicament, jour, heure, 'Pris', h, entre(0, 45));
    else if (tirage < 0.94) prise(medicament, jour, heure, 'Ignore', h + 1, 0);
  };
  if (jour <= finPilule) repondre(pilule, '21:00', 21);
  else if (jour <= fin) repondre(dienogest, '08:00', 8);
  const jourSemaine = jour.getDay();
  if (jour >= debutMagnesium && [1, 3, 5].includes(jourSemaine)) repondre(magnesium, '20:00', 20);
  if (douleurJour >= 6 && hasard() < 0.7) prise(ibuprofene, jour, null, 'Pris', entre(9, 22), entre(0, 59));
  if (jour >= debutKine && jourSemaine === 2 && hasard() < 0.85) {
    sql.push(`INSERT INTO DonneesTraitementNonMedicamenteux (CarnetSanteId, MedicamentId, Duree, Date, Commentaire) VALUES (${carnet}, ${kine}, 45, ${dateSql(jour, 18)}, NULL);`);
  }
}

sql.push('COMMIT;');
sql.push("PRINT 'Données de démonstration ajoutées.';");
process.stdout.write(`${sql.join('\n')}\n`);
