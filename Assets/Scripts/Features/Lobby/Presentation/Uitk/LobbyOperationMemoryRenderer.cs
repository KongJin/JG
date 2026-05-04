using System.Collections.Generic;
using Shared.Ui;
using UnityEngine.UIElements;

namespace Features.Lobby.Presentation
{
    internal static class LobbyOperationMemoryRenderer
    {
        public static void Render(VisualElement recordsPage, LobbyOperationMemoryViewModel viewModel)
        {
            if (recordsPage == null)
                return;

            viewModel ??= LobbyOperationMemoryViewModel.Empty;
            RenderLatestOperation(recordsPage.Q<VisualElement>("LatestOperationCard"), viewModel.Latest);
            RenderRecentOperations(recordsPage.Q<VisualElement>("RecentOperations"), viewModel.RecentRows);
            RenderUnitTrace(recordsPage.Q<VisualElement>("UnitTrace"), viewModel.Trace);
        }

        private static void RenderLatestOperation(VisualElement card, LobbyOperationLatestViewModel viewModel)
        {
            if (card == null)
                return;

            viewModel ??= LobbyOperationLatestViewModel.Empty;
            card.Clear();
            if (!viewModel.HasRecord)
            {
                card.Add(CreateLabel("LATEST_OP", "memory-kicker"));
                card.Add(CreateLabel(viewModel.ResultText, viewModel.ResultClass));
                card.Add(CreateLabel(viewModel.PressureText, "memory-sitrep-text"));
                return;
            }

            var header = new VisualElement();
            header.AddToClassList("memory-card-header");
            var titleStack = new VisualElement();
            titleStack.Add(CreateLabel("LATEST_OP", "memory-kicker"));
            titleStack.Add(CreateLabel(viewModel.ResultText, viewModel.ResultClass));
            header.Add(titleStack);
            header.Add(CreateLabel(viewModel.TimeText, "memory-time"));
            card.Add(header);

            var stats = new VisualElement();
            stats.AddToClassList("memory-stat-grid");
            AddStat(stats, "생존", viewModel.SurvivalText, "memory-stat-value memory-stat-value--blue");
            AddStat(stats, "공세", viewModel.WaveText, "memory-stat-value");
            AddStat(stats, "코어", viewModel.CoreText, viewModel.CoreClass);
            AddStat(stats, "제거", viewModel.KillText, "memory-stat-value");
            card.Add(stats);

            var sitrep = new VisualElement();
            sitrep.AddToClassList("memory-sitrep");
            sitrep.Add(CreateLabel("SITREP", "memory-sitrep-label"));
            sitrep.Add(CreateLabel(viewModel.PressureText, "memory-sitrep-text"));
            card.Add(sitrep);
        }

        private static void RenderRecentOperations(
            VisualElement section,
            IReadOnlyList<LobbyOperationRowViewModel> rows)
        {
            if (section == null)
                return;

            section.Clear();
            section.Add(CreateLabel("RECENT OPERATIONS", "memory-section-title"));
            if (rows == null || rows.Count == 0)
            {
                var empty = new VisualElement();
                UitkElementUtility.AddClasses(empty, "operation-row operation-row--empty");
                empty.Add(CreateLabel("전적 기록 대기중", "operation-empty-text"));
                section.Add(empty);
                return;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                var viewModel = rows[i];
                var row = new VisualElement();
                UitkElementUtility.AddClasses(row, viewModel.RowClass);

                var line = new VisualElement();
                UitkElementUtility.AddClasses(line, viewModel.LineClass);
                row.Add(line);

                var main = new VisualElement();
                main.AddToClassList("operation-row-main");
                main.Add(CreateLabel(viewModel.TitleText, viewModel.TitleClass));
                main.Add(CreateLabel(viewModel.MetaText, "operation-meta"));
                row.Add(main);

                row.Add(CreateLabel(viewModel.CoreText, viewModel.CoreClass));
                section.Add(row);
            }
        }

        private static void RenderUnitTrace(VisualElement section, LobbyOperationTraceViewModel viewModel)
        {
            if (section == null)
                return;

            viewModel ??= LobbyOperationMemoryViewModel.Empty.Trace;
            section.Clear();
            section.Add(CreateLabel("기체 전적", "memory-section-title"));
            var chips = new VisualElement();
            chips.AddToClassList("memory-chip-row");
            chips.Add(CreateLabel(viewModel.CountChipText, "memory-chip"));
            chips.Add(CreateLabel("LOCAL FIRST", "memory-chip memory-chip--blue"));
            chips.Add(CreateLabel(viewModel.RecentDataChipText, "memory-chip memory-chip--orange"));
            section.Add(chips);
        }

        private static void AddStat(VisualElement parent, string label, string value, string valueClass)
        {
            var cell = new VisualElement();
            cell.AddToClassList("memory-stat-cell");
            cell.Add(CreateLabel(label, "memory-stat-label"));
            cell.Add(CreateLabel(value, valueClass));
            parent.Add(cell);
        }

        private static Label CreateLabel(string text, string className)
        {
            var label = UitkElementUtility.CreateLabel(text, className);
            label.style.color = UiThemeColors.TextPrimary;
            return label;
        }
    }
}
