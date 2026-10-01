<template>
  <DocumentLegal titre="Politique de confidentialité">
    <section>
      <p>
        MonEndo t'aide à suivre ton quotidien avec l'endométriose et à préparer tes rendez-vous médicaux. Les informations
        que tu y notes sont des données de santé : elles sont protégées par le Règlement général sur la protection des
        données (RGPD). Cette page explique simplement ce que MonEndo en fait, et surtout ce qu'il n'en fait pas.
      </p>
      <p>
        En bref : tes données servent uniquement à ton suivi. Pas de publicité, pas de revente, pas de mesure d'audience,
        pas de partage avec un laboratoire ou un assureur.
      </p>
    </section>

    <section>
      <h2>Qui est responsable de tes données ?</h2>
      <p>
        MonEndo est un projet personnel édité par {{ EDITEUR.nom }}, responsable du traitement de tes données.
        Pour toute question ou demande : <a :href="`mailto:${EDITEUR.email}`">{{ EDITEUR.email }}</a>.
      </p>
    </section>

    <section>
      <h2>Quelles données sont collectées ?</h2>
      <ul>
        <li><strong>Ton compte</strong> : ton adresse e-mail, ton mot de passe (enregistré sous une forme brouillée
          irréversible, jamais en clair) et la date à laquelle tu as donné ton accord à cette politique.</li>
        <li><strong>Ce que tu choisis de noter</strong> : douleurs, jours de règles et symptômes du cycle (dont les photos
          de suivi de l'acné), traitements et prises, séances d'activité physique, transit, bilans quotidiens (émotions,
          stress, fatigue, pas, hydratation, alimentation, notes).</li>
        <li><strong>Les rappels</strong>, si tu les actives : l'abonnement aux notifications de ton appareil (une adresse
          technique fournie par ton navigateur), l'heure choisie et ton fuseau horaire.</li>
        <li><strong>Les journaux techniques</strong> du serveur : adresse IP, date et page demandée, utiles à la sécurité
          et à la correction des pannes. Ils ne contiennent ni ton e-mail ni tes données de santé.</li>
      </ul>
      <p>
        Tes repères personnels et quelques préférences d'affichage restent sur ton appareil (stockage local du navigateur)
        et ne sont jamais envoyés au serveur. L'export PDF est fabriqué par ton navigateur, sur ton appareil.
      </p>
    </section>

    <section>
      <h2>Pourquoi ?</h2>
      <ul>
        <li>te permettre de noter et de retrouver ton suivi, d'en voir les tendances et de préparer tes rendez-vous ;</li>
        <li>t'envoyer les rappels que tu as activés ;</li>
        <li>assurer la sécurité du service (connexion, prévention des abus, correction des pannes).</li>
      </ul>
      <p>
        MonEndo ne pose aucun diagnostic et ne remplace pas l'avis d'un professionnel de santé : il montre ce que tu as
        noté, sans l'interpréter.
      </p>
    </section>

    <section>
      <h2>Sur quelle base ?</h2>
      <p>
        Tes données de santé ne sont traitées qu'avec ton <strong>consentement explicite</strong> (article 9.2.a du RGPD),
        donné à l'inscription. Tu peux le retirer à tout moment en supprimant ton compte. Les journaux techniques reposent
        sur l'intérêt légitime à sécuriser le service (article 6.1.f).
      </p>
    </section>

    <section>
      <h2>Qui peut y accéder ?</h2>
      <p>
        Toi seule : chaque compte est cloisonné et personne d'autre ne voit ton suivi. L'éditeur, qui administre le
        serveur, n'y accède que si c'est indispensable à la maintenance ou à la résolution d'un incident.
      </p>
      <p>MonEndo s'appuie sur quelques prestataires techniques, qui n'utilisent pas tes données pour leur propre compte :</p>
      <ul>
        <li><strong>OVH</strong> (France) : héberge le serveur et la base de données, en France ;</li>
        <li><strong>Microsoft Azure</strong> : stocke les photos de suivi et les sauvegardes chiffrées de la base, dans un
          centre de données en Europe ;</li>
        <li><strong>le service de notifications de ton navigateur</strong> (Apple, Google ou Mozilla selon ton appareil),
          si tu actives les rappels : il achemine des notifications chiffrées, qui ne contiennent aucune donnée de santé ;</li>
        <li><strong>Google Agenda</strong>, uniquement si ton compte est relié à un agenda : MonEndo y lit tes prochains
          rendez-vous et n'y envoie aucune donnée de santé.</li>
        <li><strong>Google (Gmail)</strong>, qui héberge la boîte e-mail de l'éditeur, uniquement si tu lui écris depuis la
          page « Une suggestion ? Un bug ? ».</li>
      </ul>
      <p>
        Tes données de santé et tes photos restent dans l'Union européenne. Apple, Google et Mozilla peuvent traiter les
        notifications et l'agenda hors de l'Union européenne, dans le cadre des garanties prévues par le RGPD
        (cadre de protection des données UE–États-Unis ou clauses contractuelles types).
      </p>
    </section>

    <section>
      <h2>Combien de temps ?</h2>
      <ul>
        <li>tes données de compte et de suivi : tant que ton compte existe ; elles sont effacées quand tu le supprimes ;</li>
        <li>un compte sans aucune connexion pendant <strong>2 ans</strong> est supprimé automatiquement, avec toutes ses
          données ;</li>
        <li>l'abonnement aux notifications : jusqu'à ce que tu désactives les rappels sur cet appareil ;</li>
        <li>les journaux techniques : <strong>30 jours</strong>, puis ils sont supprimés automatiquement ;</li>
        <li>tes e-mails à l'éditeur, si tu en envoies : supprimés une fois ta demande traitée, au plus tard
          <strong>12 mois</strong> après le dernier échange ;</li>
        <li>les sauvegardes de la base, faites chaque nuit pour pouvoir la restaurer en cas de panne : <strong>30 jours</strong>.
          Une donnée que tu effaces, ou un compte supprimé, disparaît donc aussi des sauvegardes en 30 jours au plus.</li>
      </ul>
    </section>

    <section>
      <h2>Comment sont-elles protégées ?</h2>
      <p>
        Les échanges et les sauvegardes sont chiffrés, les mots de passe ne sont jamais conservés en clair, la session repose sur des
        cookies inaccessibles aux scripts de la page, un compte est verrouillé après plusieurs mots de passe erronés et
        chaque accès aux données vérifie qu'elles appartiennent bien au compte connecté. En cas de violation de données
        présentant un risque pour toi, tu en seras informée, ainsi que la CNIL dans les 72 heures.
      </p>
    </section>

    <section>
      <h2>Cookies</h2>
      <p>
        MonEndo n'utilise que les cookies indispensables à ta connexion. Aucun cookie de publicité ni de mesure
        d'audience : c'est pourquoi aucun bandeau ne te demande ton accord.
      </p>
    </section>

    <section>
      <h2>Quand tu écris à l'éditeur</h2>
      <p>
        Depuis la page « Une suggestion ? Un bug ? », tu peux écrire à l'éditeur : ton message arrive par e-mail, avec ton
        adresse e-mail et ce que tu y joins (une capture d'écran peut montrer ce que tu as noté : tu choisis ce que tu
        envoies). Il n'est pas relié à ton compte MonEndo. Il sert uniquement à te répondre et à améliorer l'application.
      </p>
    </section>

    <section>
      <h2>Tes droits</h2>
      <p>
        Tu peux à tout moment accéder à tes données, les corriger, les effacer, les récupérer dans un format lisible
        (portabilité), en limiter l'utilisation, t'y opposer ou retirer ton consentement.
      </p>
      <ul>
        <li>depuis les <strong>Paramètres</strong> : télécharger toutes tes données, supprimer ton compte ;</li>
        <li>ou par e-mail à <a :href="`mailto:${EDITEUR.email}`">{{ EDITEUR.email }}</a> : réponse sous un mois au plus.</li>
      </ul>
      <p>
        Si tu estimes que tes droits ne sont pas respectés, tu peux adresser une réclamation à la CNIL
        (<a href="https://www.cnil.fr/fr/plaintes" rel="noopener" target="_blank">cnil.fr</a>).
      </p>
    </section>

    <section>
      <h2>Évolution de cette politique</h2>
      <p>
        Si cette politique change de façon importante, tu en seras informée dans l'application avant que le changement
        s'applique. La date de dernière mise à jour figure en haut de cette page.
      </p>
      <p>
        Voir aussi les <router-link to="/mentions-legales">mentions légales</router-link>.
      </p>
    </section>
  </DocumentLegal>
</template>

<script setup lang="ts">
import DocumentLegal from '../components/DocumentLegal.vue';
import {EDITEUR} from '../config/editeur';
</script>
