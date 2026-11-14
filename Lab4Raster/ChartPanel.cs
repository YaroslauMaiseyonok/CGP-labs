using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Lab4Raster
{
    public sealed class ChartSeries
    {
        public string Name { get; set; } = "";

        public Color Color { get; set; } = Color.Black;

        public List<PointF> Points { get; set; } = new List<PointF>();
    }

    public class ChartPanel : Control
    {
        public ChartPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw, 
                true);

            BackColor = Color.White;
        }

        public string Title { get; set; } = "";

        public string XTitle { get; set; } = "";

        public string YTitle { get; set; } = "";

        public List<ChartSeries> Series { get; } = new List<ChartSeries>();
        public List<string> Categories { get; } = new List<string>();

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);

            var allPoints = Series
                .SelectMany(s => s.Points)
                .ToList();

            if (allPoints.Count == 0)
            {
                e.Graphics.DrawString("Нет данных", Font, Brushes.Black, 10, 10);
                return;
            }

            float left = 70;
            float top = 35;
            float right = Width - 25;
            float bottom = Height - 55;

            if (right <= left + 10 || bottom <= top + 10)
                return;

            if (Categories.Count > 0)
                PaintBars(e.Graphics, allPoints, left, top, right, bottom);
            else
                PaintLines(e.Graphics, allPoints, left, top, right, bottom);
        }

        #region Линейный график

        private void PaintLines(Graphics g, List<PointF> allPoints, float left, float top, float right, float bottom)
        {
            float minX = allPoints.Min(p => p.X);
            float maxX = allPoints.Max(p => p.X);

            float minY = allPoints.Min(p => p.Y);
            float maxY = allPoints.Max(p => p.Y);

            bool nonNegative = minY >= 0f;

            if (Math.Abs(maxX - minX) < 1e-6f)
            {
                minX -= 1;
                maxX += 1;
            }

            if (Math.Abs(maxY - minY) < 1e-6f)
            {
                minY -= 1;
                maxY += 1;
            }

            float rangeX = maxX - minX;
            float rangeY = maxY - minY;

            maxY += rangeY * 0.05f;
            minY = nonNegative ? 0f : minY - rangeY * 0.05f;

            rangeY = maxY - minY;

            g.DrawString(Title, Font, Brushes.Black, left, 8);

            using (var gridPen = new Pen(Color.FromArgb(235, 235, 235)))
            using (var axisPen = new Pen(Color.Black))
            using (var smallFont = new Font(FontFamily.GenericSansSerif, 8f))
            {
                // Сетка и подписи
                for (int i = 0; i <= 5; i++)
                {
                    float valueY = minY + rangeY * i / 5f;
                    float y = bottom - (bottom - top) * i / 5f;

                    g.DrawLine(gridPen, left, y, right, y);
                    g.DrawString(Format(valueY), smallFont, Brushes.Black, 2, y - 7);

                    float valueX = minX + rangeX * i / 5f;
                    float x = left + (right - left) * i / 5f;

                    g.DrawLine(gridPen, x, top, x, bottom);
                    g.DrawString(Format(valueX), smallFont, Brushes.Black, x - 12, bottom + 4);
                }

                g.DrawLine(axisPen, left, top, left, bottom);
                g.DrawLine(axisPen, left, bottom, right, bottom);

                g.DrawString(XTitle, smallFont, Brushes.Black, left, bottom + 20);
                g.DrawString(YTitle, smallFont, Brushes.Black, 2, top - 20);

                // Серии
                foreach (var series in Series)
                {
                    if (series.Points.Count == 0)
                        continue;

                    using (var pen = new Pen(series.Color, 2f))
                    using (var brush = new SolidBrush(series.Color))
                    {
                        for (int i = 0; i < series.Points.Count; i++)
                        {
                            PointF point = series.Points[i];

                            float sx = left + (point.X - minX) / rangeX * (right - left);
                            float sy = bottom - (point.Y - minY) / rangeY * (bottom - top);

                            if (i > 0)
                            {
                                PointF prev = series.Points[i - 1];

                                float px = left + (prev.X - minX) / rangeX * (right - left);
                                float py = bottom - (prev.Y - minY) / rangeY * (bottom - top);

                                g.DrawLine(pen, px, py, sx, sy);
                            }

                            g.FillEllipse(brush, sx - 3, sy - 3, 6, 6);
                        }
                    }
                }

                DrawLegend(g, smallFont, right, top);
            }
        }

        #endregion

        #region Столбчатая диаграмма

        private void PaintBars(Graphics g, List<PointF> allPoints, float left, float top, float right, float bottom)
        {
            float maxY = allPoints.Max(p => p.Y);

            if (maxY <= 0f)
                maxY = 1f;

            maxY *= 1.12f;

            g.DrawString(Title, Font, Brushes.Black, left, 8);

            using (var gridPen = new Pen(Color.FromArgb(235, 235, 235)))
            using (var axisPen = new Pen(Color.Black))
            using (var smallFont = new Font(FontFamily.GenericSansSerif, 8f))
            using (var centerFormat = new StringFormat { Alignment = StringAlignment.Center })
            {
                for (int i = 0; i <= 5; i++)
                {
                    float valueY = maxY * i / 5f;
                    float y = bottom - (bottom - top) * i / 5f;

                    g.DrawLine(gridPen, left, y, right, y);
                    g.DrawString(Format(valueY), smallFont, Brushes.Black, 2, y - 7);
                }

                g.DrawLine(axisPen, left, top, left, bottom);
                g.DrawLine(axisPen, left, bottom, right, bottom);

                g.DrawString(YTitle, smallFont, Brushes.Black, 2, top - 20);

                int categoryCount = Categories.Count;
                int seriesCount = Math.Max(1, Series.Count);

                float groupWidth = (right - left) / categoryCount;
                float barWidth = groupWidth * 0.6f / seriesCount;

                for (int c = 0; c < categoryCount; c++)
                {
                    float groupLeft = left + groupWidth * c;
                    float groupCenter = groupLeft + groupWidth / 2f;

                    g.DrawString(Categories[c], smallFont, Brushes.Black, groupCenter, bottom + 6, centerFormat);

                    float barsLeft = groupCenter - barWidth * seriesCount / 2f;

                    for (int s = 0; s < Series.Count; s++)
                    {
                        PointF? found = null;

                        foreach (PointF p in Series[s].Points)
                        {
                            if ((int)Math.Round(p.X) == c)
                            {
                                found = p;
                                break;
                            }
                        }

                        if (found == null)
                            continue;

                        float value = found.Value.Y;
                        float barHeight = (bottom - top) * value / maxY;
                        float bx = barsLeft + barWidth * s;

                        using (var brush = new SolidBrush(Series[s].Color))
                        {
                            g.FillRectangle(brush, bx, bottom - barHeight, barWidth, barHeight);
                        }

                        g.DrawString(Format(value), smallFont, Brushes.Black, bx + barWidth / 2f, bottom - barHeight - 14, centerFormat);
                    }
                }

                if (!string.IsNullOrEmpty(XTitle))
                    g.DrawString(XTitle, smallFont, Brushes.Black, left, bottom + 24);

                if (Series.Count > 1)
                    DrawLegend(g, smallFont, right, top);
            }
        }

        #endregion

        private void DrawLegend(Graphics g, Font smallFont, float right, float top)
        {
            float legendY = top + 5;

            foreach (var series in Series)
            {
                using (var brush = new SolidBrush(series.Color))
                {
                    g.FillRectangle(brush, right - 160, legendY, 10, 10);
                    g.DrawString(series.Name, smallFont, Brushes.Black, right - 145, legendY - 1);
                }

                legendY += 15;
            }
        }

        private static string Format(float value)
        {
            if (Math.Abs(value) >= 1000)
                return value.ToString("0");

            return value.ToString("0.##");
        }
    }
}
