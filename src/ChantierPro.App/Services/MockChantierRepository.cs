using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;

namespace ChantierPro.Mobile.Services;

public class MockChantierRepository : IChantierRepository
{
    private readonly List<ChantierAggregate> _chantiers = new();
    private readonly List<Material> _materials = new();
    private readonly List<Supplier> _suppliers = new();
    private readonly List<Worker> _workers = new();
    private readonly List<AlertItem> _alerts = new();

    public AppUser CurrentUser { get; } = new()
    {
        Id = Guid.NewGuid(),
        Name = "Alain Mvondo",
        Email = "alain.mvondo@chantierpro.cm",
        Role = UserRole.ChefChantier
    };

    public MockChantierRepository()
    {
        SeedData();
    }

    private void SeedData()
    {
        // 1. Fournisseurs
        var supCiment = new Supplier { Id = Guid.NewGuid(), Name = "Cimenteries du Golfe", Phone = "+237 670 11 22 33", Email = "contact@cimenteriesgolfe.cm", Address = "Zone Industrielle Bassa, Douala" };
        var supAcier = new Supplier { Id = Guid.NewGuid(), Name = "Aciéries & Métaux SA", Phone = "+237 699 44 55 66", Email = "ventes@acieries.cm", Address = "Zone Portuaire, Douala" };
        var supCarriere = new Supplier { Id = Guid.NewGuid(), Name = "Carrières du Littoral", Phone = "+237 677 88 99 00", Email = "info@carrieres-littoral.cm", Address = "Dibamba" };
        var supElec = new Supplier { Id = Guid.NewGuid(), Name = "ElecPro Cameroun", Phone = "+237 655 12 34 56", Email = "commandes@elecpro.cm", Address = "Akwa, Douala" };
        _suppliers.AddRange(new[] { supCiment, supAcier, supCarriere, supElec });

        // 2. Matériaux de base
        var matCiment = new Material { Id = Guid.NewGuid(), Name = "Ciment CPJ 42.5", Category = "Liants", Unit = "Sac 50kg", UnitPrice = 5200m, AlertThreshold = 80m, Barcode = "370001234501" };
        var matFer12 = new Material { Id = Guid.NewGuid(), Name = "Fer à béton HA Ø12", Category = "Aciers", Unit = "Barre 12m", UnitPrice = 6800m, AlertThreshold = 50m, Barcode = "370001234502" };
        var matSable = new Material { Id = Guid.NewGuid(), Name = "Sable Sanaga fin", Category = "Granulats", Unit = "Camion 10m³", UnitPrice = 95000m, AlertThreshold = 2m, Barcode = "370001234503" };
        var matGravier = new Material { Id = Guid.NewGuid(), Name = "Gravier 15/25 concassé", Category = "Granulats", Unit = "Camion 10m³", UnitPrice = 160000m, AlertThreshold = 2m, Barcode = "370001234504" };
        var matParpaing = new Material { Id = Guid.NewGuid(), Name = "Agglos pleins 15x20x40", Category = "Maçonnerie", Unit = "Unité", UnitPrice = 320m, AlertThreshold = 300m, Barcode = "370001234505" };
        var matPeinture = new Material { Id = Guid.NewGuid(), Name = "Peinture acrylique blanche", Category = "Finitions", Unit = "Fut 20L", UnitPrice = 28000m, AlertThreshold = 10m, Barcode = "370001234506" };
        _materials.AddRange(new[] { matCiment, matFer12, matSable, matGravier, matParpaing, matPeinture });

        // 3. Ouvriers
        var w1 = new Worker { Id = Guid.NewGuid(), Name = "Jean-Paul Ewane", Trade = "Chef d'équipe / Coffreur", Phone = "+237 671 22 33 44", DailyRate = 15000m, Status = WorkerStatus.Actif };
        var w2 = new Worker { Id = Guid.NewGuid(), Name = "Samuel Mbida", Trade = "Maçon qualifié", Phone = "+237 692 33 44 55", DailyRate = 10000m, Status = WorkerStatus.Actif };
        var w3 = new Worker { Id = Guid.NewGuid(), Name = "Eric Talla", Trade = "Ferrailleur", Phone = "+237 673 44 55 66", DailyRate = 9500m, Status = WorkerStatus.Actif };
        var w4 = new Worker { Id = Guid.NewGuid(), Name = "Alphonse Kamga", Trade = "Électricien bâtiment", Phone = "+237 694 55 66 77", DailyRate = 12000m, Status = WorkerStatus.Actif };
        var w5 = new Worker { Id = Guid.NewGuid(), Name = "Didier Ntsama", Trade = "Plombier", Phone = "+237 675 66 77 88", DailyRate = 11000m, Status = WorkerStatus.Actif };
        var w6 = new Worker { Id = Guid.NewGuid(), Name = "Pauline Kouam", Trade = "Peintre applicateur", Phone = "+237 696 77 88 99", DailyRate = 9000m, Status = WorkerStatus.Actif };
        var w7 = new Worker { Id = Guid.NewGuid(), Name = "Cédric Fopa", Trade = "Manœuvre polyvalent", Phone = "+237 677 88 99 10", DailyRate = 6000m, Status = WorkerStatus.Actif };
        var w8 = new Worker { Id = Guid.NewGuid(), Name = "Serge Bisseck", Trade = "Manœuvre polyvalent", Phone = "+237 698 99 10 11", DailyRate = 6000m, Status = WorkerStatus.Actif };
        _workers.AddRange(new[] { w1, w2, w3, w4, w5, w6, w7, w8 });

        // 4. Chantier 1 : Résidence Les Palmiers (Projet Principal - En cours, avancé)
        var c1 = new Chantier
        {
            Id = Guid.NewGuid(),
            Reference = "CH-2026-001",
            Name = "Résidence Les Palmiers",
            Client = "SCI Palm Promotion",
            Address = "Boulevard de la Liberté, Akwa, Douala",
            Latitude = 4.051056,
            Longitude = 9.767868,
            StartDate = DateTime.Today.AddMonths(-3),
            PlannedEndDate = DateTime.Today.AddMonths(3),
            TotalBudget = 145000000m,
            Status = ChantierStatus.EnCours,
            Description = "Construction d'un immeuble R+3 à usage mixte résidentiel et commercial avec sous-sol de parking."
        };

        var phasesC1 = ChantierTemplates.DefaultPhases(c1.Id, c1.StartDate, c1.PlannedEndDate, c1.TotalBudget);
        // Ajuster avancements
        phasesC1[0].Progress = 100m; // Terrassement
        phasesC1[1].Progress = 100m; // Fondations
        phasesC1[2].Progress = 85m;  // Gros oeuvre
        phasesC1[3].Progress = 40m;  // Toiture
        phasesC1[4].Progress = 20m;  // Second oeuvre
        phasesC1[5].Progress = 0m;   // Finitions
        phasesC1[6].Progress = 0m;   // Aménagements

        var budgetLinesC1 = ChantierTemplates.DefaultBudgetLines(c1.Id, c1.TotalBudget);

        var agg1 = new ChantierAggregate
        {
            Chantier = c1,
            Phases = phasesC1,
            BudgetLines = budgetLinesC1
        };

        // Mouvements de stock pour Chantier 1
        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matCiment.Id, Type = MovementType.Entree, Quantity = 500m, UnitPrice = 5200m, SupplierId = supCiment.Id, Date = DateTime.Today.AddDays(-60), DeliveryNoteNumber = "BL-5821", Author = "Alain Mvondo" });
        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matCiment.Id, Type = MovementType.Sortie, Quantity = 430m, UnitPrice = 5200m, PhaseId = phasesC1[2].Id, Date = DateTime.Today.AddDays(-20), Author = "Alain Mvondo" }); // Reste 70 (sous seuil 80 -> alerte!)

        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matFer12.Id, Type = MovementType.Entree, Quantity = 300m, UnitPrice = 6800m, SupplierId = supAcier.Id, Date = DateTime.Today.AddDays(-55), DeliveryNoteNumber = "BL-4410", Author = "Alain Mvondo" });
        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matFer12.Id, Type = MovementType.Sortie, Quantity = 260m, UnitPrice = 6800m, PhaseId = phasesC1[2].Id, Date = DateTime.Today.AddDays(-15), Author = "Alain Mvondo" }); // Reste 40 (sous seuil 50 -> alerte!)

        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matSable.Id, Type = MovementType.Entree, Quantity = 12m, UnitPrice = 95000m, SupplierId = supCarriere.Id, Date = DateTime.Today.AddDays(-40), DeliveryNoteNumber = "BL-1092", Author = "Alain Mvondo" });
        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matSable.Id, Type = MovementType.Sortie, Quantity = 10m, UnitPrice = 95000m, PhaseId = phasesC1[2].Id, Date = DateTime.Today.AddDays(-10), Author = "Alain Mvondo" });

        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matGravier.Id, Type = MovementType.Entree, Quantity = 8m, UnitPrice = 160000m, SupplierId = supCarriere.Id, Date = DateTime.Today.AddDays(-35), DeliveryNoteNumber = "BL-1140", Author = "Alain Mvondo" });
        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matGravier.Id, Type = MovementType.Sortie, Quantity = 7m, UnitPrice = 160000m, PhaseId = phasesC1[2].Id, Date = DateTime.Today.AddDays(-8), Author = "Alain Mvondo" });

        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matParpaing.Id, Type = MovementType.Entree, Quantity = 3500m, UnitPrice = 320m, Date = DateTime.Today.AddDays(-30), DeliveryNoteNumber = "BL-9812", Author = "Alain Mvondo" });
        agg1.Movements.Add(new StockMovement { ChantierId = c1.Id, MaterialId = matParpaing.Id, Type = MovementType.Sortie, Quantity = 3100m, UnitPrice = 320m, PhaseId = phasesC1[2].Id, Date = DateTime.Today.AddDays(-5), Author = "Alain Mvondo" });

        // Pointages pour Chantier 1 (aujourd'hui et derniers jours)
        for (int i = 0; i < 7; i++)
        {
            var day = DateTime.Today.AddDays(-i);
            agg1.Attendances.Add(new Attendance { ChantierId = c1.Id, WorkerId = w1.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w1.DailyRate, OvertimeHours = 1m });
            agg1.Attendances.Add(new Attendance { ChantierId = c1.Id, WorkerId = w2.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w2.DailyRate, OvertimeHours = 0m });
            agg1.Attendances.Add(new Attendance { ChantierId = c1.Id, WorkerId = w3.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w3.DailyRate, OvertimeHours = 2m });
            agg1.Attendances.Add(new Attendance { ChantierId = c1.Id, WorkerId = w7.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w7.DailyRate, OvertimeHours = 0m });
            agg1.Attendances.Add(new Attendance { ChantierId = c1.Id, WorkerId = w8.Id, Date = day, Status = AttendanceStatus.Present, DailyRate = w8.DailyRate, OvertimeHours = 0m });
        }

        // Dépenses annexes Chantier 1
        agg1.Expenses.Add(new Expense { ChantierId = c1.Id, Category = BudgetCategory.LocationEngins, Amount = 6500000m, Date = DateTime.Today.AddDays(-70), Supplier = "Engins BTP Littoral", Label = "Location tractopelle & pelleteuse 15j", PaymentMode = PaymentMode.Virement, Author = "Alain Mvondo" });
        agg1.Expenses.Add(new Expense { ChantierId = c1.Id, Category = BudgetCategory.LocationEngins, Amount = 3800000m, Date = DateTime.Today.AddDays(-30), Supplier = "Grue Express", Label = "Location grue mobile coulée dalle", PaymentMode = PaymentMode.Virement, Author = "Alain Mvondo" });
        agg1.Expenses.Add(new Expense { ChantierId = c1.Id, Category = BudgetCategory.Transport, Amount = 2100000m, Date = DateTime.Today.AddDays(-45), Supplier = "Transporteurs Unis", Label = "Évacuation des déblais et terres", PaymentMode = PaymentMode.MobileMoney, Author = "Alain Mvondo" });
        agg1.Expenses.Add(new Expense { ChantierId = c1.Id, Category = BudgetCategory.Outillage, Amount = 1450000m, Date = DateTime.Today.AddDays(-65), Supplier = "Quincaillerie Centrale", Label = "Aiguilles vibrantes, brouettes, pelles", PaymentMode = PaymentMode.Especes, Author = "Alain Mvondo" });
        agg1.Expenses.Add(new Expense { ChantierId = c1.Id, Category = BudgetCategory.SousTraitance, Amount = 4800000m, Date = DateTime.Today.AddDays(-15), Supplier = "Charpente Bois & Métal", Label = "Acompte 40% charpente métallique", PaymentMode = PaymentMode.Virement, Author = "Alain Mvondo" });

        // Journal de chantier
        agg1.Journal.Add(new JournalEntry { ChantierId = c1.Id, Date = DateTime.Today, Weather = WeatherKind.Ensoleille, Note = "Coulage du dernier chaînage haut. Équipe motivée, aucun incident à signaler. Contrôle armature validé par le bureau d'études.", Incident = "Néant", Visitors = "M. Tcheuko (Maître d'œuvre)", Author = "Alain Mvondo" });
        agg1.Journal.Add(new JournalEntry { ChantierId = c1.Id, Date = DateTime.Today.AddDays(-1), Weather = WeatherKind.Pluie, Note = "Arrêt de la coulée pendant 2h en raison de fortes averses tropicales. Reprise à 14h.", Incident = "Pluie torrentielle l'après-midi", Visitors = "Aucun", Author = "Alain Mvondo" });

        _chantiers.Add(agg1);

        // 5. Chantier 2 : Immeuble Horizon 2026 (Bastos, Yaoundé) - En démarrage
        var c2 = new Chantier
        {
            Id = Guid.NewGuid(),
            Reference = "CH-2026-002",
            Name = "Immeuble Horizon 2026",
            Client = "Groupe Horizon Immobilier",
            Address = "Quartier Bastos, Yaoundé",
            Latitude = 3.8912,
            Longitude = 11.5123,
            StartDate = DateTime.Today.AddDays(-20),
            PlannedEndDate = DateTime.Today.AddMonths(10),
            TotalBudget = 280000000m,
            Status = ChantierStatus.EnCours,
            Description = "Ensemble immobilier haut standing de bureaux et appartements."
        };
        var phasesC2 = ChantierTemplates.DefaultPhases(c2.Id, c2.StartDate, c2.PlannedEndDate, c2.TotalBudget);
        phasesC2[0].Progress = 90m; // Terrassement presque fini
        phasesC2[1].Progress = 15m; // Fondations démarrées
        var agg2 = new ChantierAggregate
        {
            Chantier = c2,
            Phases = phasesC2,
            BudgetLines = ChantierTemplates.DefaultBudgetLines(c2.Id, c2.TotalBudget)
        };
        agg2.Movements.Add(new StockMovement { ChantierId = c2.Id, MaterialId = matCiment.Id, Type = MovementType.Entree, Quantity = 200m, UnitPrice = 5200m, SupplierId = supCiment.Id, Date = DateTime.Today.AddDays(-15), DeliveryNoteNumber = "BL-7711", Author = "Alain Mvondo" });
        agg2.Movements.Add(new StockMovement { ChantierId = c2.Id, MaterialId = matSable.Id, Type = MovementType.Entree, Quantity = 5m, UnitPrice = 95000m, SupplierId = supCarriere.Id, Date = DateTime.Today.AddDays(-12), DeliveryNoteNumber = "BL-7714", Author = "Alain Mvondo" });
        _chantiers.Add(agg2);

        // 6. Chantier 3 : Villa Moderne Kribi (Planifié)
        var c3 = new Chantier
        {
            Id = Guid.NewGuid(),
            Reference = "CH-2026-003",
            Name = "Villa Océane Kribi",
            Client = "Dr. Roger Manga",
            Address = "Plage de Grand Batanga, Kribi",
            Latitude = 2.9234,
            Longitude = 9.8976,
            StartDate = DateTime.Today.AddDays(15),
            PlannedEndDate = DateTime.Today.AddMonths(7),
            TotalBudget = 75000000m,
            Status = ChantierStatus.Planifie,
            Description = "Villa balnéaire R+1 avec piscine à débordement et aménagements paysagers."
        };
        var agg3 = new ChantierAggregate
        {
            Chantier = c3,
            Phases = ChantierTemplates.DefaultPhases(c3.Id, c3.StartDate, c3.PlannedEndDate, c3.TotalBudget),
            BudgetLines = ChantierTemplates.DefaultBudgetLines(c3.Id, c3.TotalBudget)
        };
        _chantiers.Add(agg3);

        // Recalculer alertes pour chaque chantier
        RefreshAllAlerts();
    }

    private void RefreshAllAlerts()
    {
        _alerts.Clear();
        foreach (var c in _chantiers)
        {
            var insight = ChantierInsightService.Analyze(c, _materials, DateTime.Today);
            _alerts.AddRange(insight.Alerts);
        }
    }

    public Task<IReadOnlyList<ChantierAggregate>> GetChantiersAsync()
    {
        RefreshAllAlerts();
        return Task.FromResult<IReadOnlyList<ChantierAggregate>>(_chantiers.ToList());
    }

    public Task<ChantierAggregate?> GetChantierAsync(Guid id)
    {
        var found = _chantiers.FirstOrDefault(c => c.Chantier.Id == id);
        return Task.FromResult(found);
    }

    public Task SaveChantierAsync(ChantierAggregate data)
    {
        var existing = _chantiers.FirstOrDefault(c => c.Chantier.Id == data.Chantier.Id);
        if (existing != null)
        {
            var idx = _chantiers.IndexOf(existing);
            _chantiers[idx] = data;
        }
        else
        {
            _chantiers.Add(data);
        }
        RefreshAllAlerts();
        return Task.CompletedTask;
    }

    public Task<ChantierAggregate> DuplicateChantierAsync(Guid id)
    {
        var original = _chantiers.FirstOrDefault(c => c.Chantier.Id == id) ?? _chantiers.First();
        var newChantier = new Chantier
        {
            Id = Guid.NewGuid(),
            Reference = original.Chantier.Reference + "-COPIE",
            Name = original.Chantier.Name + " (Copie)",
            Client = original.Chantier.Client,
            Address = original.Chantier.Address,
            Latitude = original.Chantier.Latitude,
            Longitude = original.Chantier.Longitude,
            StartDate = DateTime.Today,
            PlannedEndDate = DateTime.Today.AddMonths(6),
            TotalBudget = original.Chantier.TotalBudget,
            Status = ChantierStatus.Planifie,
            Description = original.Chantier.Description
        };

        var newPhases = ChantierTemplates.DefaultPhases(newChantier.Id, newChantier.StartDate, newChantier.PlannedEndDate, newChantier.TotalBudget);
        var newBudget = ChantierTemplates.DefaultBudgetLines(newChantier.Id, newChantier.TotalBudget);

        var copy = new ChantierAggregate
        {
            Chantier = newChantier,
            Phases = newPhases,
            BudgetLines = newBudget
        };
        _chantiers.Add(copy);
        RefreshAllAlerts();
        return Task.FromResult(copy);
    }

    public Task SetStatusAsync(Guid id, ChantierStatus status)
    {
        var c = _chantiers.FirstOrDefault(x => x.Chantier.Id == id);
        if (c != null)
        {
            c.Chantier.Status = status;
        }
        return Task.CompletedTask;
    }

    public Task SavePhaseAsync(Phase phase, string author, string note)
    {
        var chantier = _chantiers.FirstOrDefault(c => c.Chantier.Id == phase.ChantierId);
        if (chantier != null)
        {
            var p = chantier.Phases.FirstOrDefault(x => x.Id == phase.Id);
            if (p != null)
            {
                p.Progress = phase.Progress;
                p.History.Add(new ProgressEntry
                {
                    PhaseId = phase.Id,
                    Progress = phase.Progress,
                    Date = DateTime.Now,
                    Author = author,
                    Note = note
                });
            }
        }
        RefreshAllAlerts();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Material>> GetMaterialsAsync()
        => Task.FromResult<IReadOnlyList<Material>>(_materials.ToList());

    public Task SaveMaterialAsync(Material material)
    {
        var idx = _materials.FindIndex(m => m.Id == material.Id);
        if (idx >= 0) _materials[idx] = material;
        else _materials.Add(material);
        RefreshAllAlerts();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Supplier>> GetSuppliersAsync()
        => Task.FromResult<IReadOnlyList<Supplier>>(_suppliers.ToList());

    public Task AddMovementAsync(StockMovement movement)
    {
        var chantier = _chantiers.FirstOrDefault(c => c.Chantier.Id == movement.ChantierId);
        if (chantier != null)
        {
            chantier.Movements.Add(movement);

            // RG-08 : actualisation du prix moyen pondéré en cas d'entrée
            if (movement.Type == MovementType.Entree)
            {
                var mat = _materials.FirstOrDefault(m => m.Id == movement.MaterialId);
                if (mat != null)
                {
                    var currentStock = StockCalculator.CurrentStock(chantier.Movements, chantier.Chantier.Id, mat.Id) - movement.Quantity;
                    mat.UnitPrice = StockCalculator.WeightedAveragePrice(Math.Max(0m, currentStock), mat.UnitPrice, movement.Quantity, movement.UnitPrice);
                }
            }
        }
        RefreshAllAlerts();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Worker>> GetWorkersAsync()
        => Task.FromResult<IReadOnlyList<Worker>>(_workers.ToList());

    public Task SaveWorkerAsync(Worker worker)
    {
        var idx = _workers.FindIndex(w => w.Id == worker.Id);
        if (idx >= 0) _workers[idx] = worker;
        else _workers.Add(worker);
        return Task.CompletedTask;
    }

    public Task SaveAttendancesAsync(Guid chantierId, DateTime date, IEnumerable<Attendance> attendances)
    {
        var chantier = _chantiers.FirstOrDefault(c => c.Chantier.Id == chantierId);
        if (chantier != null)
        {
            chantier.Attendances.RemoveAll(a => a.Date.Date == date.Date);
            chantier.Attendances.AddRange(attendances);
        }
        RefreshAllAlerts();
        return Task.CompletedTask;
    }

    public Task AddExpenseAsync(Expense expense)
    {
        var chantier = _chantiers.FirstOrDefault(c => c.Chantier.Id == expense.ChantierId);
        if (chantier != null)
        {
            chantier.Expenses.Add(expense);
        }
        RefreshAllAlerts();
        return Task.CompletedTask;
    }

    public Task AddJournalEntryAsync(JournalEntry entry)
    {
        var chantier = _chantiers.FirstOrDefault(c => c.Chantier.Id == entry.ChantierId);
        if (chantier != null)
        {
            chantier.Journal.Insert(0, entry);
        }
        return Task.CompletedTask;
    }

    public Task AddPhotoAsync(SitePhoto photo)
    {
        var chantier = _chantiers.FirstOrDefault(c => c.Chantier.Id == photo.ChantierId);
        if (chantier != null)
        {
            chantier.Photos.Insert(0, photo);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AlertItem>> GetAlertsAsync()
    {
        RefreshAllAlerts();
        return Task.FromResult<IReadOnlyList<AlertItem>>(_alerts.OrderByDescending(a => (int)a.Level).ThenByDescending(a => a.Date).ToList());
    }

    public Task ReplaceAlertsAsync(Guid chantierId, IEnumerable<AlertItem> generated)
    {
        _alerts.RemoveAll(a => a.ChantierId == chantierId);
        _alerts.AddRange(generated);
        return Task.CompletedTask;
    }

    public Task MarkAlertReadAsync(Guid alertId, bool isRead)
    {
        var a = _alerts.FirstOrDefault(x => x.Id == alertId);
        if (a != null) a.IsRead = isRead;
        return Task.CompletedTask;
    }

    public Task MarkAllAlertsReadAsync()
    {
        foreach (var a in _alerts) a.IsRead = true;
        return Task.CompletedTask;
    }
}
