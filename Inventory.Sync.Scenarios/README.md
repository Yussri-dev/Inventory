# Scénarios de synchronisation

Exécution observée : quatre scénarios PASS, code de sortie 0.

## Périmètre

Vraie base SQLite en mémoire créée avec PosLocalDbContext.EnsureCreatedAsync.
Vrais LocalSaleService, SyncQueueService, SaleLocalSyncPayloadBuilder, LocalSaleSyncMapper,
LocalBulkSyncUploader et SaleLocalSyncResultHandler.
API simulée : aucune connexion HTTP ou donnée réelle utilisée.

Une vente de 2 unités à 10 est créée par LocalSaleService, avec un stock initial de 10.
Les tests vérifient le stock final de 8, une seule vente et un seul paiement,
la conservation du ClientOperationId, la libération des verrous après échec,
le délai de nouvelle tentative, le rapprochement des identifiants serveur et
le non-renvoi des opérations terminées.

## Résultats

- PASS offline-reconnect : échec réseau, puis succès à la reconnexion.
- PASS lost-response-duplicate : réponse perdue après acceptation simulée, puis réponse Duplicate acceptée.
- PASS cancel-retry : annulation pendant le transfert, libération du lot et reprise.
- PASS crash-after-claim : nouveau DbContext après réservation du lot, récupération par
  la vraie méthode RecoverInterruptedQueueItemsAsync de LocalSyncUploader, puis transfert réussi.

Le délai de retry est avancé dans la base de test pour éviter une attente réelle.
Le dernier test simule une perte du contexte après réservation, pas un arrêt forcé du processus.
Il appelle par réflexion la méthode privée de récupération dans la DLL MAUI compilée,
sans construire les services réseau de cette classe. Il nécessite donc une compilation
Windows à jour et ne valide pas le déclenchement automatique du coordinateur.

## Exécution

Depuis la racine du dépôt :

    dotnet build Inventory.Ui/Inventory.Ui.csproj -f net9.0-windows10.0.19041.0 --no-restore
    dotnet run --project Inventory.Sync.Scenarios/Inventory.Sync.Scenarios.csproj

## Limites

Ces tests ne valident pas la déduplication PostgreSQL réelle, le traitement serveur des ventes,
les migrations existantes, la concurrence multi-caisses ou le parcours graphique MAUI.
Le moteur Docker était indisponible lors de la vérification.
Les réponses serveur et leur idempotence sont simulées : le scénario Duplicate prouve
la bonne réaction du client, pas une garantie de non-duplication du serveur.