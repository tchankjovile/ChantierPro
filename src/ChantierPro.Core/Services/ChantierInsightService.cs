using ChantierPro.Core.Models;

namespace ChantierPro.Core.Services;

/// <summary>Synthèse complète d'un chantier : indicateurs, stocks et alertes générées.</summary>
public record ChantierInsight(
    ChantierAggregate Data,
    ChantierMetrics Metrics,
    IReadOnlyList<MaterialStock> Stocks,
    IReadOnlyList<AlertItem> Alerts);

public static class ChantierInsightService
{
    public static IReadOnlyList<MaterialStock> ComputeStocks(ChantierAggregate data, IEnumerable<Material> materials)
    {
        var usedIds = data.Movements.Select(m => m.MaterialId).ToHashSet();
        return materials
            .Where(m => usedIds.Contains(m.Id))
            .Select(m => new MaterialStock(m, StockCalculator.CurrentStock(data.Movements, data.Chantier.Id, m.Id)))
            .OrderBy(s => s.Material.Name)
            .ToList();
    }

    public static ChantierInsight Analyze(ChantierAggregate data, IReadOnlyList<Material> materials, DateTime today,
        BudgetThresholds? thresholds = null)
    {
        var stocks = ComputeStocks(data, materials);
        var metrics = ChantierMetricsService.Compute(data, today, thresholds, stocks.Count(s => s.IsLow));
        var names = materials.ToDictionary(m => m.Id, m => m.Name);
        var alerts = AlertEngine.Generate(data, metrics, stocks, today, Labels.Of,
            id => names.TryGetValue(id, out var n) ? n : "Matériau");
        return new ChantierInsight(data, metrics, stocks, alerts);
    }
}
