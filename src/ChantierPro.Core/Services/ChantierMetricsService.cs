using ChantierPro.Core.Models;

namespace ChantierPro.Core.Services;

/// <summary>Stock courant d'un matériau sur un chantier.</summary>
public record MaterialStock(Material Material, decimal Quantity)
{
    public bool IsLow => Quantity <= Material.AlertThreshold;
    public decimal Value => Quantity * Material.UnitPrice;
}

/// <summary>Résultat budget vs réalisé pour un poste (F-DEP-04).</summary>
public record BudgetLineResult(
    BudgetCategory Category,
    decimal Planned,
    decimal Actual,
    decimal Variance,
    decimal VariancePercent,
    decimal Rate,
    AlertLevel? Level);

public record CurvePoint(DateTime Date, decimal Planned, decimal? Actual);

/// <summary>Indicateurs calculés d'un chantier.</summary>
public record ChantierMetrics(
    decimal Progress,
    decimal ExpectedProgress,
    decimal Consumed,
    decimal Remaining,
    decimal ConsumptionRate,
    decimal ExpensesTotal,
    decimal MaterialsTotal,
    decimal LabourTotal,
    decimal EstimateAtCompletion,
    HealthLevel Health,
    int HealthScore,
    IReadOnlyList<BudgetLineResult> Lines)
{
    /// <summary>Dépassement projeté (positif = dépassement).</summary>
    public decimal ProjectedOverrun(decimal budget) => EstimateAtCompletion - budget;
}

/// <summary>Calcule les indicateurs d'un chantier (RG-01, RG-02, RG-04, F-DEP-03, F-DEP-06).</summary>
public static class ChantierMetricsService
{
    public const decimal IncoherenceTolerancePoints = 15m;

    public static ChantierMetrics Compute(ChantierAggregate data, DateTime today, BudgetThresholds? thresholds = null,
        int lowStockCount = 0)
    {
        thresholds ??= new BudgetThresholds();
        var budget = data.Chantier.TotalBudget;

        var expenses = data.Expenses.Sum(e => e.Amount);
        var materials = data.Movements.Where(m => m.Type == MovementType.Sortie).Sum(m => m.TotalCost);
        var labour = data.Attendances.Sum(a => a.Cost);
        var consumed = expenses + materials + labour; // RG-02

        var rate = budget > 0m ? consumed / budget : 0m; // RG-04
        var progress = ProgressCalculator.GlobalProgress(data.Phases);
        var expected = ProgressCalculator.ExpectedGlobalProgress(data.Phases, today);

        var eac = progress > 0m ? Math.Round(consumed / (progress / 100m), 0, MidpointRounding.AwayFromZero) : budget;
        var lines = ComputeLines(data, thresholds);

        var (health, score) = EvaluateHealth(rate, progress, expected, data.Phases, today, lowStockCount, thresholds);

        return new ChantierMetrics(progress, expected, consumed, budget - consumed, rate, expenses, materials, labour,
            eac, health, score, lines);
    }

    public static IReadOnlyList<BudgetLineResult> ComputeLines(ChantierAggregate data, BudgetThresholds thresholds)
    {
        var categories = data.BudgetLines.Select(b => b.Category)
            .Union(data.Expenses.Select(e => e.Category))
            .Union(new[] { BudgetCategory.Materiaux, BudgetCategory.MainOeuvre })
            .Distinct()
            .OrderBy(c => (int)c)
            .ToList();

        var results = new List<BudgetLineResult>();
        foreach (var category in categories)
        {
            var planned = data.BudgetLines.Where(b => b.Category == category).Sum(b => b.PlannedAmount);
            var actual = data.Expenses.Where(e => e.Category == category).Sum(e => e.Amount);

            // F-DEP-03 : intégration automatique des coûts matériaux et main-d'œuvre.
            if (category == BudgetCategory.Materiaux)
                actual += data.Movements.Where(m => m.Type == MovementType.Sortie).Sum(m => m.TotalCost);
            if (category == BudgetCategory.MainOeuvre)
                actual += data.Attendances.Sum(a => a.Cost);

            if (planned == 0m && actual == 0m) continue;

            var variance = planned - actual;
            var variancePct = planned > 0m ? Math.Round(variance / planned * 100m, 1, MidpointRounding.AwayFromZero) : 0m;
            var rate = planned > 0m ? actual / planned : (actual > 0m ? 1m : 0m);
            results.Add(new BudgetLineResult(category, planned, actual, variance, variancePct, rate,
                thresholds.LevelFor(rate)));
        }

        return results;
    }

    /// <summary>Indice de santé du chantier (vert / orange / rouge) combinant budget, délais et stock.</summary>
    public static (HealthLevel Level, int Score) EvaluateHealth(decimal rate, decimal progress, decimal expected,
        IEnumerable<Phase> phases, DateTime today, int lowStockCount, BudgetThresholds thresholds)
    {
        var score = 100;
        if (rate >= thresholds.Critical) score -= 40;
        else if (rate >= thresholds.Warning) score -= 20;

        var delay = expected - progress;
        if (delay > 20m) score -= 30;
        else if (delay > 5m) score -= 15;

        if (phases.Any(p => ProgressCalculator.IsPhaseLate(p, today))) score -= 10;
        if (lowStockCount > 0) score -= Math.Min(15, lowStockCount * 5);
        if (IsIncoherent(rate, progress)) score -= 10;

        score = Math.Max(0, score);
        var level = score >= 75 ? HealthLevel.Good : score >= 50 ? HealthLevel.Warning : HealthLevel.Critical;
        return (level, score);
    }

    /// <summary>F-ALE-05 : budget consommé supérieur à l'avancement réel (écart supérieur à la tolérance).</summary>
    public static bool IsIncoherent(decimal consumptionRate, decimal progress)
        => consumptionRate * 100m - progress > IncoherenceTolerancePoints;

    /// <summary>Courbe cumulée prévu / réel (F-DEP-05), un point par semaine.</summary>
    public static IReadOnlyList<CurvePoint> BuildCumulativeCurve(ChantierAggregate data, DateTime today)
    {
        var start = data.Chantier.StartDate.Date;
        var end = data.Chantier.PlannedEndDate.Date;
        if (end <= start) end = start.AddDays(7);

        var points = new List<CurvePoint>();
        for (var d = start; ; d = d.AddDays(7))
        {
            var date = d > end ? end : d;
            var planned = PlannedCumulative(data.Phases, date);
            decimal? actual = date <= today.Date ? ActualCumulative(data, date) : null;
            points.Add(new CurvePoint(date, planned, actual));
            if (date >= end) break;
        }

        return points;
    }

    private static decimal PlannedCumulative(IEnumerable<Phase> phases, DateTime date)
        => Math.Round(phases.Sum(p => p.AllocatedBudget * ProgressCalculator.ExpectedPhaseProgress(p, date) / 100m), 0,
            MidpointRounding.AwayFromZero);

    private static decimal ActualCumulative(ChantierAggregate data, DateTime date)
    {
        var expenses = data.Expenses.Where(e => e.Date.Date <= date).Sum(e => e.Amount);
        var materials = data.Movements.Where(m => m.Type == MovementType.Sortie && m.Date.Date <= date).Sum(m => m.TotalCost);
        var labour = data.Attendances.Where(a => a.Date.Date <= date).Sum(a => a.Cost);
        return expenses + materials + labour;
    }
}

/// <summary>Génère les alertes d'un chantier (F-ALE-01, 03, 04, 05).</summary>
public static class AlertEngine
{
    public static IReadOnlyList<AlertItem> Generate(ChantierAggregate data, ChantierMetrics metrics,
        IEnumerable<MaterialStock> stocks, DateTime today, Func<BudgetCategory, string> categoryName,
        Func<Guid, string> materialName)
    {
        var chantierId = data.Chantier.Id;
        var alerts = new List<AlertItem>();
        var threshold = new BudgetThresholds();

        AlertItem Make(AlertType type, AlertLevel level, string key, string message) => new()
        {
            ChantierId = chantierId, Type = type, Level = level, Key = key, Message = message, Date = today
        };

        // Budget global
        var globalLevel = threshold.LevelFor(metrics.ConsumptionRate);
        if (globalLevel is { } gl)
        {
            var pct = Math.Round(metrics.ConsumptionRate * 100m, 0, MidpointRounding.AwayFromZero);
            alerts.Add(Make(AlertType.Budget, gl, $"budget:global:{gl}",
                gl == AlertLevel.Critical
                    ? $"Budget global dépassé : {pct} % consommé."
                    : $"Budget global à {pct} % : seuil d'avertissement atteint."));
        }

        // Budget par poste
        foreach (var line in metrics.Lines.Where(l => l.Planned > 0m && l.Level != null))
        {
            var pct = Math.Round(line.Rate * 100m, 0, MidpointRounding.AwayFromZero);
            var level = line.Level!.Value;
            alerts.Add(Make(AlertType.Budget, level, $"budget:{line.Category}:{level}",
                level == AlertLevel.Critical
                    ? $"Poste « {categoryName(line.Category)} » dépassé : {pct} % du budget."
                    : $"Poste « {categoryName(line.Category)} » à {pct} % du budget."));
        }

        // Stock bas
        foreach (var s in stocks.Where(s => s.IsLow))
        {
            var level = s.Quantity <= 0m ? AlertLevel.Critical : AlertLevel.Avertissement;
            alerts.Add(Make(AlertType.Stock, level, $"stock:{s.Material.Id}",
                $"Stock bas : {materialName(s.Material.Id)} ({s.Quantity:0.##} {s.Material.Unit} restant)."));
        }

        // Retard de phase
        foreach (var phase in data.Phases.Where(p => ProgressCalculator.IsPhaseLate(p, today)))
        {
            var expected = ProgressCalculator.ExpectedPhaseProgress(phase, today);
            alerts.Add(Make(AlertType.Retard, AlertLevel.Avertissement, $"late:{phase.Id}",
                $"Phase « {phase.Name} » en retard : {phase.Progress:0.#} % réalisé pour {expected:0.#} % prévu."));
        }

        // Incohérence budget / avancement
        if (ChantierMetricsService.IsIncoherent(metrics.ConsumptionRate, metrics.Progress) && metrics.ConsumptionRate > 0m)
        {
            alerts.Add(Make(AlertType.Incoherence, AlertLevel.Avertissement, "incoherence",
                $"Incohérence : {Math.Round(metrics.ConsumptionRate * 100m, 0)} % du budget consommé pour {metrics.Progress:0.#} % d'avancement."));
        }

        return alerts;
    }
}
