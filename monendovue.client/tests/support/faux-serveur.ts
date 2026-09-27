import type { Page } from '@playwright/test';

/** Requête reçue par une route simulée : paramètres capturés par l'expression régulière et corps JSON éventuel. */
export interface RequeteSimulee {
  params: string[];
  corps: any;
  url: URL;
}

export interface ReponseSimulee {
  status?: number;
  body?: unknown;
}

type Gestionnaire = (requete: RequeteSimulee) => ReponseSimulee | void;

interface Route {
  methode: string;
  motif: RegExp;
  gerer: Gestionnaire;
}

/**
 * Faux backend branché sur les appels XHR/fetch de la page : l'application tourne avec VITE_DOCKER=true, donc toutes ses
 * requêtes API sont relatives (`/DonneesActivitePhysique/...`) et interceptées ici. Chaque test a son propre serveur :
 * les données ajoutées pendant un test y restent jusqu'à la fin du test, puis disparaissent.
 *
 * Un appel sans route simulée reçoit une 404 et est noté dans `nonGerees` : la fixture fait alors échouer le test,
 * ce qui signale un appel oublié ou un contrat qui a changé.
 */
export class FauxServeur {
  private readonly routes: Route[] = [];
  readonly nonGerees: string[] = [];
  readonly appels: { methode: string; chemin: string; corps: any }[] = [];

  /** Déclare une route ; une route déclarée plus tard l'emporte (un test peut remplacer une route par défaut). */
  on(methode: string, motif: RegExp, gerer: Gestionnaire): this {
    this.routes.unshift({ methode, motif, gerer });
    return this;
  }

  /** Appels reçus pour une méthode et un chemin (pour vérifier ce que l'interface a envoyé). */
  appelsVers(methode: string, motif: RegExp) {
    return this.appels.filter((a) => a.methode === methode && motif.test(a.chemin));
  }

  async brancher(page: Page): Promise<void> {
    await page.route('**/*', async (route) => {
      const requete = route.request();
      const url = new URL(requete.url());
      // Seuls les appels à l'API (relatifs, donc vers le serveur Vite local) sont simulés ; les ressources tierces
      // (traductions DataTables, polices) passent normalement.
      if (!['xhr', 'fetch'].includes(requete.resourceType()) || url.hostname !== 'localhost') {
        return route.fallback();
      }

      const methode = requete.method();
      const chemin = decodeURIComponent(url.pathname.replace(/^\//, ''));
      const corps = lireCorps(requete.postData());
      this.appels.push({ methode, chemin, corps });

      const trouvee = this.routes.find((r) => r.methode === methode && r.motif.test(chemin));
      if (!trouvee) {
        this.nonGerees.push(`${methode} ${chemin}`);
        return route.fulfill({ status: 404 });
      }

      const reponse = trouvee.gerer({ params: chemin.match(trouvee.motif)!.slice(1), corps, url }) ?? {};
      return route.fulfill({
        status: reponse.status ?? 200,
        contentType: 'application/json',
        body: reponse.body === undefined ? '' : JSON.stringify(reponse.body),
      });
    });
  }
}

const lireCorps = (donnees: string | null): any => {
  if (!donnees) return undefined;
  try {
    return JSON.parse(donnees);
  } catch {
    return donnees;
  }
};

/**
 * Liste au format de l'API (ReferenceHandler.Preserve) quand le contrôleur renvoie une `List` ou un `IEnumerable` :
 * `{ $id, $values }`. Un **tableau** C# (`ToArrayAsync`) est au contraire sérialisé en simple tableau JSON : vérifier le
 * type de retour réel de l'endpoint avant de choisir.
 */
export const liste = <T>(valeurs: T[]) => ({ $id: '1', $values: valeurs });
