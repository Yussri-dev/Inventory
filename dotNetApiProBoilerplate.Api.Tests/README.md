# Tests API et PostgreSQL de synchronisation

Ces tests passent par le vrai SyncBatchController, la validation JWT, les services et les repositories.
ASP.NET Core TestServer transporte les requêtes HTTP en mémoire ; la base utilise un vrai processus PostgreSQL 17.

## Exécution sur Windows

    dotnet test dotNetApiProBoilerplate.Api.Tests/dotNetApiProBoilerplate.Api.Tests.csproj --logger "console;verbosity=normal"

PostgreSQL doit être installé. Le chemin par défaut est C:\Program Files\PostgreSQL\17\bin.
La variable INVENTORY_TEST_PG_BIN permet de choisir un autre répertoire contenant initdb.exe et pg_ctl.exe.
Le processus doit pouvoir lancer initdb et PostgreSQL (le bac à sable restreint de Codex ne le permet pas).

Chaque exécution initialise son propre cluster dans le dossier temporaire InventoryPgTests-<GUID>,
écoute uniquement sur 127.0.0.1 sur un port choisi dynamiquement et arrête le serveur à la fin.
Les dossiers temporaires et postgres.log sont conservés pour diagnostic.
Aucune chaîne de connexion de production et aucune base existante ne sont utilisées.
Le cluster contient seulement des données de test ; son authentification locale utilise trust.

## Scénarios

- Absence de JWT : réponse HTTP 401.
- Réponse volontairement ignorée après commit puis renvoi : une vente, un paiement et une déduction de stock.
- Même opération avec contenu différent : Conflict.
- Cinq renvois simultanés : un Done et quatre Duplicate.
- Vente invalide après création du brouillon : aucune écriture métier résiduelle.
- Renvoi du même conflit : même rejet, sans consommation persistante du numéro de document.
- Exception injectée après les écritures du vrai SaleService : transaction annulée, puis reprise réussie.
- Renvoi de création de client sur le chemin batch : un seul client.
- Session de caisse ou produit appartenant à une autre entreprise : rejet.
- Même ClientOperationId dans deux entreprises : opérations indépendantes.

Les assertions contrôlent ventes, lignes, paiements, mouvements de stock, mouvements de caisse et traces de synchronisation.
Une vente de 2 unités fait passer le stock de 10 à 8, sans nouvelle déduction au renvoi.

## Défaut reproduit et corrigé

Avant correction, SyncOperationExecutor committait les modifications du handler lorsque celui-ci retournait Conflict.
SaleService avait déjà sauvegardé une vente Pending avant de détecter un produit inexistant.
Le test Invalid_sale_rolls_back_business_writes échouait avec 1 vente au lieu de 0.

Un savepoint est maintenant créé après la réservation de ClientOperationId.
En cas de Conflict, toutes les écritures métier suivantes sont annulées et le ChangeTracker est vidé.
Seule la trace du conflit est conservée, afin de garder un résultat stable pour les renvois.

## Scénarios métier supplémentaires

PostgreSqlBusinessTests couvre les achats impayés, partiellement payés et soldés, avec renvoi
du même identifiant : stock, dette fournisseur et journal fournisseur sont vérifiés ensemble.
Un achat cash sans fonds est refusé sans achat ni modification du stock.
Les retours vérifient la quantité cumulée (y compris les doublons dans une requête), le prix
du ticket, la conversion historique en unités de stock, le renvoi et la clôture de caisse.
Les paiements clients, remboursements de crédit et refus de surpaiement sont également exercés.

Corrections : l'achat complet inscrit maintenant sa dette et son paiement au journal fournisseur.
Le retour complet contrôle ses lignes contre la vente avant de créer ses effets métier,
réapprovisionne les unités réellement vendues et extrait la TVA du montant TTC.

## Limites

Le schéma de test est créé avec EnsureCreated : les migrations de mise à niveau ne sont pas validées ici.
Les tests ne lancent pas le client MAUI et ne coupent pas physiquement le réseau ni le processus serveur.
Ils ne remplacent pas les tests de concurrence entre opérations différentes sur un même stock,
une même vente retournée ou un même solde, ni une validation de tous les parcours MAUI.
Les soldes fournisseurs historiques déjà erronés ne sont pas recalculés par cette correction.
