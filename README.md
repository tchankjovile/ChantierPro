# ChantierPro – Solution Complète Mobile & Backend BTP

Application mobile professionnelle (.NET MAUI) et backend d'entreprise (ASP.NET Core 8 Web API + Entity Framework Core 8) de suivi de chantier de construction (Budget, Matériaux, Main-d’œuvre et Avancement par phase).

Conforme au **Cahier des charges Version 1.0 (Octobre 2026)**, aux règles de gestion **RG-01 à RG-08**, aux principes de design **Material 3 / iOS HIG** et à l'approche **Offline-First**.

---

## 🏗️ Structure de la Solution

```
ChantierPro/
│
├── Directory.Build.props              # Configuration globale du build & versions ciblées
├── ChantierPro.sln                    # Solution Visual Studio / Rider (6 projets)
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
│   │       └── IChantierRepository.cs # Contrat d'accès aux données partagé
│   │
│   ├── ChantierPro.Data/              # Couche d'accès aux données EF Core 8 multi-base
│   │   ├── AppDbContext.cs            # DbContext unifié (Chantiers, Stocks, RH, Alertes, Auth)
│   │   ├── DatabaseOptions.cs         # Configuration SQLite / PostgreSQL / SQL Server
│   │   ├── DataServiceCollectionExtensions.cs
│   │   └── Entities/
│   │       ├── UserAccount.cs         # Compte utilisateur persistant avec mot de passe haché
│   │       └── RefreshToken.cs        # Jeton de rafraîchissement avec empreinte SHA-256
│   │
│   ├── ChantierPro.Api/               # Backend Web API RESTful ASP.NET Core 8
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs      # Connexion, Refresh token, Profil, Changement mot de passe
│   │   │   ├── ChantiersController.cs # CRUD chantiers, duplication, métriques, courbes d'avancement
│   │   │   ├── PhasesController.cs    # Suivi d'avancement, checklists, historique
│   │   │   ├── StockController.cs     # Matériaux, mouvements d'entrée/sortie (RG-03, RG-08)
│   │   │   ├── LabourController.cs    # Ouvriers, pointages journaliers, calculs de coût (RG-05)
│   │   │   ├── BudgetController.cs    # Lignes budgétaires prévisionnelles et dépenses
│   │   │   ├── JournalController.cs   # Journal de chantier, photos et téléversement sécurisé
│   │   │   ├── AlertsController.cs    # Centre d'alertes unifié et acquittement
│   │   │   └── UsersController.cs     # Administration des comptes et des rôles
│   │   ├── Dtos/                      # Objets de transfert de données d'API
│   │   ├── Infrastructure/            # JWT, Auth Context, Horloge, Middleware d'erreurs RFC 7807
│   │   ├── Services/
│   │   │   ├── SqlChantierRepository.cs # Implémentation EF Core complète de IChantierRepository
│   │   │   └── DataSeeder.cs          # Jeu de données d'amorçage réaliste (BTP Afrique/Cameroun)
│   │   ├── Program.cs                 # Configuration du pipeline HTTP, Swagger et JWT
│   │   └── appsettings.json           # Paramètres configurables (BDD, JWT, Uploads, Fuseau)
│   │
│   └── ChantierPro.App/               # Application mobile .NET MAUI (Android, iOS, Windows)
│       ├── Controls/                  # Composants visuels réutilisables (StatCard, Gauge...)
│       ├── Converters/                # Convertisseurs XAML
│       ├── Helpers/                   # Icônes vectorielles Material & palettes sémantiques
│       ├── Resources/                 # Polices Inter, Styles et Iconographie
│       ├── Services/                  # Services d'accès données et dialogue
│       ├── ViewModels/                # 14 ViewModels MVVM
│       └── Views/                     # 17 Écrans XAML conformes au cahier des charges
│
└── tests/
    ├── ChantierPro.Tests/             # Tests unitaires xUnit couvrant les règles métier (100%)
    └── ChantierPro.Api.Tests/         # Tests d'intégration API WebApplicationFactory
```

---

## ⚙️ Backend : Architecture & Fonctionnalités

### 1. Pile Technique
- **Framework** : ASP.NET Core 8 Web API
- **ORM** : Entity Framework Core 8
- **Fournisseurs BDD supportés** :
  - **SQLite** : Par défaut en développement local (`Data Source=chantierpro.db`).
  - **PostgreSQL** : Production haute disponibilité.
  - **SQL Server** : Intégration d'entreprise.
- **Sécurité & Authentification** :
  - JWT Bearer (JSON Web Token) avec jetons de rafraîchissement révocables en base.
  - Hachage de mot de passe sécurisé **PBKDF2 avec SHA-256**, sel cryptographique aléatoire de 16 octets et 100 000 itérations.
  - Contrôle d'accès basé sur les rôles (**RBAC**) : `Administrateur`, `ChefChantier`, `Magasinier`, `Consultant`.
- **Documentation Interactive** : Interface OpenAPI / Swagger UI intégrée.
- **Téléversement de Fichiers** : Stockage sécurisé des photos de chantier et justificatifs de dépenses avec filtrage MIME et limitation de taille.

### 2. Comptes de Démonstration (Seeding Automatique)
Lors du premier démarrage en environnement de développement, la base est automatiquement initialisée avec :
- **Administrateur** : `admin@chantierpro.cm` / `Admin#2026`
- **Chef de chantier** : `alain.mvondo@chantierpro.cm` / `Demo#2026`
- **Magasinier** : `marcelle.eyenga@chantierpro.cm` / `Demo#2026`
- **Consultant (Audit)** : `audit@veritas.cm` / `Demo#2026`

---

## 🚀 Démarrage du Backend

Dès qu'un SDK .NET 8 (ou supérieur) est présent sur votre poste :

```bash
# Se placer dans le dossier de l'API
cd src/ChantierPro.Api

# Lancer le serveur backend
dotnet run
```

L'API est accessible par défaut sur `http://localhost:5080` :
- **Swagger UI** : `http://localhost:5080/swagger`
- **Endpoint de connexion** : `POST http://localhost:5080/api/auth/login`

Pour lancer la suite de tests automatisés (règles métier + API) :
```bash
dotnet test ChantierPro.sln
```
