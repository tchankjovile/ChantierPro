using ChantierPro.Core.Models;

namespace ChantierPro.Core.Services;

/// <summary>Modèles de phases et de budget (F-PHA-01, F-DEP-01) pour créer un chantier en moins de 5 minutes.</summary>
public static class ChantierTemplates
{
    private static readonly (string Name, decimal Weight, string[] Tasks)[] PhaseModel =
    {
        ("Terrassement", 5m, new[] { "Implantation et piquetage", "Décapage du terrain", "Fouilles en rigole" }),
        ("Fondations", 15m, new[] { "Coulage du béton de propreté", "Ferraillage des semelles", "Coulage des semelles et longrines" }),
        ("Gros œuvre", 30m, new[] { "Élévation des murs", "Coulage des poteaux et chaînages", "Réalisation de la dalle" }),
        ("Toiture", 10m, new[] { "Pose de la charpente", "Couverture", "Étanchéité et zinguerie" }),
        ("Second œuvre", 20m, new[] { "Électricité", "Plomberie", "Menuiseries", "Enduits et cloisons" }),
        ("Finitions", 15m, new[] { "Carrelage", "Peinture", "Pose des sanitaires et luminaires" }),
        ("Aménagements extérieurs", 5m, new[] { "Clôture et portail", "Voirie et drainage", "Nettoyage final" })
    };

    private static readonly (BudgetCategory Category, decimal Share)[] BudgetModel =
    {
        (BudgetCategory.Materiaux, 45m),
        (BudgetCategory.MainOeuvre, 25m),
        (BudgetCategory.LocationEngins, 8m),
        (BudgetCategory.Transport, 4m),
        (BudgetCategory.SousTraitance, 8m),
        (BudgetCategory.Outillage, 3m),
        (BudgetCategory.Administratif, 3m),
        (BudgetCategory.Imprevus, 4m)
    };

    /// <summary>Phases standard : poids = 100 %, budget et durée répartis au prorata du poids.</summary>
    public static List<Phase> DefaultPhases(Guid chantierId, DateTime start, DateTime end, decimal totalBudget)
    {
        var totalDays = Math.Max(PhaseModel.Length, (end.Date - start.Date).Days);
        var phases = new List<Phase>();
        var cursor = start.Date;
        var allocated = 0m;
        var order = 1;

        for (var i = 0; i < PhaseModel.Length; i++)
        {
            var (name, weight, tasks) = PhaseModel[i];
            var isLast = i == PhaseModel.Length - 1;
            var days = isLast ? (start.Date.AddDays(totalDays) - cursor).Days : (int)Math.Round(totalDays * weight / 100m);
            days = Math.Max(1, days);
            var budget = isLast ? totalBudget - allocated : Math.Round(totalBudget * weight / 100m, 0, MidpointRounding.AwayFromZero);
            allocated += budget;

            var phase = new Phase
            {
                ChantierId = chantierId,
                Name = name,
                Order = order++,
                Weight = weight,
                StartDate = cursor,
                EndDate = cursor.AddDays(days),
                AllocatedBudget = budget,
                Progress = 0m
            };
            var taskOrder = 1;
            foreach (var t in tasks)
                phase.Tasks.Add(new PhaseTask { PhaseId = phase.Id, Label = t, Order = taskOrder++ });
            phases.Add(phase);
            cursor = phase.EndDate;
        }

        return phases;
    }

    /// <summary>Budget prévisionnel par poste réparti selon un modèle standard (somme = budget total).</summary>
    public static List<BudgetLine> DefaultBudgetLines(Guid chantierId, decimal totalBudget)
    {
        var lines = new List<BudgetLine>();
        var allocated = 0m;
        for (var i = 0; i < BudgetModel.Length; i++)
        {
            var (category, share) = BudgetModel[i];
            var amount = i == BudgetModel.Length - 1
                ? totalBudget - allocated
                : Math.Round(totalBudget * share / 100m, 0, MidpointRounding.AwayFromZero);
            allocated += amount;
            lines.Add(new BudgetLine { ChantierId = chantierId, Category = category, PlannedAmount = amount });
        }

        return lines;
    }
}
