# Synchronisation des magasins

- Chaque poste utilise son horloge et son fuseau local : vérifier leur configuration Windows.
- La planification vérifie chaque minute si l'échéance de 21 h est atteinte. Cette vérification seule ne contacte pas le serveur.
- La date d'activation et la dernière tentative quotidienne sont conservées dans les préférences du poste, séparément pour chaque magasin (TenantId).
- L'application doit être ouverte et le magasin connecté. Une échéance manquée pendant la fermeture ou sans Internet est rattrapée à la prochaine ouverture/connectivité. Plusieurs jours manqués produisent une seule synchronisation.
- Une tentative ayant démarré, même partielle, consomme l'échéance quotidienne pour éviter de répéter les conflits chaque minute. Après une erreur API ou une interruption pendant l'exécution, utiliser le bouton manuel, ou attendre la prochaine échéance.
- Menu **Synchronisation** → **Synchroniser tout** lance immédiatement le même parcours, sans consommer l'échéance de 21 h. Les lancements sont sérialisés sur le poste.
- Le parcours envoie les prérequis, références, achats, ventes, retours, paiements et clôtures, puis récupère les références et stocks. Les corrections serveur sont récupérées même si un autre envoi échoue ; les modifications locales liées restent protégées.
- Une file bloquée ou un téléchargement échoué affiche une synchronisation partielle. Les conflits ne sont jamais effacés automatiquement.
- L'initialisation des données minimales d'un nouveau poste reste immédiate.

Validation : tests `DailySyncScheduleTests` pour les limites de 21 h, la persistance de la dernière tentative et le rattrapage. Pour valider l'interface, reconstruire et relancer MAUI, puis ouvrir le menu Synchronisation.
