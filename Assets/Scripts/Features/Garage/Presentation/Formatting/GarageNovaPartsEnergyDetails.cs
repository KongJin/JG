using System.Collections.Generic;

namespace Features.Garage.Presentation
{
    internal static class GarageNovaPartsEnergyDetails
    {
        public static GarageNovaPartsPanelViewModel Apply(
            GaragePanelCatalog catalog,
            GarageNovaPartsPanelViewModel source)
        {
            if (catalog == null || source == null || source.Options == null)
                return source;

            var options = new List<GarageNovaPartOptionViewModel>(source.Options.Count);
            GarageNovaPartOptionViewModel selected = null;
            for (var i = 0; i < source.Options.Count; i++)
            {
                var option = source.Options[i];
                var mapped = new GarageNovaPartOptionViewModel(
                    option.Slot,
                    option.Id,
                    option.DisplayName,
                    BuildEnergyDetail(catalog, option),
                    option.SourcePath,
                    option.IsSelected,
                    option.NeedsNameReview,
                    option.PreviewPrefab,
                    option.Alignment);

                if (mapped.IsSelected)
                    selected = mapped;

                options.Add(mapped);
            }

            return new GarageNovaPartsPanelViewModel(
                source.ActiveSlot,
                source.SearchText,
                source.CountText,
                source.SelectedNameText,
                selected != null ? BuildSelectedDetailText(selected) : source.SelectedDetailText,
                source.SelectedPreviewPrefab,
                source.SelectedAlignment,
                options);
        }

        private static string BuildEnergyDetail(GaragePanelCatalog catalog, GarageNovaPartOptionViewModel option)
        {
            return option.Slot switch
            {
                GarageNovaPartPanelSlot.Firepower when catalog.FindFirepower(option.Id) is { } part =>
                    $"EN {part.EnergyCost} | ATK {part.AttackDamage:0} | RNG {part.Range:0.0}",
                GarageNovaPartPanelSlot.Mobility when catalog.FindMobility(option.Id) is { } part =>
                    $"EN {part.EnergyCost} | MOV {part.MoveRange:0.0} | HP+ {part.HpBonus:0}",
                _ when catalog.FindFrame(option.Id) is { } part =>
                    $"EN {part.EnergyCost} | HP {part.BaseHp:0} | DEF {part.Defense:0}",
                _ => option.DetailText ?? string.Empty,
            };
        }

        private static string BuildSelectedDetailText(GarageNovaPartOptionViewModel selected)
        {
            var source = string.IsNullOrWhiteSpace(selected.SourcePath) ? selected.Id : selected.SourcePath;
            return $"{selected.DetailText}\n{source}";
        }
    }
}
