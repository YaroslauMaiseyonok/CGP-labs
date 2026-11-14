using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lab4Raster
{
    public class BenchmarkPoint
    {
        public string Label { get; set; }

        public int LineCount { get; set; }

        public int CircleCount { get; set; }

        public int PrimitiveCount => LineCount + CircleCount;

        public double AvgLineLength { get; set; }

        public double AvgCircleRadius { get; set; }

        public double ElapsedMs { get; set; }

        public long SetPixelCalls { get; set; }

        public long ChangedPixels { get; set; }
    }

    public class AlgorithmComparisonPoint
    {
        public string Name { get; set; }

        public double Ms { get; set; }

        public long SetPixelCalls { get; set; }

        public long ChangedPixels { get; set; }
    }

    public class BenchmarkResult
    {
        public int Width { get; set; }

        public int Height { get; set; }

        /// <summary>Сколько раз повторялось каждое измерение (в таблицах берётся минимум).</summary>
        public int Repeats { get; set; }

        /// <summary>Сколько повторов использовалось при сравнении алгоритмов.</summary>
        public int ComparisonRepeats { get; set; }

        /// <summary>Сколько линий сравнения алгоритмов использовалось.</summary>
        public int ComparisonLineCount { get; set; }

        public List<BenchmarkPoint> LineCountSeries { get; set; } = new List<BenchmarkPoint>();

        public List<BenchmarkPoint> LengthSeries { get; set; } = new List<BenchmarkPoint>();

        public List<BenchmarkPoint> CircleCountSeries { get; set; } = new List<BenchmarkPoint>();

        public List<AlgorithmComparisonPoint> AlgorithmComparison { get; set; } = new List<AlgorithmComparisonPoint>();

        public BenchmarkPoint FullStress { get; set; }

        /// <summary>Сколько линий "солнца" попало в каждый из 8 октантов (полная нагрузка).</summary>
        public int[] OctantCounts { get; set; } = new int[8];
    }

    public static class BenchmarkRunner
    {
        private const int SeriesRepeats = 3;
        private const int ComparisonRepeatCount = 7;
        private const int ComparisonLines = 20000;

        private static readonly int[] LineCounts =
        {
            2000,
            5000,
            10000,
            25000,
            50000,
            100000
        };

        private static readonly int[] CircleCounts =
        {
            1000,
            2000,
            5000,
            10000,
            25000,
            50000
        };

        private static readonly double[] RadiusFractions =
        {
            0.10,
            0.20,
            0.40,
            0.60,
            0.80,
            1.00
        };

        #region Наборы примитивов (генерируются ДО замера времени)

        private sealed class LineSet
        {
            public int[] X0, Y0, X1, Y1;
            public double AvgLength;
            public int[] OctantCounts = new int[8];
        }

        private sealed class CircleSet
        {
            public int[] Cx, Cy, R;
            public double AvgRadius;
        }

        #endregion

        public static BenchmarkResult RunAll(int width, int height)
        {
            var result = new BenchmarkResult
            {
                Width = width,
                Height = height,
                Repeats = SeriesRepeats,
                ComparisonRepeats = ComparisonRepeatCount,
                ComparisonLineCount = ComparisonLines
            };

            result.LineCountSeries = RunLineSeries(width, height, LineCounts, 0.95);
            result.LengthSeries = RunLengthSeries(width, height, 20000, RadiusFractions);
            result.CircleCountSeries = RunCircleSeries(width, height, CircleCounts);
            result.AlgorithmComparison = RunAlgorithmComparison(width, height, ComparisonLines);
            result.FullStress = RunFullStress(width, height, 100000, 50000, result.OctantCounts);

            return result;
        }

        public static BenchmarkPoint RunFullStress(int width, int height, int lineCount, int circleCount)
        {
            return RunFullStress(width, height, lineCount, circleCount, new int[8]);
        }

        private static BenchmarkPoint RunFullStress(int width, int height, int lineCount, int circleCount, int[] octantCounts)
        {
            var grid = new VirtualGrid(width, height);

            LineSet lines = CreateSunLines(width, height, lineCount, 12345, 0.98);
            CircleSet circles = CreateRandomCircles(width, height, circleCount, 54321);

            long calls, changed;
            double ms = MeasureMin(grid, () =>
            {
                DrawLines(grid, lines);
                DrawCircles(grid, circles);
            }, SeriesRepeats, out calls, out changed);

            for (int i = 0; i < 8 && i < octantCounts.Length; i++)
                octantCounts[i] = lines.OctantCounts[i];

            return new BenchmarkPoint
            {
                Label = "Полная нагрузка",
                LineCount = lineCount,
                CircleCount = circleCount,
                AvgLineLength = lines.AvgLength,
                AvgCircleRadius = circles.AvgRadius,
                ElapsedMs = ms,
                SetPixelCalls = calls,
                ChangedPixels = changed
            };
        }

        private static List<BenchmarkPoint> RunLineSeries(int width, int height, int[] counts, double maxRadiusFraction)
        {
            var list = new List<BenchmarkPoint>();
            var grid = new VirtualGrid(width, height);

            foreach (int count in counts)
            {
                LineSet lines = CreateSunLines(width, height, count, 12345, maxRadiusFraction);

                long calls, changed;
                double ms = MeasureMin(grid, () => DrawLines(grid, lines), SeriesRepeats, out calls, out changed);

                list.Add(new BenchmarkPoint
                {
                    Label = $"Линии: {count}",
                    LineCount = count,
                    CircleCount = 0,
                    AvgLineLength = lines.AvgLength,
                    ElapsedMs = ms,
                    SetPixelCalls = calls,
                    ChangedPixels = changed
                });
            }

            return list;
        }

        private static List<BenchmarkPoint> RunLengthSeries(int width, int height, int fixedLineCount, double[] fractions)
        {
            var list = new List<BenchmarkPoint>();
            var grid = new VirtualGrid(width, height);

            foreach (double fraction in fractions)
            {
                LineSet lines = CreateSunLines(width, height, fixedLineCount, 12345, fraction);

                long calls, changed;
                double ms = MeasureMin(grid, () => DrawLines(grid, lines), SeriesRepeats, out calls, out changed);

                list.Add(new BenchmarkPoint
                {
                    Label = $"Длина: {lines.AvgLength:F2}",
                    LineCount = fixedLineCount,
                    CircleCount = 0,
                    AvgLineLength = lines.AvgLength,
                    ElapsedMs = ms,
                    SetPixelCalls = calls,
                    ChangedPixels = changed
                });
            }

            return list;
        }

        private static List<BenchmarkPoint> RunCircleSeries(int width, int height, int[] counts)
        {
            var list = new List<BenchmarkPoint>();
            var grid = new VirtualGrid(width, height);

            foreach (int count in counts)
            {
                CircleSet circles = CreateRandomCircles(width, height, count, 54321);

                long calls, changed;
                double ms = MeasureMin(grid, () => DrawCircles(grid, circles), SeriesRepeats, out calls, out changed);

                list.Add(new BenchmarkPoint
                {
                    Label = $"Окружности: {count}",
                    LineCount = 0,
                    CircleCount = count,
                    AvgCircleRadius = circles.AvgRadius,
                    ElapsedMs = ms,
                    SetPixelCalls = calls,
                    ChangedPixels = changed
                });
            }

            return list;
        }

        private static List<AlgorithmComparisonPoint> RunAlgorithmComparison(int width, int height, int count)
        {
            // Одни и те же 20 000 случайных отрезков для всех четырёх алгоритмов.
            var random = new Random(271828);

            int[] x0 = new int[count];
            int[] y0 = new int[count];
            int[] x1 = new int[count];
            int[] y1 = new int[count];

            for (int i = 0; i < count; i++)
            {
                x0[i] = random.Next(width);
                y0[i] = random.Next(height);
                x1[i] = random.Next(width);
                y1[i] = random.Next(height);

                if (x0[i] == x1[i] && y0[i] == y1[i])
                {
                    x1[i] = (x1[i] + 1) % width;
                }
            }

            var grid = new VirtualGrid(width, height);
            var list = new List<AlgorithmComparisonPoint>();

            list.Add(MeasureAlgorithm("Пошаговый", grid, () =>
            {
                for (int i = 0; i < count; i++)
                    RasterEngine.StepLine(grid, x0[i], y0[i], x1[i], y1[i]);
            }));

            list.Add(MeasureAlgorithm("ЦДА", grid, () =>
            {
                for (int i = 0; i < count; i++)
                    RasterEngine.DdaLine(grid, x0[i], y0[i], x1[i], y1[i]);
            }));

            list.Add(MeasureAlgorithm("Брезенхем", grid, () =>
            {
                for (int i = 0; i < count; i++)
                    RasterEngine.BresenhamLine(grid, x0[i], y0[i], x1[i], y1[i]);
            }));

            list.Add(MeasureAlgorithm("Ву", grid, () =>
            {
                for (int i = 0; i < count; i++)
                    RasterEngine.WuLine(grid, x0[i], y0[i], x1[i], y1[i]);
            }));

            return list;
        }

        private static AlgorithmComparisonPoint MeasureAlgorithm(string name, VirtualGrid grid, Action work)
        {
            long calls, changed;
            double ms = MeasureMin(grid, work, ComparisonRepeatCount, out calls, out changed);

            return new AlgorithmComparisonPoint
            {
                Name = name,
                Ms = ms,
                SetPixelCalls = calls,
                ChangedPixels = changed
            };
        }

        private static double MeasureMin(VirtualGrid grid, Action work, int repeats, out long calls, out long changed)
        {
            grid.Clear();
            work();

            double best = double.MaxValue;
            calls = 0;
            changed = 0;

            for (int r = 0; r < repeats; r++)
            {
                grid.Clear();

                var stopwatch = Stopwatch.StartNew();
                work();
                stopwatch.Stop();

                double ms = stopwatch.Elapsed.TotalMilliseconds;

                if (ms < best)
                    best = ms;

                calls = grid.SetPixelCalls;
                changed = grid.ChangedPixels;
            }

            return best;
        }

        private static void DrawLines(VirtualGrid grid, LineSet lines)
        {
            int[] x0 = lines.X0, y0 = lines.Y0, x1 = lines.X1, y1 = lines.Y1;

            for (int i = 0; i < x0.Length; i++)
                RasterEngine.BresenhamLine(grid, x0[i], y0[i], x1[i], y1[i]);
        }

        private static void DrawCircles(VirtualGrid grid, CircleSet circles)
        {
            int[] cx = circles.Cx, cy = circles.Cy, r = circles.R;

            for (int i = 0; i < cx.Length; i++)
                RasterEngine.BresenhamCircle(grid, cx[i], cy[i], r[i]);
        }

        private static LineSet CreateSunLines(int width, int height, int count, int seed, double maxRadiusFraction)
        {
            var set = new LineSet
            {
                X0 = new int[Math.Max(0, count)],
                Y0 = new int[Math.Max(0, count)],
                X1 = new int[Math.Max(0, count)],
                Y1 = new int[Math.Max(0, count)]
            };

            if (count <= 0)
                return set;

            var random = new Random(seed);

            int cx = width / 2;
            int cy = height / 2;

            double baseRadius = Math.Min(width, height) / 2.0 - 2.0;

            if (baseRadius < 1.0)
                baseRadius = 1.0;

            double maxRadius = baseRadius * Math.Max(0.05, maxRadiusFraction);

            double lengthSum = 0.0;

            for (int i = 0; i < count; i++)
            {
                double angle = random.NextDouble() * Math.PI * 2.0;
                double r = maxRadius * (0.15 + 0.85 * random.NextDouble());

                int x1 = (int)Math.Round(cx + Math.Cos(angle) * r);
                int y1 = (int)Math.Round(cy + Math.Sin(angle) * r);

                set.X0[i] = cx;
                set.Y0[i] = cy;
                set.X1[i] = x1;
                set.Y1[i] = y1;

                double dx = x1 - cx;
                double dy = y1 - cy;

                lengthSum += Math.Sqrt(dx * dx + dy * dy);

                int octant = (int)Math.Floor(Math.Atan2(dy, dx) / (Math.PI / 4.0));

                if (octant < 0)
                    octant += 8;

                if (octant > 7)
                    octant = 7;

                set.OctantCounts[octant]++;
            }

            set.AvgLength = lengthSum / count;

            return set;
        }

        private static CircleSet CreateRandomCircles(int width, int height, int count, int seed)
        {
            var set = new CircleSet
            {
                Cx = new int[Math.Max(0, count)],
                Cy = new int[Math.Max(0, count)],
                R = new int[Math.Max(0, count)]
            };

            if (count <= 0)
                return set;

            var random = new Random(seed);

            int maxRadius = Math.Max(2, Math.Min(width, height) / 8);

            double radiusSum = 0.0;

            for (int i = 0; i < count; i++)
            {
                int r = random.Next(1, maxRadius + 1);

                int minX = r + 1;
                int maxX = width - r - 1;

                int minY = r + 1;
                int maxY = height - r - 1;

                set.Cx[i] = minX <= maxX ? random.Next(minX, maxX + 1) : width / 2;
                set.Cy[i] = minY <= maxY ? random.Next(minY, maxY + 1) : height / 2;
                set.R[i] = r;

                radiusSum += r;
            }

            set.AvgRadius = radiusSum / count;

            return set;
        }
    }
}
