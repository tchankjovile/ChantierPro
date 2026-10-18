using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using Xunit;

namespace ChantierPro.Tests;

public class ProgressCalculatorTests
{
    private static Phase P(decimal weight, decimal progress, string start = "2026-01-01", string end = "2026-01-31") => new()
    {
        Weight = weight, Progress = progress, StartDate = DateTime.Parse(start), EndDate = DateTime.Parse(end)
    };

    [Fact]
    public void GlobalProgress_IsWeightedSum_RG01()
    {
        var phases = new[] { P(50, 100), P(30, 50), P(20, 0) };
        Assert.Equal(65.0m, ProgressCalculator.GlobalProgress(phases));
    }

    [Fact]
    public void GlobalProgress_NoPhases_IsZero() => Assert.Equal(0m, ProgressCalculator.GlobalProgress(Array.Empty<Phase>()));

    [Fact]
    public void GlobalProgress_ClampsValuesAbove100()
        => Assert.Equal(100m, ProgressCalculator.GlobalProgress(new[] { P(100, 250) }));

    [Fact]
    public void Weights_MustSumTo100()
    {
        Assert.True(ProgressCalculator.AreWeightsValid(new[] { P(60, 0), P(40, 0) }));
        Assert.False(ProgressCalculator.AreWeightsValid(new[] { P(60, 0), P(30, 0) }));
    }

    [Fact]
    public void ExpectedProgress_IsLinearBetweenDates()
    {
        var phase = P(100, 0, "2026-01-01", "2026-01-11");
        Assert.Equal(0m, ProgressCalculator.ExpectedPhaseProgress(phase, DateTime.Parse("2025-12-01")));
        Assert.Equal(50m, ProgressCalculator.ExpectedPhaseProgress(phase, DateTime.Parse("2026-01-06")));
        Assert.Equal(100m, ProgressCalculator.ExpectedPhaseProgress(phase, DateTime.Parse("2026-03-01")));
    }

    [Fact]
    public void Phase_IsLate_WhenBehindExpectedBeyondTolerance()
    {
        var phase = P(100, 20, "2026-01-01", "2026-01-11");
        Assert.True(ProgressCalculator.IsPhaseLate(phase, DateTime.Parse("2026-01-06")));
        phase.Progress = 48m;
        Assert.False(ProgressCalculator.IsPhaseLate(phase, DateTime.Parse("2026-01-06")));
        phase.Progress = 100m;
        Assert.False(ProgressCalculator.IsPhaseLate(phase, DateTime.Parse("2026-02-06")));
    }
}

public class LabourCostCalculatorTests
{
    [Theory]
    [InlineData(AttendanceStatus.Present, 0, 10000)]
    [InlineData(AttendanceStatus.Retard, 0, 10000)]
    [InlineData(AttendanceStatus.DemiJournee, 0, 5000)]
    [InlineData(AttendanceStatus.Absent, 0, 0)]
    public void DayCost_AppliesPresenceCoefficient_RG05(AttendanceStatus status, int overtime, int expected)
        => Assert.Equal((decimal)expected, LabourCostCalculator.DayCost(10000m, status, overtime));

    [Fact]
    public void DayCost_AddsMajoredOvertime()
    {
        // 10 000 + 2 h × (10 000 / 8) × 1,25 = 10 000 + 3 125
        Assert.Equal(13125m, LabourCostCalculator.DayCost(10000m, AttendanceStatus.Present, 2m));
    }

    [Fact]
    public void DayCost_IgnoresOvertimeWhenAbsent()
        => Assert.Equal(0m, LabourCostCalculator.DayCost(10000m, AttendanceStatus.Absent, 4m));
}

public class StockCalculatorTests
{
    [Fact]
    public void WeightedAverage_RG08()
        => Assert.Equal(1100m, StockCalculator.WeightedAveragePrice(100m, 1000m, 100m, 1200m));

    [Fact]
    public void WeightedAverage_EmptyStock_UsesIncomingPrice()
        => Assert.Equal(1200m, StockCalculator.WeightedAveragePrice(0m, 1000m, 50m, 1200m));

    [Theory]
    [InlineData(10, 5, false, true)]
    [InlineData(10, 10, false, true)]
    [InlineData(10, 11, false, false)]
    [InlineData(10, 11, true, true)]
    [InlineData(10, 0, true, false)]
    public void CanIssue_RG03(int available, int qty, bool allowNegative, bool expected)
        => Assert.Equal(expected, StockCalculator.CanIssue(available, qty, allowNegative));

    [Fact]
    public void CurrentStock_SumsEntriesMinusExits()
    {
        var c = Guid.NewGuid();
        var m = Guid.NewGuid();
        var moves = new[]
        {
            new StockMovement { ChantierId = c, MaterialId = m, Type = MovementType.Entree, Quantity = 100m },
            new StockMovement { ChantierId = c, MaterialId = m, Type = MovementType.Sortie, Quantity = 30m },
            new StockMovement { ChantierId = Guid.NewGuid(), MaterialId = m, Type = MovementType.Entree, Quantity = 999m }
        };
        Assert.Equal(70m, StockCalculator.CurrentStock(moves, c, m));
    }

    [Fact]
    public void DaysRemaining_UsesRecentConsumption()
    {
        var c = Guid.NewGuid();
        var m = Guid.NewGuid();
        var today = new DateTime(2026, 6, 15);
        var moves = new[]
        {
            new StockMovement { ChantierId = c, MaterialId = m, Type = MovementType.Sortie, Quantity = 28m, Date = today.AddDays(-3) }
        };
        // 28 u sur 14 jours = 2 u/jour ; 10 u restantes => 5 jours
        Assert.Equal(5m, StockCalculator.DaysRemaining(10m, moves, c, m, today));
        Assert.Null(StockCalculator.DaysRemaining(10m, Array.Empty<StockMovement>(), c, m, today));
    }
}

public class ChantierMetricsTests
{
    private static ChantierAggregate Build(decimal budget = 1_000_000m)
    {
        var chantier = new Chantier
        {
            Name = "Test", TotalBudget = budget, Status = ChantierStatus.EnCours,
            StartDate = new DateTime(2026, 1, 1), PlannedEndDate = new DateTime(2026, 3, 1)
        };
        var phase = new Phase
        {
            ChantierId = chantier.Id, Name = "Gros œuvre", Weight = 100m, Progress = 40m,
            StartDate = chantier.StartDate, EndDate = chantier.PlannedEndDate, AllocatedBudget = budget
        };
        var material = Guid.NewGuid();
        return new ChantierAggregate
        {
            Chantier = chantier,
            Phases = { phase },
            BudgetLines =
            {
                new BudgetLine { ChantierId = chantier.Id, Category = BudgetCategory.Materiaux, PlannedAmount = 500_000m },
                new BudgetLine { ChantierId = chantier.Id, Category = BudgetCategory.MainOeuvre, PlannedAmount = 300_000m },
                new BudgetLine { ChantierId = chantier.Id, Category = BudgetCategory.Transport, PlannedAmount = 200_000m }
            },
            Expenses =
            {
                new Expense { ChantierId = chantier.Id, Category = BudgetCategory.Transport, Amount = 50_000m, Date = new DateTime(2026, 1, 10) }
            },
            Movements =
            {
                new StockMovement { ChantierId = chantier.Id, MaterialId = material, Type = MovementType.Entree, Quantity = 100, UnitPrice = 5_000m, Date = new DateTime(2026, 1, 5) },
                new StockMovement { ChantierId = chantier.Id, MaterialId = material, Type = MovementType.Sortie, Quantity = 40, UnitPrice = 5_000m, Date = new DateTime(2026, 1, 12) }
            },
            Attendances =
            {
                new Attendance { ChantierId = chantier.Id, Date = new DateTime(2026, 1, 12), Status = AttendanceStatus.Present, DailyRate = 10_000m },
                new Attendance { ChantierId = chantier.Id, Date = new DateTime(2026, 1, 12), Status = AttendanceStatus.DemiJournee, DailyRate = 10_000m }
            }
        };
    }

    [Fact]
    public void Consumed_IsExpensesPlusIssuedMaterialsPlusLabour_RG02()
    {
        var m = ChantierMetricsService.Compute(Build(), new DateTime(2026, 1, 20));
        Assert.Equal(50_000m, m.ExpensesTotal);
        Assert.Equal(200_000m, m.MaterialsTotal);   // seules les sorties sont consommées
        Assert.Equal(15_000m, m.LabourTotal);
        Assert.Equal(265_000m, m.Consumed);
        Assert.Equal(735_000m, m.Remaining);
        Assert.Equal(0.265m, m.ConsumptionRate);
    }

    [Fact]
    public void Lines_IntegrateMaterialsAndLabourAutomatically_FDEP03()
    {
        var m = ChantierMetricsService.Compute(Build(), new DateTime(2026, 1, 20));
        var materials = m.Lines.Single(l => l.Category == BudgetCategory.Materiaux);
        var labour = m.Lines.Single(l => l.Category == BudgetCategory.MainOeuvre);
        Assert.Equal(200_000m, materials.Actual);
        Assert.Equal(15_000m, labour.Actual);
        Assert.Equal(300_000m, materials.Variance);
        Assert.Equal(60m, materials.VariancePercent);
    }

    [Fact]
    public void EstimateAtCompletion_ProjectsFromProgress_FDEP06()
    {
        var m = ChantierMetricsService.Compute(Build(), new DateTime(2026, 1, 20));
        Assert.Equal(40m, m.Progress);
        Assert.Equal(662_500m, m.EstimateAtCompletion); // 265 000 / 0,40
    }

    [Theory]
    [InlineData(0.50, null)]
    [InlineData(0.80, AlertLevel.Avertissement)]
    [InlineData(0.99, AlertLevel.Avertissement)]
    [InlineData(1.00, AlertLevel.Critical)]
    [InlineData(1.30, AlertLevel.Critical)]
    public void Thresholds_80And100_RG04(double rate, AlertLevel? expected)
        => Assert.Equal(expected, new BudgetThresholds().LevelFor((decimal)rate));

    [Fact]
    public void Thresholds_AreConfigurable()
        => Assert.Equal(AlertLevel.Avertissement, new BudgetThresholds(0.5m, 0.9m).LevelFor(0.6m));

    [Fact]
    public void Incoherence_WhenConsumptionExceedsProgressBeyondTolerance_FALE05()
    {
        Assert.True(ChantierMetricsService.IsIncoherent(0.60m, 30m));
        Assert.False(ChantierMetricsService.IsIncoherent(0.40m, 30m));
    }

    [Fact]
    public void Alerts_BudgetCritical_AndLowStock()
    {
        var data = Build(budget: 200_000m);
        var mat = new Material { Name = "Ciment", Unit = "sac", UnitPrice = 5_000m, AlertThreshold = 70m };
        data.Movements.ForEach(x => x.MaterialId = mat.Id);

        var insight = ChantierInsightService.Analyze(data, new[] { mat }, new DateTime(2026, 1, 20));

        Assert.Contains(insight.Alerts, a => a.Type == AlertType.Budget && a.Level == AlertLevel.Critical);
        Assert.Contains(insight.Alerts, a => a.Type == AlertType.Stock && a.Message.Contains("Ciment"));
        Assert.Equal(HealthLevel.Critical, insight.Metrics.Health);
    }

    [Fact]
    public void Curve_StartsAtZeroAndEndsAtTotalAllocatedBudget()
    {
        var curve = ChantierMetricsService.BuildCumulativeCurve(Build(), new DateTime(2026, 1, 20));
        Assert.Equal(0m, curve.First().Planned);
        Assert.Equal(1_000_000m, curve.Last().Planned);
        Assert.Null(curve.Last().Actual);
        Assert.NotNull(curve.First().Actual);
    }
}

public class ChantierTemplatesTests
{
    [Fact]
    public void DefaultPhases_WeightsSumTo100_AndBudgetEqualsTotal()
    {
        var id = Guid.NewGuid();
        var phases = ChantierTemplates.DefaultPhases(id, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), 123_456_789m);
        Assert.True(ProgressCalculator.AreWeightsValid(phases));
        Assert.Equal(123_456_789m, phases.Sum(p => p.AllocatedBudget));
        Assert.All(phases, p => Assert.True(p.EndDate > p.StartDate));
        Assert.All(phases, p => Assert.NotEmpty(p.Tasks));
    }

    [Fact]
    public void DefaultBudgetLines_SumToTotal()
    {
        var lines = ChantierTemplates.DefaultBudgetLines(Guid.NewGuid(), 987_654_321m);
        Assert.Equal(987_654_321m, lines.Sum(l => l.PlannedAmount));
        Assert.Equal(8, lines.Count);
    }
}

public class MoneyTests
{
    [Fact]
    public void Format_UsesGroupingAndCurrency()
        => Assert.Equal("1\u00a0250\u00a0000\u00a0FCFA", Money.Format(1_250_000m));

    [Theory]
    [InlineData(850, "850\u00a0FCFA")]
    [InlineData(12_500, "12,5\u00a0k\u00a0FCFA")]
    [InlineData(2_400_000, "2,4\u00a0M\u00a0FCFA")]
    public void Compact_Abbreviates(int value, string expected) => Assert.Equal(expected, Money.Compact(value));
}
