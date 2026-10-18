using ChantierPro.Core.Models;

namespace ChantierPro.Core.Services;

/// <summary>RG-01 : avancement global pondéré.</summary>
public static class ProgressCalculator
{
    public static decimal Clamp(decimal value) => Math.Min(100m, Math.Max(0m, value));

    /// <summary>Σ (avancement de la phase × poids) / Σ poids. Vaut 0 si aucune phase.</summary>
    public static decimal GlobalProgress(IEnumerable<Phase> phases)
    {
        var list = phases.ToList();
        var totalWeight = list.Sum(p => p.Weight);
        if (list.Count == 0 || totalWeight <= 0m) return 0m;
        var weighted = list.Sum(p => Clamp(p.Progress) * p.Weight);
        return Math.Round(weighted / totalWeight, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>RG-01 : la somme des poids doit être égale à 100 %.</summary>
    public static bool AreWeightsValid(IEnumerable<Phase> phases) => phases.Sum(p => p.Weight) == 100m;

    /// <summary>Avancement théorique d'une phase à une date donnée (linéaire entre début et fin).</summary>
    public static decimal ExpectedPhaseProgress(Phase phase, DateTime today)
    {
        if (today.Date <= phase.StartDate.Date) return 0m;
        if (today.Date >= phase.EndDate.Date) return 100m;
        var total = (phase.EndDate.Date - phase.StartDate.Date).TotalDays;
        if (total <= 0) return 100m;
        var elapsed = (today.Date - phase.StartDate.Date).TotalDays;
        return Math.Round((decimal)(elapsed / total) * 100m, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>Avancement global théorique à une date donnée.</summary>
    public static decimal ExpectedGlobalProgress(IEnumerable<Phase> phases, DateTime today)
    {
        var list = phases.ToList();
        var totalWeight = list.Sum(p => p.Weight);
        if (list.Count == 0 || totalWeight <= 0m) return 0m;
        var weighted = list.Sum(p => ExpectedPhaseProgress(p, today) * p.Weight);
        return Math.Round(weighted / totalWeight, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>F-ALE-04 : une phase est en retard si son avancement est inférieur au prévu (avec tolérance).</summary>
    public static bool IsPhaseLate(Phase phase, DateTime today, decimal tolerance = 5m)
    {
        if (phase.Progress >= 100m) return false;
        var expected = ExpectedPhaseProgress(phase, today);
        return expected - Clamp(phase.Progress) > tolerance;
    }
}

/// <summary>RG-05 : coût de main-d'œuvre.</summary>
public static class LabourCostCalculator
{
    public const decimal HoursPerDay = 8m;
    public const decimal OvertimeMultiplier = 1.25m;

    public static decimal PresenceCoefficient(AttendanceStatus status) => status switch
    {
        AttendanceStatus.Present => 1m,
        AttendanceStatus.Retard => 1m,
        AttendanceStatus.DemiJournee => 0.5m,
        _ => 0m
    };

    /// <summary>Taux journalier × coefficient de présence + heures supplémentaires majorées.</summary>
    public static decimal DayCost(decimal dailyRate, AttendanceStatus status, decimal overtimeHours)
    {
        var coefficient = PresenceCoefficient(status);
        var basePay = dailyRate * coefficient;
        var overtime = coefficient > 0m
            ? Math.Max(0m, overtimeHours) * (dailyRate / HoursPerDay) * OvertimeMultiplier
            : 0m;
        return Math.Round(basePay + overtime, 0, MidpointRounding.AwayFromZero);
    }
}

/// <summary>Stock de matériaux : RG-03 et RG-08.</summary>
public static class StockCalculator
{
    /// <summary>RG-08 : nouveau prix moyen pondéré après une entrée.</summary>
    public static decimal WeightedAveragePrice(decimal currentQty, decimal currentPrice, decimal inQty, decimal inPrice)
    {
        if (inQty <= 0m) return currentPrice;
        if (currentQty <= 0m) return inPrice;
        var total = currentQty + inQty;
        return Math.Round((currentQty * currentPrice + inQty * inPrice) / total, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>RG-03 : une sortie ne peut pas dépasser le stock sauf autorisation explicite.</summary>
    public static bool CanIssue(decimal available, decimal quantity, bool allowNegative)
        => quantity > 0m && (quantity <= available || allowNegative);

    public static decimal CurrentStock(IEnumerable<StockMovement> movements, Guid chantierId, Guid materialId)
        => movements
            .Where(m => m.ChantierId == chantierId && m.MaterialId == materialId)
            .Sum(m => m.Type == MovementType.Entree ? m.Quantity : -m.Quantity);

    /// <summary>Prévision de rupture : jours restants selon le rythme de consommation récent. Null si aucune consommation.</summary>
    public static decimal? DaysRemaining(decimal stock, IEnumerable<StockMovement> movements, Guid chantierId,
        Guid materialId, DateTime today, int windowDays = 14)
    {
        if (stock <= 0m) return 0m;
        var from = today.Date.AddDays(-windowDays);
        var consumed = movements
            .Where(m => m.ChantierId == chantierId && m.MaterialId == materialId
                        && m.Type == MovementType.Sortie && m.Date.Date > from && m.Date.Date <= today.Date)
            .Sum(m => m.Quantity);
        if (consumed <= 0m) return null;
        var perDay = consumed / windowDays;
        return Math.Round(stock / perDay, 1, MidpointRounding.AwayFromZero);
    }
}

/// <summary>Seuils d'alerte budgétaire (F-ALE-01), configurables.</summary>
public record BudgetThresholds(decimal Warning = 0.80m, decimal Critical = 1.00m)
{
    public AlertLevel? LevelFor(decimal rate)
    {
        if (rate >= Critical) return AlertLevel.Critical;
        if (rate >= Warning) return AlertLevel.Avertissement;
        return null;
    }
}
