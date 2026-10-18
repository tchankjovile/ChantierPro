# ChantierPro – Frontend Mobile .NET MAUI

Application mobile professionnelle de suivi de chantier de construction (Budget, Matériaux, Main-d’œuvre et Avancement par phase).

Conforme au **Cahier des charges Version 1.0 (Octobre 2026)**, aux règles de gestion **RG-01 à RG-08**, aux principes de design **Material 3 / iOS HIG** et à l'approche **Offline-First**.

---

## 🏗️ Structure de la Solution

```
ChantierPro/
│
├── Directory.Build.props              # Configuration globale du build & version .NET ciblée (9.0 ou 10.0)
├── ChantierPro.sln                    # Solution Visual Studio / Rider
│
├── src/
│   ├── ChantierPro.Core/              # Bibliothèque métier portable (.NET Standard / .NET 8+)
│   │   ├── Models/
│   │   │   ├── Enums.cs               # Rôles, Statuts, Niveaux d'alerte, Modes de paiement
│   │   │   └── Entities.cs            # Chantier, Phase, Matériau, Ouvrier, Pointage, Dépense, Alerte...
│   │   ├── Services/
│   │   │   ├── BusinessRules.cs       # RG-01 (Avancement), RG-03 & RG-08 (Stock), RG-05 (Main-d'œuvre)
│   │   │   ├── ChantierMetricsService.cs # RG-02 (Budget consommé), RG-04 (Alertes), Santé du chantier
│   │   │   ├── ChantierTemplates.cs   # Modèles types de phases & budget (Création < 5 min)
│   │   │   ├── ChantierInsightService.cs # Synthèse indicateurs & moteur d'alertes
│   │   │   └── Labels.cs              # Formatage monétaire (FCFA / XAF), dates et libellés français
│   │   └── Abstractions/
│   │       └── IChantierRepository.cs # Contrat d'accès aux données (prêt pour SQLite / API REST)
│   │
│   └── ChantierPro.App/               # Application mobile .NET MAUI (Android, iOS, Windows)
│       ├── Controls/                  # Composants visuels réutilisables
│       │   ├── StatCard.xaml          # Carte KPI avec icône, tendance et ton sémantique
│       │   ├── ProgressGauge.xaml     # Jauge d'avancement réel vs prévu avec badge %
│       │   ├── EmptyStateView.xaml    # Écrans vides pédagogiques avec actions
│       │   └── OfflineSyncBanner.xaml # Indicateur d'état de synchronisation & mode local
│       ├── Converters/                # Convertisseurs XAML (devises, %, dates, tons)
│       ├── Helpers/
│       │   ├── Icons.cs               # Glyphes vectoriels Material Icons (2000+ icônes)
│       │   └── Palette.cs             # Couleurs sémantiques (Vert, Orange, Rouge, Bleu ardoise)
│       ├── Resources/
│       │   ├── Fonts/                 # Polices Inter (Regular, Medium, SemiBold, Bold) et MaterialIcons
│       │   ├── Styles/                # Colors.xaml (Light/Dark) et Styles.xaml
│       │   └── AppIcon / Splash       # Casque de chantier stylisé & charte graphique
│       ├── Services/
│       │   ├── DialogService.cs       # Alertes, confirmations et sélecteurs natifs
│       │   └── MockChantierRepository.cs # Jeu de données complet & réaliste (Cameroun / BTP)
│       ├── ViewModels/                # 14 ViewModels MVVM (CommunityToolkit.Mvvm)
│       └── Views/                     # 17 Écrans XAML conformes au cahier des charges
│
└── tests/
    └── ChantierPro.Tests/             # Tests unitaires xUnit couvrant les règles de gestion (100%)
```

---

## 📱 Les 17 Écrans Réalisés

| N° | Écran | Fichier XAML | Fonctionnalités clés |
|---|---|---|---|
| **1 & 2** | Connexion & Onboarding | `LoginPage.xaml` | E-mail/mdp, déverrouillage biométrique (empreinte), réinitialisation mdp. |
| **3** | Tableau de bord | `DashboardPage.xaml` | Indicateurs consolidés, bandeau alertes actives, filtres par statut, recherche, actions rapides. |
| **4** | Création / Édition Chantier | `ChantierEditPage.xaml` | Nom, client, budget, dates, GPS automatique, instanciation instantanée des 7 phases types. |
| **5** | Détail / Fiche Chantier | `ChantierDetailPage.xaml` | Score de santé /100, avancement pondéré, budget consommé vs prévu, EAC, hub des modules. |
| **6** | Liste des Phases | `PhasesPage.xaml` | Avancement global, détection des phases en retard (F-ALE-04), conformité poids 100% (RG-01). |
| **7** | Mise à jour Phase | `PhaseDetailPage.xaml` | Curseur slider 0-100%, note de traçabilité (RG-06), photo de preuve, checklist de sous-tâches. |
| **8** | Stock de Matériaux | `StockPage.xaml` | Stock temps réel, alerte rupture, prévision d'autonomie en jours (innovant), historique mouvements. |
| **9** | Nouveau Mouvement Stock | `StockMovementEditPage.xaml` | Entrée (fournisseur, BL) ou Sortie (phase), contrôle anti-stock négatif (RG-03), scan code-barres. |
| **10** | Annuaire des Ouvriers | `WorkersPage.xaml` | Fiches ouvriers, métiers, taux journaliers, appel direct en un clic. |
| **11** | Pointage Quotidien | `AttendancePage.xaml` | Pointage collectif ultra-rapide (< 1 min), calcul automatique coût journalier (RG-05), heures sup. |
| **12** | Budget & Dépenses | `BudgetPage.xaml` | Tableau budget vs réalisé par poste avec écarts en valeur et % (F-DEP-04), intégration auto (F-DEP-03). |
| **13** | Saisie d'une Dépense | `ExpenseEditPage.xaml` | Catégorie de poste, montant, fournisseur, mode de règlement (Mobile Money, Espèces...), photo justificatif. |
| **14** | Centre d'Alertes | `AlertsPage.xaml` | Alertes de budget (80% / 100%), stock critique, retards de phase, incohérences, filtres par type. |
| **15** | Rapports & Exports | `ReportsPage.xaml` | Rapport de synthèse exécutif, simulation d'export PDF et Excel, partage direct WhatsApp / Email. |
| **16** | Journal de Chantier | `JournalPage.xaml` | Météo du jour, faits marquants, incidents, visiteurs, galerie photos horodatée (avant / pendant / après). |
| **17** | Profil & Paramètres | `SettingsPage.xaml` | Thème clair/sombre, **Mode « Terrain »** (cibles tactiles 60dp), devises, langues, sauvegarde locale. |

---

## ⚙️ Règles de Gestion Implémentées (RG-01 à RG-08)

1. **RG-01 (Avancement global pondéré)** : `ProgressCalculator.GlobalProgress()` calcule $\sum (\text{avancement}_i \times \text{poids}_i) / \sum \text{poids}_i$ avec validation stricte de la somme des poids à 100%.
2. **RG-02 (Budget consommé)** : Intègre automatiquement $\text{Dépenses saisies} + \text{Matériaux sortis} + \text{Main-d'œuvre pointée}$.
3. **RG-03 (Contrôle des sorties de stock)** : Une sortie ne peut pas excéder le stock disponible, sauf accord explicite via la case de dérogation d'urgence.
4. **RG-04 (Seuils d'alerte budgétaire)** : Avertissement automatique dès 80%, alerte critique dès 100% du budget prévu, globalement et par poste.
5. **RG-05 (Coût journalier de main-d'œuvre)** : $\text{Taux journalier} \times \text{coefficient (1; 0,5; 0)} + \text{heures sup majorées à 25\%}$.
6. **RG-06 (Traçabilité & Horodatage)** : Toute modification d'avancement ou mouvement de stock enregistre date, heure et auteur.
7. **RG-07 (Chantier clôturé)** : Verrouillage strict en lecture seule dès que le statut passe à « Clôturé ».
8. **RG-08 (Prix Moyen Pondéré)** : Chaque entrée en stock réévalue le prix unitaire moyen pondéré du matériau.

---

## 🚀 Comment Compiler et Tester sur un PC avec le SDK .NET

### 1. Prérequis
- **.NET SDK** : Version 9.0 ou 10.0 (LTS) avec la charge de travail MAUI (`dotnet workload install maui`).
- **Visual Studio 2022** (17.12+) ou **JetBrains Rider** ou **VS Code** avec l'extension .NET MAUI.

### 2. Ajuster la version du SDK (si nécessaire)
Ouvrez le fichier [`Directory.Build.props`](file:///c:/Users/GENIUS%20ELECTRONICS/OneDrive%20-%20IAI%20Cameroun/Bureau/DJANGO%20projects/ChantierPro/Directory.Build.props) à la racine :
```xml
<!-- Modifiez selon le SDK installe sur votre PC de test (9.0 ou 10.0) -->
<ChantierProDotNet>10.0</ChantierProDotNet>
```

### 3. Exécuter les tests unitaires
Vérifiez la conformité de tous les calculs métier :
```bash
dotnet test tests/ChantierPro.Tests/ChantierPro.Tests.csproj
```

### 4. Lancer l'application

#### Sur Android (Émulateur ou appareil physique relié en USB) :
```bash
dotnet build src/ChantierPro.App/ChantierPro.App.csproj -t:Run -f net10.0-android
```
*(ou faites simplement **F5** dans Visual Studio avec votre appareil Android sélectionné)*

#### Sur Windows (Machine de bureau locale) :
```bash
dotnet build src/ChantierPro.App/ChantierPro.App.csproj -t:Run -f net10.0-windows10.0.19041.0
```

---

## 🎨 Design System et Accessibilité
- **Couleurs sémantiques** : Bleu ardoise (`#1F3A5F`), Orange sécurité (`#F28C28`), Vert conforme (`#1F9D61`), Rouge alerte (`#E04848`).
- **Typographie** : Famille de polices *Inter* intégrée localement pour un rendu net sur toutes les résolutions.
- **Iconographie** : Police *Material Icons* vectorielle complète (plus de 2 200 icônes intégrées, sans aucune dépendance réseau).
- **Mode « Terrain »** : Activé depuis les paramètres, il porte instantanément la taille minimale des boutons et sélecteurs de 48 dp à 60 dp pour une utilisation aisée en extérieur avec des gants.
