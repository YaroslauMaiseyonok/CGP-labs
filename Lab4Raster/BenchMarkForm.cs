using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Lab4Raster
{
    public class BenchmarkForm : Form
    {
        public BenchmarkForm(BenchmarkResult result)
        {
            Text = "Результаты бенчмарка";
            Size = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterParent;

            var tabs = new TabControl
            {
                Dock = DockStyle.Fill
            };

            var lineCountSeries = new ChartSeries
            {
                Name = "Линии (Брезенхем)",
                Color = Color.Blue,
                Points = result.LineCountSeries
                    .Select(p => new PointF(p.LineCount, (float)p.ElapsedMs))
                    .ToList()
            };

            tabs.TabPages.Add(CreateChartTab(
                "Линии: количество",
                "Количество линий",
                "Время, мс",
                null,
                lineCountSeries));

            var lengthSeries = new ChartSeries
            {
                Name = "Линии: длина (20 000 шт.)",
                Color = Color.Green,
                Points = result.LengthSeries
                    .Select(p => new PointF((float)p.AvgLineLength, (float)p.ElapsedMs))
                    .ToList()
            };

            tabs.TabPages.Add(CreateChartTab(
                "Линии: длина",
                "Средняя длина линии, пикселей",
                "Время, мс",
                null,
                lengthSeries));

            var circleSeries = new ChartSeries
            {
                Name = "Окружности (Брезенхем)",
                Color = Color.Red,
                Points = result.CircleCountSeries
                    .Select(p => new PointF(p.CircleCount, (float)p.ElapsedMs))
                    .ToList()
            };

            tabs.TabPages.Add(CreateChartTab(
                "Окружности: количество",
                "Количество окружностей",
                "Время, мс",
                null,
                circleSeries));

            var algorithmSeries = new ChartSeries
            {
                Name = "Время, мс",
                Color = Color.Purple,
                Points = result.AlgorithmComparison
                    .Select((p, index) => new PointF(index, (float)p.Ms))
                    .ToList()
            };

            tabs.TabPages.Add(CreateChartTab(
                "Сравнение алгоритмов",
                string.Format("{0} случайных отрезков, один и тот же набор для всех алгоритмов", result.ComparisonLineCount),
                "Время, мс",
                result.AlgorithmComparison.Select(p => p.Name).ToList(),
                algorithmSeries));

            var reportPage = new TabPage("Отчет");

            var reportBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font(FontFamily.GenericMonospace, 9f),
                Text = BuildReport(result)
            };

            reportPage.Controls.Add(reportBox);
            tabs.TabPages.Add(reportPage);

            Controls.Add(tabs);

            reportBox.SelectionStart = 0;
            reportBox.SelectionLength = 0;
        }

        private TabPage CreateChartTab(
            string title,
            string xTitle,
            string yTitle,
            IList<string> categories,
            params ChartSeries[] series)
        {
            var page = new TabPage(title);

            var chart = new ChartPanel
            {
                Dock = DockStyle.Fill,
                Title = title,
                XTitle = xTitle,
                YTitle = yTitle
            };

            if (categories != null)
            {
                foreach (string category in categories)
                    chart.Categories.Add(category);
            }

            foreach (var s in series)
            {
                chart.Series.Add(s);
            }

            page.Controls.Add(chart);

            return page;
        }

        private static AlgorithmComparisonPoint Find(BenchmarkResult result, string name)
        {
            return result.AlgorithmComparison.FirstOrDefault(p => p.Name == name);
        }

        private static string NsPerCall(double ms, long calls)
        {
            if (calls <= 0)
                return "-";

            return (ms * 1000000.0 / calls).ToString("F2");
        }

        private string BuildReport(BenchmarkResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Лабораторная работа 4. Отчет по бенчмарку.");
            sb.AppendLine();
            sb.AppendLine($"Размер виртуального растра: {result.Width}x{result.Height}");
            sb.AppendLine("Методика: Stopwatch (высокоточный таймер); координаты примитивов генерируются");
            sb.AppendLine("ДО замера; перед измерением выполняется холостой прогон (JIT, кэш); в таблицах -");
            sb.AppendLine($"минимум из нескольких повторов (серии: {result.Repeats}, сравнение алгоритмов: {result.ComparisonRepeats}).");
            sb.AppendLine("Время замера не включает UI и очистку растра.");
            sb.AppendLine();

            if (result.FullStress != null)
            {
                var f = result.FullStress;

                sb.AppendLine($"1. Полный стресс-тест ({f.LineCount} линий + {f.CircleCount} окружностей):");
                sb.AppendLine($"   Время: {f.ElapsedMs:F2} мс");
                sb.AppendLine($"   Вызовов SetPixel: {f.SetPixelCalls}");
                sb.AppendLine($"   Закрашено пикселей: {f.ChangedPixels}");
                sb.AppendLine($"   Средняя длина линии: {f.AvgLineLength:F2}; средний радиус окружности: {f.AvgCircleRadius:F2}");
                sb.AppendLine("   Распределение линий по октантам (номер = floor(atan2(dy, dx) / 45°) по модулю 8, ось Y растра направлена вниз):");
                sb.AppendLine("   " + string.Join(", ", result.OctantCounts.Select((c, i) => $"{i}: {c}")));
                sb.AppendLine();
            }

            sb.AppendLine($"2. Сравнение алгоритмов ({result.ComparisonLineCount} случайных линий):");
            sb.AppendLine("   Алгоритм;       Мс;       SetPixel;   Закрашено;  нс/SetPixel;  Отн. Брезенхема");

            var bres = Find(result, "Брезенхем");

            foreach (var p in result.AlgorithmComparison)
            {
                string ratio = bres != null && bres.Ms > 0 ? (p.Ms / bres.Ms).ToString("F2") + "x" : "-";
                sb.AppendLine($"   {p.Name,-14}; {p.Ms,8:F2}; {p.SetPixelCalls,10}; {p.ChangedPixels,10}; {NsPerCall(p.Ms, p.SetPixelCalls),10}; {ratio,8}");
            }

            sb.AppendLine();

            AppendSeries(sb, "3. Зависимость времени от количества линий (Брезенхем):", result.LineCountSeries, p => p.LineCount.ToString());
            AppendSeries(sb, "4. Зависимость времени от средней длины линии (20 000 линий):", result.LengthSeries, p => p.AvgLineLength.ToString("F2"));
            AppendSeries(sb, "5. Зависимость времени от количества окружностей (Брезенхем):", result.CircleCountSeries, p => p.CircleCount.ToString());

            AppendAnalysis(sb, result);

            return sb.ToString();
        }

        private static void AppendSeries(StringBuilder sb, string caption, List<BenchmarkPoint> points, Func<BenchmarkPoint, string> xSelector)
        {
            sb.AppendLine(caption);
            sb.AppendLine("   X;            Мс;       SetPixel;   нс/SetPixel");

            foreach (var p in points)
            {
                sb.AppendLine($"   {xSelector(p),-12}; {p.ElapsedMs,8:F2}; {p.SetPixelCalls,10}; {NsPerCall(p.ElapsedMs, p.SetPixelCalls),10}");
            }

            sb.AppendLine("   Почти постоянное значение нс/SetPixel означает линейный рост времени:");
            sb.AppendLine("   T ~ (число примитивов) x (длина) ~ число вызовов SetPixel.");
            sb.AppendLine();
        }

        private static void AppendAnalysis(StringBuilder sb, BenchmarkResult result)
        {
            var step = Find(result, "Пошаговый");
            var dda = Find(result, "ЦДА");
            var bres = Find(result, "Брезенхем");
            var wu = Find(result, "Ву");

            sb.AppendLine("АНАЛИТИЧЕСКИЙ РАЗБОР:");
            sb.AppendLine();

            sb.AppendLine("6. ЦДА и Брезенхем: почему ЦДА (в теории) медленнее.");
            sb.AppendLine("   Число шагов у обоих одинаково: N = max(|dx|, |dy|) + 1, то есть различается только стоимость одного шага.");
            sb.AppendLine("   ЦДА на каждом шаге: 2 сложения double (x += xInc; y += yInc), 2 вызова Math.Round(..., AwayFromZero)");
            sb.AppendLine("   и 2 преобразования double -> int. Сложение double имеет задержку ~3-5 тактов против 1 такта у целого,");
            sb.AppendLine("   а Math.Round - это вызов функции, не инструкция процессора.");
            sb.AppendLine("   Брезенхем на каждом шаге: сложения/сравнения int (error, e2 = 2*error - сдвиг), без деления, без округления.");
            sb.AppendLine("   Накопление ошибки округления у ЦДА также не нулевое: x += xInc выполняется N раз с погрешностью ~1e-16 на шаг,");
            sb.AppendLine("   тогда как у Брезенхема вся арифметика точная (целочисленная).");
            sb.AppendLine("   Пошаговый алгоритм дополнительно делает деление t = i / N и умножение dy * t на каждом шаге.");
            sb.AppendLine();

            if (dda != null && bres != null && bres.Ms > 0)
            {
                double ratio = dda.Ms / bres.Ms;

                if (ratio > 1.05)
                {
                    sb.AppendLine($"   ИЗМЕРЕНИЕ: ЦДА = {dda.Ms:F2} мс, Брезенхем = {bres.Ms:F2} мс, ЦДА медленнее в {ratio:F2} раза - теория подтверждена.");
                }
                else
                {
                    sb.AppendLine($"   ИЗМЕРЕНИЕ: ЦДА = {dda.Ms:F2} мс, Брезенхем = {bres.Ms:F2} мс (отношение {ratio:F2}).");
                    sb.AppendLine("   Разница на этом компьютере мала: в обоих алгоритмах заметную часть времени занимает общий вызов SetPixel");
                    sb.AppendLine("   (проверка границ, запись в массив), а современный процессор выполняет вещественные операции конвейерно.");
                    sb.AppendLine("   Преимущество Брезенхема проявляется в числе операций на шаг, а не обязательно в абсолютных миллисекундах.");
                }
            }

            if (step != null && bres != null && bres.Ms > 0)
            {
                sb.AppendLine($"   Пошаговый = {step.Ms:F2} мс ({step.Ms / bres.Ms:F2}x от Брезенхема).");
            }

            sb.AppendLine();

            sb.AppendLine("7. Сглаживание (алгоритм Ву).");
            sb.AppendLine("   На каждом шаге по основной оси Ву вычисляет дробную часть координаты (FPart) и закрашивает ДВА соседних пикселя");
            sb.AppendLine("   с яркостями 1 - frac и frac, поэтому вызовов SetPixel примерно вдвое больше, чем у Брезенхема.");

            if (wu != null && bres != null && bres.Ms > 0 && bres.SetPixelCalls > 0)
            {
                sb.AppendLine($"   ИЗМЕРЕНИЕ: Ву = {wu.Ms:F2} мс ({wu.Ms / bres.Ms:F2}x от Брезенхема); вызовов SetPixel: {wu.SetPixelCalls} против {bres.SetPixelCalls} ({(double)wu.SetPixelCalls / bres.SetPixelCalls:F2}x).");
            }

            sb.AppendLine("   Для целых концов крайние пиксели получают яркость 0.5: покрытие пикселя отрезком по основной оси равно половине.");
            sb.AppendLine();

            sb.AppendLine("8. Алгоритм Кастла-Питвея (эллипсы).");
            sb.AppendLine("   Реализован целочисленный алгоритм Зингла (обобщение Брезенхема на эллипс): ошибка накапливается в long,");
            sb.AppendLine("   вещественная арифметика и корни не используются, рисуются сразу 4 симметричные точки.");
            sb.AppendLine("   Для узких эллипсов (например, 1x40) после основного цикла дорисовывается \"кончик\" эллипса.");
        }
    }
}
