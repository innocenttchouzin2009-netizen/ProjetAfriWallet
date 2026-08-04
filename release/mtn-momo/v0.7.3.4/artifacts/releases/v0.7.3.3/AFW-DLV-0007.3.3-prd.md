# PRD — AFW-DLV-0007.3.3

**Titre :** Flutter MTN MoMo Experience

**Objectif :**
Créer une expérience Flutter complète, premium et cohérente pour les opérations MTN MoMo, connectée au backend sandbox d'AfriWallet.

## Contexte métier

AfriWallet doit offrir une expérience utilisateur fluide, intuitive et élégante pour les opérations Mobile Money, afin de renforcer la confiance des utilisateurs et préparer la prochaine génération d'intégrations de paiement.

## User Story

> En tant qu'utilisateur AfriWallet,
> 
> je souhaite effectuer un dépôt ou un retrait MTN MoMo via une interface claire et premium,
> 
> afin de suivre mes transactions avec simplicité et confiance.

## Fonctionnalités attendues

Le système doit permettre :

- d'afficher l'accueil MTN MoMo ;
- d'effectuer un dépôt ;
- d'effectuer un retrait ;
- de valider le numéro et le montant saisis ;
- de confirmer une transaction avant envoi ;
- de suivre l'état d'une transaction en temps réel ;
- d'actualiser une transaction ;
- de consulter l'historique et le détail d'une transaction ;
- d'afficher un reçu de transaction ;
- de gérer les états chargement, erreur et historique vide ;
- d'utiliser un repository API et un repository de démonstration selon le contexte d'exécution.

## Critères d'acceptation

- Les écrans de dépôt, retrait, historique et détail sont présents.
- L'expérience inclut des composants premium, des animations et des états de chargement.
- Les flux sont connectés au backend sandbox d'AfriWallet.
- Les tests widget, repository, controller et navigation sont présents.
- La documentation utilisateur et développeur est fournie.

## Hors périmètre

Cette livraison n'inclut pas :

- l'intégration avec des services MTN réels ;
- les identifiants ou secrets d'authentification de production ;
- l'activation de transactions réelles sur un réseau externe.

## Décision d'architecture

L'expérience Flutter MTN MoMo doit être pensée comme une couche de présentation riche et cohérente, reposant sur la logique backend déjà introduite, afin de garantir :

- une expérience utilisateur premium ;
- un niveau de qualité élevé ;
- une séparation claire entre logique métier, services et interface.
