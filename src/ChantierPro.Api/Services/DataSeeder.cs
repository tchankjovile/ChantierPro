using ChantierPro.Api.Infrastructure;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Data;
using ChantierPro.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChantierPro.Api.Services;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminName { get; set; } = "Administrateur";
    public string AdminEmail { get; set; } = "admin@chantierpro.cm";
    public string AdminPassword { get; set; } = "Admin#2026";
    public bool DemoData { get; set; } = true;
    public string DemoPassword { get; set; } = "Demo#2026";
}

public class DataSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly SeedOptions _options;
    private readonly IClock _clock;

    public DataSeeder(
        AppDbContext db,
        IPasswordHasher hasher,
        IOptions<SeedOptions> options,
        IClock clock)
    {
        _db = db;
        _hasher = hasher;
        _options = options.Value;
        _clock = clock;
    }

    public async Task SeedAsync()
    {
        // 1. Utilisateurs
        if (!await _db.Users.AnyAsync())
        {
            var admin = new UserAccount
            {
                Id = Guid.NewGuid(),
                Name = _options.AdminName,
                Email = _options.AdminEmail.Trim().ToLowerInvariant(),
                PasswordHash = _hasher.HashPassword(_options.AdminPassword),
                Role = UserRole.Administrateur,
                IsActive = true,
                CreatedAt = _clock.UtcNow
            };
            _db.Users.Add(admin);

            if (_options.DemoData)
            {
                var chef = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    Name = "Alain Mvondo",
                    Email = "alain.mvondo@chantierpro.cm",
                    PasswordHash = _hasher.HashPassword(_options.DemoPassword),
                    Role = UserRole.ChefChantier,
                    IsActive = true,
                    CreatedAt = _clock.UtcNow
                };

                var magasinier = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    Name = "Marcelle Eyenga",
                    Email = "marcelle.eyenga@chantierpro.cm",
                    PasswordHash = _hasher.HashPassword(_options.DemoPassword),
                    Role = UserRole.Magasinier,
                    IsActive = true,
                    CreatedAt = _clock.UtcNow
                };

                var consultant = new UserAccount
                {
                    Id = Guid.NewGuid(),
                    Name = "Bureau Véritas Audit",
                    Email = "audit@veritas.cm",
                    PasswordHash = _hasher.HashPassword(_options.DemoPassword),
                    Role = UserRole.Consultant,
                    IsActive = true,
                    CreatedAt = _clock.UtcNow
                };

                _db.Users.AddRange(chef, magasinier, consultant);
            }

            await _db.SaveChangesAsync();
        }

        // 2. Données de démonstration
        if (_options.DemoData && !await _db.Chantiers.AnyAsync())
        {
            // Fournisseurs
            var supCiment = new Supplier { Id = Guid.NewGuid(), Name = "Cimenteries du Golfe", Phone = "+237 670 11 22 33", Email = "contact@cimenteriesgolfe.cm", Address = "Zone Industrielle Bassa, Douala" };
            var supAcier = new Supplier { Id = Guid.NewGuid(), Name = "Aciéries & Métaux SA", Phone = "+237 699 44 55 66", Email = "ventes@acieries.cm", Address = "Zone Portuaire, Douala" };
            var supCarriere = new Supplier { Id = Guid.NewGuid(), Name = "Carrières du Littoral", Phone = "+237 677 88 99 00", Email = "info@carrieres-littoral.cm", Address = "Dibamba" };
            var supElec = new Supplier { Id = Guid.NewGuid(), Name = "ElecPro Cameroun", Phone = "+237 655 12 34 56", Email = "commandes@elecpro.cm", Address = "Akwa, Douala" };
            _db.Suppliers.AddRange(supCiment, supAcier, supCarriere, supElec);

            // Matériaux
            var matCiment = new Material { Id = Guid.NewGuid(), Name = "Ciment CPJ 42.5", Category = "Liants", Unit = "Sac 50kg", UnitPrice = 5200m, AlertThreshold = 80m, Barcode = "370001234501" };
            var matFer12 = new Material { Id = Guid.NewGuid(), Name = "Fer à béton HA Ø12", Category = "Aciers", Unit = "Barre 12m", UnitPrice = 6800m, AlertThreshold = 50m, Barcode = "370001234502" };
            var matSable = new Material { Id = Guid.NewGuid(), Name = "Sable Sanaga fin", Category = "Granulats", Unit = "Camion 10m³", UnitPrice = 95000m, AlertThreshold = 2m, Barcode = "370001234503" };
            var matGravier = new Material { Id = Guid.NewGuid(), Name = "Gravier 15/25 concassé", Category = "Granulats", Unit = "Camion 10m³", UnitPrice = 160000m, AlertThreshold = 2m, Barcode = "370001234504" };
            var matParpaing = new Material { Id = Guid.NewGuid(), Name = "Agglos pleins 15x20x40", Category = "Maçonnerie", Unit = "Unité", UnitPrice = 320m, AlertThreshold = 300m, Barcode = "370001234505" };
            var matPeinture = new Material { Id = Guid.NewGuid(), Name = "Peinture acrylique blanche", Category = "Finitions", Unit = "Fut 20L", UnitPrice = 28000m, AlertThreshold = 10m, Barcode = "370001234506" };
            _db.Materials.AddRange(matCiment, matFer12, matSable, matGravier, matParpaing, matPeinture);

            // Ouvriers
            var w1 = new Worker { Id = Guid.NewGuid(), Name = "Jean-Paul Ewane", Trade = "Chef d'équipe / Coffreur", Phone = "+237 671 22 33 44", DailyRate = 15000m, Status = WorkerStatus.Actif };
            var w2 = new Worker { Id = Guid.NewGuid(), Name = "Samuel Mbida", Trade = "Maçon qualifié", Phone = "+237 692 33 44 55", DailyRate = 10000m, Status = WorkerStatus.Actif };
            var w3 = new Worker { Id = Guid.NewGuid(), Name = "Eric Talla", Trade = "Ferrailleur", Phone = "+237 673 44 55 66", DailyRate = 9500m, Status = WorkerStatus.Actif };
            var w4 = new Worker { Id = Guid.NewGuid(), Name = "Alphonse Kamga", Trade = "Électricien bâtiment", Phone = "+237 694 55 66 77", DailyRate = 12000m, Status = WorkerStatus.Actif };
            var w5 = new Worker { Id = Guid.NewGuid(), Name = "Didier Ntsama", Trade = "Plombier", Phone = "+237 675 66 77 88", DailyRate = 11000m, Status = WorkerStatus.Actif };
            var w6 = new Worker { Id = Guid.NewGuid(), Name = "Pauline Kouam", Trade = "Peintre applicateur", Phone = "+237 696 77 88 99", DailyRate = 9000m, Status = WorkerStatus.Actif };
            var w7 = new Worker { Id = Guid.NewGuid(), Name = "Cédric Fopa", Trade = "Manœuvre polyvalent", Phone = "+237 677 88 99 10", DailyRate = 6000m, Status = WorkerStatus.Actif };
            var w8 = new Worker { Id = Guid.NewGuid(), Name = "Serge Bisseck", Trade = "Manœuvre polyvalent", Phone = "+237 698 99 10 11", DailyRate = 6000m, Status = WorkerStatus.Actif };
            _db.Workers.AddRange(w1, w2, w3, w4, w5, w6, w7, w8);

            // Chantier 1 : Résidence Les Palmiers
            var c1 = new Chantier
            {
                Id = Guid.NewGuid(),
                Reference = "CH-2026-001",
                Name = "Résidence Les Palmiers",
                Client = "SCI Palm Promotion",
                Address = "Boulevard de la Liberté, Akwa, Douala",
                Latitude = 4.051056,
                Longitude = 9.767868,
                StartDate = _clock.Today.AddMonths(-3),
                PlannedEndDate = _clock.Today.AddMonths(3),
                TotalBudget = 145000000m,
                Status = ChantierStatus.EnCours,
                Description = "Construction d'un immeuble R+3 à usage mixte résidentiel et commercial avec sous-sol de parking."
            };
            _db.Chantiers.Add(c1);

            var phasesC1 = ChantierTemplates.DefaultPhases(c1.Id, c1.StartDate, c1.PlannedEndDate, c1.TotalBudget);
            phasesC1[0].Progress = 100m;
            phasesC1[1].Progress = 100m;
            phasesC1[2].Progress = 85m;
            phasesC1[3].Progress = 40m;
            phasesC1[4].Progress = 20m;
            phasesC1[5].Progress = 0m;
            phasesC1[6].Progress = 0m;
            _db.Phases.AddRange(phasesC1);

            var budgetLinesC1 = ChantierTemplates.DefaultBudgetLines(c1.Id, c1.TotalBudget);
            _db.BudgetLines.AddRange(budgetLinesC1);

            // Mouvements stock C1
            _db.StockMovements.AddRange(
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matCiment.Id, Type = MovementType.Entree, Quantity = 500m, UnitPrice = 5200m, SupplierId = supCiment.Id, Date = _clock.Today.AddDays(-60), DeliveryNoteNumber = "BL-5821", Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matCiment.Id, Type = MovementType.Sortie, Quantity = 430m, UnitPrice = 5200m, PhaseId = phasesC1[2].Id, Date = _clock.Today.AddDays(-20), Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matFer12.Id, Type = MovementType.Entree, Quantity = 300m, UnitPrice = 6800m, SupplierId = supAcier.Id, Date = _clock.Today.AddDays(-55), DeliveryNoteNumber = "BL-4410", Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matFer12.Id, Type = MovementType.Sortie, Quantity = 260m, UnitPrice = 6800m, PhaseId = phasesC1[2].Id, Date = _clock.Today.AddDays(-15), Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matSable.Id, Type = MovementType.Entree, Quantity = 12m, UnitPrice = 95000m, SupplierId = supCarriere.Id, Date = _clock.Today.AddDays(-40), DeliveryNoteNumber = "BL-1092", Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matSable.Id, Type = MovementType.Sortie, Quantity = 10m, UnitPrice = 95000m, PhaseId = phasesC1[2].Id, Date = _clock.Today.AddDays(-10), Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matGravier.Id, Type = MovementType.Entree, Quantity = 8m, UnitPrice = 160000m, SupplierId = supCarriere.Id, Date = _clock.Today.AddDays(-35), DeliveryNoteNumber = "BL-1140", Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matGravier.Id, Type = MovementType.Sortie, Quantity = 7m, UnitPrice = 160000m, PhaseId = phasesC1[2].Id, Date = _clock.Today.AddDays(-8), Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matParpaing.Id, Type = MovementType.Entree, Quantity = 3500m, UnitPrice = 320m, Date = _clock.Today.AddDays(-30), DeliveryNoteNumber = "BL-9812", Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c1.Id, MaterialId = matParpaing.Id, Type = MovementType.Sortie, Quantity = 3100m, UnitPrice = 320m, PhaseId = phasesC1[2].Id, Date = _clock.Today.AddDays(-5), Author = "Alain Mvondo" }
            );

            // Pointages C1
            for (int i = 0; i < 7; i++)
            {
                var day = _clock.Today.AddDays(-i);
                _db.Attendances.AddRange(
                    new Attendance { Id = Guid.NewGuid(), ChantierId = c1.Id, WorkerId = w1.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w1.DailyRate, OvertimeHours = 1m },
                    new Attendance { Id = Guid.NewGuid(), ChantierId = c1.Id, WorkerId = w2.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w2.DailyRate, OvertimeHours = 0m },
                    new Attendance { Id = Guid.NewGuid(), ChantierId = c1.Id, WorkerId = w3.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w3.DailyRate, OvertimeHours = 2m },
                    new Attendance { Id = Guid.NewGuid(), ChantierId = c1.Id, WorkerId = w7.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w7.DailyRate, OvertimeHours = 0m },
                    new Attendance { Id = Guid.NewGuid(), ChantierId = c1.Id, WorkerId = w8.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w8.DailyRate, OvertimeHours = 0m }
                );
            }

            // Dépenses C1
            _db.Expenses.AddRange(
                new Expense { Id = Guid.NewGuid(), ChantierId = c1.Id, Category = BudgetCategory.LocationEngins, Amount = 6500000m, Date = _clock.Today.AddDays(-70), Supplier = "Engins BTP Littoral", Label = "Location tractopelle & pelleteuse 15j", PaymentMode = PaymentMode.Virement, Author = "Alain Mvondo" },
                new Expense { Id = Guid.NewGuid(), ChantierId = c1.Id, Category = BudgetCategory.LocationEngins, Amount = 3800000m, Date = _clock.Today.AddDays(-30), Supplier = "Grue Express", Label = "Location grue mobile coulée dalle", PaymentMode = PaymentMode.Virement, Author = "Alain Mvondo" },
                new Expense { Id = Guid.NewGuid(), ChantierId = c1.Id, Category = BudgetCategory.Transport, Amount = 2100000m, Date = _clock.Today.AddDays(-45), Supplier = "Transporteurs Unis", Label = "Évacuation des déblais et terres", PaymentMode = PaymentMode.MobileMoney, Author = "Alain Mvondo" },
                new Expense { Id = Guid.NewGuid(), ChantierId = c1.Id, Category = BudgetCategory.Outillage, Amount = 1450000m, Date = _clock.Today.AddDays(-65), Supplier = "Quincaillerie Centrale", Label = "Aiguilles vibrantes, brouettes, pelles", PaymentMode = PaymentMode.Especes, Author = "Alain Mvondo" },
                new Expense { Id = Guid.NewGuid(), ChantierId = c1.Id, Category = BudgetCategory.SousTraitance, Amount = 4800000m, Date = _clock.Today.AddDays(-15), Supplier = "Charpente Bois & Métal", Label = "Acompte 40% charpente métallique", PaymentMode = PaymentMode.Virement, Author = "Alain Mvondo" }
            );

            // Journal C1
            _db.JournalEntries.AddRange(
                new JournalEntry { Id = Guid.NewGuid(), ChantierId = c1.Id, Date = _clock.Today, Weather = WeatherKind.Ensoleille, Note = "Coulage du dernier chaînage haut. Équipe motivée, aucun incident à signaler. Contrôle armature validé par le bureau d'études.", Incident = "Néant", Visitors = "M. Tcheuko (Maître d'œuvre)", Author = "Alain Mvondo" },
                new JournalEntry { Id = Guid.NewGuid(), ChantierId = c1.Id, Date = _clock.Today.AddDays(-1), Weather = WeatherKind.Pluie, Note = "Arrêt de la coulée pendant 2h en raison de fortes averses tropicales. Reprise à 14h.", Incident = "Pluie torrentielle l'après-midi", Visitors = "Aucun", Author = "Alain Mvondo" }
            );

            // Chantier 2 : Immeuble Horizon 2026
            var c2 = new Chantier
            {
                Id = Guid.NewGuid(),
                Reference = "CH-2026-002",
                Name = "Immeuble Horizon 2026",
                Client = "Groupe Horizon Immobilier",
                Address = "Quartier Bastos, Yaoundé",
                Latitude = 3.8912,
                Longitude = 11.5123,
                StartDate = _clock.Today.AddDays(-20),
                PlannedEndDate = _clock.Today.AddMonths(10),
                TotalBudget = 280000000m,
                Status = ChantierStatus.EnCours,
                Description = "Ensemble immobilier haut standing de bureaux et appartements."
            };
            _db.Chantiers.Add(c2);

            var phasesC2 = ChantierTemplates.DefaultPhases(c2.Id, c2.StartDate, c2.PlannedEndDate, c2.TotalBudget);
            phasesC2[0].Progress = 90m;
            phasesC2[1].Progress = 15m;
            _db.Phases.AddRange(phasesC2);
            _db.BudgetLines.AddRange(ChantierTemplates.DefaultBudgetLines(c2.Id, c2.TotalBudget));

            _db.StockMovements.AddRange(
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c2.Id, MaterialId = matCiment.Id, Type = MovementType.Entree, Quantity = 200m, UnitPrice = 5200m, SupplierId = supCiment.Id, Date = _clock.Today.AddDays(-15), DeliveryNoteNumber = "BL-7711", Author = "Alain Mvondo" },
                new StockMovement { Id = Guid.NewGuid(), ChantierId = c2.Id, MaterialId = matSable.Id, Type = MovementType.Entree, Quantity = 5m, UnitPrice = 95000m, SupplierId = supCarriere.Id, Date = _clock.Today.AddDays(-12), DeliveryNoteNumber = "BL-7714", Author = "Alain Mvondo" }
            );

            // Chantier 3 : Villa Océane Kribi
            var c3 = new Chantier
            {
                Id = Guid.NewGuid(),
                Reference = "CH-2026-003",
                Name = "Villa Océane Kribi",
                Client = "Dr. Roger Manga",
                Address = "Plage de Grand Batanga, Kribi",
                Latitude = 2.9234,
                Longitude = 9.8976,
                StartDate = _clock.Today.AddDays(15),
                PlannedEndDate = _clock.Today.AddMonths(7),
                TotalBudget = 75000000m,
                Status = ChantierStatus.Planifie,
                Description = "Villa balnéaire R+1 avec piscine à débordement et aménagements paysagers."
            };
            _db.Chantiers.Add(c3);
            _db.Phases.AddRange(ChantierTemplates.DefaultPhases(c3.Id, c3.StartDate, c3.PlannedEndDate, c3.TotalBudget));
            _db.BudgetLines.AddRange(ChantierTemplates.DefaultBudgetLines(c3.Id, c3.TotalBudget));

            await _db.SaveChangesAsync();

            // Génération des alertes initiales
            var materialsList = await _db.Materials.ToListAsync();
            foreach (var ch in new[] { c1, c2, c3 })
            {
                var phases = await _db.Phases.Where(p => p.ChantierId == ch.Id).Include(p => p.Tasks).Include(p => p.History).ToListAsync();
                var budget = await _db.BudgetLines.Where(b => b.ChantierId == ch.Id).ToListAsync();
                var exp = await _db.Expenses.Where(e => e.ChantierId == ch.Id).ToListAsync();
                var mov = await _db.StockMovements.Where(m => m.ChantierId == ch.Id).ToListAsync();
                var att = await _db.Attendances.Where(a => a.ChantierId == ch.Id).ToListAsync();
                var agg = new ChantierAggregate
                {
                    Chantier = ch,
                    Phases = phases,
                    BudgetLines = budget,
                    Expenses = exp,
                    Movements = mov,
                    Attendances = att
                };
                var insight = ChantierInsightService.Analyze(agg, materialsList, _clock.Today);
                _db.Alerts.AddRange(insight.Alerts);
            }
            await _db.SaveChangesAsync();
        }
    }
}
