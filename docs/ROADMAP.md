# Roadmap AfrikaWallet / AfWal — versement direct vers Mobile Money

Décision produit : 3 octobre 2026. Statut : planifié, non validé en production.
Cette roadmap ciblée complète les livraisons existantes ; elle ne remplace pas leur historique.
Ticket de développement : [AFW-BE-MOMO-PAYOUT-1 — #223](https://github.com/innocenttchouzin2009-netizen/ProjetAfriWallet/issues/223).

## Objectif accepté

Un expéditeur de la diaspora peut envoyer de l'argent directement sur le compte Mobile Money d'un bénéficiaire en Afrique, même si celui-ci n'a aucun compte AfWal.
Destination : pays + opérateur + numéro Mobile Money + identité requise par le partenaire.
Avant confirmation : montant débité, frais, taux de change, montant exact reçu, expiration du devis et délai annoncé selon les capacités du partenaire.

## Priorité

Prochain chantier backend : AFW-BE-MOMO-PAYOUT-1, après clôture de la gate/PR en cours et main vert.
Chaque lot part d'une branche neuve et fait l'objet de tests ciblés, CI et PR. Aucun ajout dans une PR active.
Le premier lot est préparé dans le ticket #223 ; son code n'est pas encore commencé.

## Livraisons prévues

| Ordre | Jalon | Résultat attendu | Statut |
| --- | --- | --- | --- |
| 1 | AFW-BE-MOMO-PAYOUT-1 | Destination externe sans AfWal ID obligatoire ; validation pays/opérateur/numéro ; éligibilité des corridors et capacité de versement sortant | Prochain lot de code planifié |
| 2 | AFW-BE-MOMO-PAYOUT-2 | Devis lié au bénéficiaire : frais, FX, montant exact reçu, limites et expiration | Planifié |
| 3 | AFW-BE-MOMO-PAYOUT-3 | Exécution durable sandbox, réservation, idempotence et suivi fournisseur | Planifié |
| 4 | AFW-BE-MOMO-PAYOUT-4 | Confirmation fournisseur authentifiée, interrogation de statut, réconciliation et récupération des résultats inconnus | Planifié |
| 5 | AFW-MOB-MOMO-PAYOUT-1 | Parcours Flutter : bénéficiaire, devis, confirmation, suivi, historique et reçu | Planifié |
| 6 | AFW-MOMO-PAYOUT-PILOT-1 | Validation de bout en bout et activation contrôlée d'un premier corridor réel | Bloqué par les prérequis partenaires et opérationnels |

Ces identifiants sont des nouveaux lots de cette roadmap, pas des livraisons terminées.
Cible de pilote à valider : Allemagne → Cameroun, EUR → XAF, Orange Money et MTN MoMo selon couverture contractuelle.
Extension à d'autres pays de départ de la diaspora et pays africains corridor par corridor.

## Prochain lot : AFW-BE-MOMO-PAYOUT-1

1. Auditer les moteurs et contrats existants avant de choisir les fichiers à modifier.
2. Réutiliser Mobile Money Registry/connecteurs et Transfer Intent ; coordonner avec FX, Routing, Compliance, Treasury, Ledger et Settlement.
3. Distinguer collecte entrante, retrait personnel et versement sortant au bénéficiaire. Un connecteur de collecte ne prouve pas une capacité remittance/payout.
4. Modéliser une destination externe sans dépendance obligatoire à un compte AfWal du bénéficiaire.
5. Refuser corridor désactivé, opérateur non supporté et données invalides ; ne jamais substituer silencieusement un opérateur.
6. Exposer les capacités via la couche applicative/API existante après audit ; protéger les données personnelles.
7. Vérifier par tests ciblés : bénéficiaire sans compte AfWal, numéro invalide, collecte seule, corridor non supporté/désactivé, et absence de mouvement de fonds dans ce lot.

Hors périmètre du premier lot : appels financiers réels, choix contractuel final du partenaire, TikTok et modifications de la carte virtuelle.

## Critères d'acceptation du parcours complet

- Le bénéficiaire reçoit sur son Mobile Money sans création obligatoire de compte AfWal.
- L'identité du bénéficiaire est vérifiée quand le partenaire le permet ; l'interface ne prétend pas qu'elle l'est autrement.
- Seul un devis valide et accepté peut autoriser l'exécution, avec les contrôles de conformité et de fonds requis.
- « Dépôt confirmé » exige une confirmation de crédit du partenaire/opérateur ; une simple soumission ne suffit pas.
- Un timeout reste un résultat inconnu à investiguer : ni nouvelle tentative financière aveugle, ni remboursement sur la seule base du timeout.
- Requêtes/callbacks dupliqués, concurrence et redémarrages ne produisent pas de double débit ou dépôt.
- Réservations, règlement et restitutions éventuelles sont traçables dans le ledger et réconciliés.
- L'utilisateur voit le statut, la référence et le montant reçu ; les logs ne divulguent pas le numéro complet.

## Rapidité de versement — exigence acceptée le 3 octobre 2026

Référence d'expérience souhaitée par l'utilisateur : Taptap Send, pour un versement rapide application → Mobile Money. Il s'agit d'une référence d'expérience, sans partenariat AfWal/Taptap Send annoncé.
Objectif : crédit du bénéficiaire en quelques secondes lorsque les conditions le permettent. Cet objectif n'est pas une garantie universelle ni un délai de production déjà validé.

- Mesurer le délai de bout en bout depuis la confirmation de l'expéditeur dans AfWal jusqu'au crédit réel du Mobile Money ; enregistrer séparément l'heure de réception de sa confirmation par AfWal pour distinguer crédit et retard de notification.
- Si l'heure réelle de crédit n'est pas fournie, mesurer le délai jusqu'à confirmation fournisseur comme indicateur de substitution, explicitement identifié.
- Instrumenter les étapes : contrôles/réservation, soumission, traitement partenaire, crédit et callback/interrogation de statut ; ne jamais confondre réponse API rapide et argent disponible.
- Évaluer latences médiane/p95/p99, taux de succès, délais dépassés et résultats inconnus par corridor et opérateur ; inclure les opérations en attente dans le bilan.
- Afficher avant l'envoi une estimation réaliste selon corridor/opérateur, puis le statut et la confirmation de crédit. Ne pas afficher « instantané garanti » sans preuve contractuelle et mesures.
- Sélectionner le partenaire selon capacité de versement rapide, disponibilité, preuve de crédit, suivi de statut et performances mesurées ; prévoir liquidité/préfinancement selon son modèle.
- Conserver les contrôles de conformité, l'idempotence et la réconciliation ; la vitesse ne justifie aucune double exécution.

Répartition du code : prévoir les capacités de délai dans le lot 1 ; estimation affichable dans le lot 2 ; horodatages et mesures dans les lots 3–4 ; statut/estimation dans Flutter ; validation des performances de bout en bout au pilote.
Le pilote fixe les objectifs chiffrés et seuils d'alerte après essais représentatifs ; tester également retard opérateur, callback tardif et résultat inconnu.

## Reconnaissance et mémorisation du bénéficiaire — décision corrigée le 3 octobre 2026

Cette décision remplace la demande antérieure de confirmation de l'opérateur à chaque transfert.

### Premier transfert
- Dans le contexte du pays de destination choisi, l'utilisateur saisit uniquement le numéro Mobile Money ; AfWal le normalise au format international et vérifie sa structure.
- AfWal cherche à détecter automatiquement l'opérateur du wallet et le nom du titulaire Mobile Money via un service autorisé de vérification du partenaire/opérateur.
- Afficher le nom retourné (éventuellement masqué selon le contrat) et l'opérateur, par exemple MTN MoMo ou Orange Money au Cameroun, afin que l'utilisateur confirme la destination lors du premier envoi.
- Ne pas déduire le nom des contacts du téléphone, d'un annuaire ou d'un profil AfWal. Un préfixe de numéro fournit au mieux une suggestion d'opérateur et ne prouve ni le titulaire ni l'existence d'un wallet.
- La disponibilité de la résolution nom + opérateur doit être auditée et validée par corridor/partenaire ; aucune API offrant cette capacité n'est encore confirmée par cette roadmap.
- En cas de détection indisponible ou ambiguë, indiquer clairement la limite. Demander le choix de l'opérateur si nécessaire ; ne jamais présenter un nom saisi manuellement comme un nom vérifié. Si la vérification est obligatoire sur le corridor, bloquer l'envoi tant qu'elle n'aboutit pas.

### Enregistrement et paiements suivants
- Après le premier transfert confirmé réussi, enregistrer automatiquement le bénéficiaire pour cet expéditeur : pays, numéro normalisé, opérateur, nom affichable et provenance/date de vérification.
- Ne pas enregistrer comme bénéficiaire validé une simple soumission, un échec ou une opération dont le résultat est encore inconnu.
- Lors d'un paiement suivant, sélectionner le bénéficiaire enregistré ou retrouver celui-ci à partir de son numéro dans le même pays ; réutiliser son opérateur sans demander de le choisir ou confirmer à nouveau.
- Afficher les coordonnées et le nom sur le récapitulatif normal du paiement. Cette confirmation du paiement ne constitue pas une nouvelle saisie/confirmation spécifique de l'opérateur.
- Conserver les contrôles techniques d'éligibilité et, lorsque nécessaire, une vérification partenaire en arrière-plan. Ils ne doivent pas créer une demande systématique à l'utilisateur.
- En cas de changement détecté, coordonnées modifiées ou contradiction du partenaire, interrompre l'envoi et résoudre l'anomalie ; ne jamais remplacer silencieusement l'opérateur ou le titulaire.
- Lier pays, numéro et opérateur au devis ; une modification de destination invalide le devis et exige un nouveau devis.
- Enregistrement durable, isolé par expéditeur, et idempotent : callbacks dupliqués, paiements répétés et redémarrages ne créent pas de doublons. Protéger nom/numéro, limiter les accès et respecter les règles de confidentialité du partenaire.

### Prochains lots de code
AFW-BE-MOMO-PAYOUT-1 : audit des capacités de résolution nom/opérateur, normalisation, contrat de lookup autorisé et modèle de bénéficiaire enregistré ; tests avec réponses simulées explicitement identifiées.
Lot 2 : destination liée au devis.
Lots 3–4 : enregistrement automatique après succès confirmé, idempotence/durabilité et contrôles en arrière-plan.
AFW-MOB-MOMO-PAYOUT-1 : saisie du numéro, affichage du nom/opérateur détectés, premier envoi puis réutilisation sans nouvelle question sur l'opérateur.

Tests : formats local/international, pays incohérent, lookup réussi/inconnu/ambigu/indisponible, nom absent ou masqué, préfixe insuffisant, enregistrement au succès seulement, duplication de callback, isolation par expéditeur, redémarrage, paiement suivant sans confirmation spécifique d'opérateur et anomalie partenaire bloquant l'envoi.

### Détection rapide et choix manuel — précision acceptée le 3 octobre 2026

- Valider séparément les capacités de détection de l'opérateur et de récupération du nom du titulaire ; l'échec ou le retard d'une recherche ne doit pas masquer le résultat disponible de l'autre.
- Si l'opérateur n'est pas détecté rapidement, proposer immédiatement un choix manuel parmi les opérateurs Mobile Money activés pour le pays (ex. MTN MoMo / Orange Money au Cameroun).
- Définir un délai maximal d'attente de détection configurable et mesuré au pilote ; ne pas imposer d'attente indéfinie. Aucun seuil chiffré n'est encore validé.
- Le choix manuel de l'opérateur permet de poursuivre le parcours selon les exigences du corridor. Il ne vaut ni vérification du titulaire ni autorisation de contourner une vérification obligatoire.
- Présenter le nom uniquement selon la preuve disponible : retourné/vérifié par le partenaire ou indisponible. Ne pas inventer un nom si la recherche échoue.
- Une réponse de détection tardive ne remplace jamais silencieusement le choix manuel ou une destination déjà confirmée ; en cas de désaccord, demander de résoudre l'anomalie avant l'envoi.
- Après succès confirmé, mémoriser le bénéficiaire et l'opérateur choisi comme prévu ; aux paiements suivants, réutilisation sans demande systématique d'opérateur.

Tests complémentaires : opérateur détecté sans nom, nom disponible sans opérateur, détection lente/timeout, sélection manuelle, réponse tardive identique/contradictoire et respect des vérifications obligatoires du corridor.

## Activation en production

Préconditions : capacité outbound vérifiée du partenaire, contrats et autorisations du corridor, conformité, liquidité/préfinancement, frais/FX, limites, monitoring et procédure de support.
Tester succès, rejet, timeout, callback tardif/dupliqué, redémarrage, réconciliation et récupération avant activation.
Les connecteurs et documents historiques de readiness ne prouvent pas à eux seuls la disponibilité réelle du service.

## Références existantes

- [Architecture Transfer Intent](ADR-0138-unified-transfer-intent-architecture.md)
- [MTN MoMo Sandbox Connector](releases/v0.7.3/AFW-DLV-0007.3-prd.md)
