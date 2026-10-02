# Support central : correction du client d'une vente

Le portail est servi par l'API à `/support/index.html`. Il utilise les comptes existants :
`SuperAdmin` choisit un tenant ; `Admin` ne voit et ne modifie que son tenant ; `Cashier` est refusé.
Le sélecteur « Magasin » représente actuellement un tenant, pas une succursale indépendante au sein d'un tenant.
Le jeton de connexion reste uniquement en mémoire dans la page et disparaît à son rechargement.

## Parcours livré

1. Se connecter, sélectionner le magasin et rechercher le numéro de ticket.
2. Choisir le bon client, saisir un motif et vérifier le récapitulatif, dont la dette à transférer.
3. Confirmer. Le serveur contrôle à nouveau la version de la vente, le client et le montant attendu.
4. La correction, les éventuelles écritures compensatoires de crédit et les deux soldes sont enregistrés
   dans une transaction PostgreSQL sérialisable. Le stock, les paiements et les mouvements de caisse
   ne sont pas modifiés par cette action.
5. Le journal conserve l'opération, la vente, les deux identifiants clients, l'auteur, la date,
   le motif et le montant. Un renvoi avec le même identifiant ne transfère pas la dette deux fois.

Une vente déjà retournée ou un crédit dont l'affectation des règlements est ambiguë est refusé.
Le nouveau client doit être actif, appartenir au même tenant et pouvoir recevoir le crédit.
Le changement de client via l'ancienne modification de vente complète est refusé : utiliser le portail.
Les corrections successives utilisent les écritures compensatoires, sans réécrire le journal initial.

## Réception sur les postes

Le coordinateur consulte le serveur toutes les deux minutes pendant l'exécution de l'application,
ainsi qu'à la connexion et lors des synchronisations habituelles. Il termine les envois avant de
télécharger les associations vente/client, les soldes et les écritures de correction, par lots de 250 ventes.
La réception est atomique dans SQLite ; les opérations reçues ne sont pas remises dans la file d'envoi.
Un client absent du poste est créé à partir de sa fiche serveur.

La réception s'exécute également lorsque les envois échouent, dans un contexte SQLite séparé.
Elle reporte uniquement les ventes dont la vente elle-même ou les comptes concernés ont des opérations
locales non terminées (fiche client, autre vente sur le même compte, règlement ou retour), y compris en conflit.
Un conflit sur un produit ou un client sans rapport ne bloque plus toutes les corrections.
Un poste hors ligne ou fermé reçoit les corrections après sa reconnexion et la reprise de la synchronisation.
Il n'y a pas encore d'accusé de réception par poste affiché dans le portail : « enregistrée » signifie
enregistrée au serveur, et ne prouve pas que tous les PC l'ont déjà appliquée.
Les vues déjà ouvertes peuvent nécessiter un rechargement pour relire SQLite.

## Mise en service

Cette modification du dépôt ne déploie rien sur le serveur ni sur les PC existants.

1. Appliquer la migration `20260917140549_AddSaleCustomerCorrections` sur la base de déploiement.
   Elle ajoute uniquement la table d'audit et ses index ; elle ne recalcule pas les données historiques.
   `AddSaleCustomerCorrections.sql` fournit le script de cette seule migration, depuis
   `20260823160527_AddSyncOperationServerReferenceNumber`. Vérifier que les migrations précédentes sont appliquées.
2. Publier l'API avec son dossier `wwwroot/support`, derrière l'accès HTTPS habituel.
3. Distribuer une fois la nouvelle version du client MAUI aux magasins. Les anciennes versions
   ne savent pas recevoir ces corrections : les opérations suivantes ne demanderont plus une intervention par PC.
4. Se connecter sur `https://<hôte-api>/support/index.html` avec un compte autorisé.

## Validation et limites

`SupportCorrectionTests` utilise les vrais contrôleurs, JWT et PostgreSQL temporaire, puis applique
le résultat dans SQLite. Les tests couvrent les corrections payées/à crédit, les renvois,
les magasins étrangers, le rôle caissier, le support SuperAdmin, les écrans périmés, les règlements
ultérieurs, les corrections simultanées et l'application SQL de la nouvelle migration.
La compilation Windows vérifie l'intégration MAUI ; les tests ne pilotent pas un poste magasin réel.

Le navigateur a été vérifié avec `dotNetApiProBoilerplate.Api.Tests/preview-support.py`, un serveur
strictement local à données fictives. Il ne teste pas la connexion réelle du déploiement.
Ne jamais utiliser de vrais identifiants dans cet aperçu.

Ce premier parcours ne couvre pas toutes les corrections de stock, achats, retours ou règlements,
ni la résolution assistée des crédits complexes. Les accès SQL manuels ne sont pas nécessaires pour
le parcours livré. La concurrence avec tous les anciens endpoints financiers reste à auditer séparément.
La réception utilise des instantanés complets des ventes présentes sur le poste ; un flux incrémental
et des accusés de réception par appareil sont à prévoir pour de grands historiques et le suivi du support.
