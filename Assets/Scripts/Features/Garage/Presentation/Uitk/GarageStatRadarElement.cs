using UnityEngine;
using UnityEngine.UIElements;

namespace Features.Garage.Presentation
{
    internal sealed class GarageStatRadarElement : VisualElement
    {
        private const int AxisCount = 7;
        private const float GraphRadiusFactor = 0.3f;
        private const float LabelRadiusFactor = 0.42f;
        private const float LabelWidth = 34f;
        private const float LabelHeight = 14f;
        private static readonly string[] AxisShortLabels =
        {
            "ATK",
            "ASPD",
            "RNG",
            "HP",
            "DEF",
            "SPD",
            "MOV"
        };

        private readonly float[] _current = new float[AxisCount];
        private readonly float[] _previous = new float[AxisCount];
        private readonly Label[] _axisLabels = new Label[AxisCount];
        private bool _hasCurrent;
        private bool _hasPrevious;

        public GarageStatRadarElement()
        {
            for (int i = 0; i < AxisCount; i++)
            {
                var label = new Label(AxisShortLabels[i])
                {
                    name = $"StatRadarAxisLabel{i + 1:00}",
                    pickingMode = PickingMode.Ignore
                };
                label.AddToClassList("stat-radar-label");
                label.style.position = Position.Absolute;
                label.style.width = LabelWidth;
                label.style.height = LabelHeight;
                Add(label);
                _axisLabels[i] = label;
            }

            RegisterCallback<GeometryChangedEvent>(_ => PositionAxisLabels());
            generateVisualContent += Draw;
        }

        public void Render(GarageStatRadarViewModel radar)
        {
            _hasCurrent = radar != null;
            _hasPrevious = radar?.HasPrevious == true;

            for (int i = 0; i < AxisCount; i++)
            {
                _current[i] = _hasCurrent && i < radar.CurrentValues.Length ? radar.CurrentValues[i] : 0f;
                _previous[i] = _hasPrevious && i < radar.PreviousValues.Length ? radar.PreviousValues[i] : 0f;
            }

            PositionAxisLabels();
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            var rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * GraphRadiusFactor;

            DrawGrid(painter, center, radius);
            if (_hasPrevious)
                DrawPolygon(painter, center, radius, _previous, new Color(0.55f, 0.65f, 0.75f, 0.28f), new Color(0.7f, 0.78f, 0.85f, 0.45f));
            if (_hasCurrent)
                DrawPolygon(painter, center, radius, _current, new Color(0.37f, 0.71f, 1f, 0.32f), new Color(0.37f, 0.71f, 1f, 0.92f));
        }

        private void PositionAxisLabels()
        {
            var rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
                return;

            var center = rect.center;
            var radius = Mathf.Min(rect.width, rect.height) * LabelRadiusFactor;
            float maxLeft = Mathf.Max(rect.xMin, rect.xMax - LabelWidth);
            float maxTop = Mathf.Max(rect.yMin, rect.yMax - LabelHeight);
            for (int i = 0; i < AxisCount; i++)
            {
                var point = GetPoint(center, radius, i, 1f);
                _axisLabels[i].style.left = Mathf.Clamp(point.x - LabelWidth * 0.5f, rect.xMin, maxLeft);
                _axisLabels[i].style.top = Mathf.Clamp(point.y - LabelHeight * 0.5f, rect.yMin, maxTop);
            }
        }

        private static void DrawGrid(Painter2D painter, Vector2 center, float radius)
        {
            painter.lineWidth = 1f;
            painter.strokeColor = new Color(0.37f, 0.71f, 1f, 0.18f);

            for (int ring = 1; ring <= 3; ring++)
            {
                DrawPath(painter, center, radius * ring / 3f, null);
                painter.Stroke();
            }

            for (int i = 0; i < AxisCount; i++)
            {
                var point = GetPoint(center, radius, i, 1f);
                painter.BeginPath();
                painter.MoveTo(center);
                painter.LineTo(point);
                painter.Stroke();
            }
        }

        private static void DrawPolygon(
            Painter2D painter,
            Vector2 center,
            float radius,
            float[] values,
            Color fill,
            Color stroke)
        {
            DrawPath(painter, center, radius, values);
            painter.fillColor = fill;
            painter.Fill();

            DrawPath(painter, center, radius, values);
            painter.lineWidth = 2f;
            painter.strokeColor = stroke;
            painter.Stroke();
        }

        private static void DrawPath(Painter2D painter, Vector2 center, float radius, float[] values)
        {
            painter.BeginPath();
            for (int i = 0; i < AxisCount; i++)
            {
                float value = values == null ? 1f : Mathf.Clamp01(values[i]);
                var point = GetPoint(center, radius, i, value);
                if (i == 0)
                    painter.MoveTo(point);
                else
                    painter.LineTo(point);
            }

            painter.ClosePath();
        }

        private static Vector2 GetPoint(Vector2 center, float radius, int index, float value)
        {
            float angle = -90f + index * (360f / AxisCount);
            float radians = angle * Mathf.Deg2Rad;
            return center + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius * value;
        }
    }
}
