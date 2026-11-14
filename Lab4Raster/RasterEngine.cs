using System;

namespace Lab4Raster
{
    public static class RasterEngine
    {
        private static int RoundToInt(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        #region Пошаговый
        public static void StepLine(IRasterTarget target, int x0, int y0, int x1, int y1, Action<string> logger = null)
        {
            if (target == null) return;

            int dx = x1 - x0, dy = y1 - y0;
            int adx = Math.Abs(dx), ady = Math.Abs(dy);

            if (adx == 0 && ady == 0) { target.SetPixel(x0, y0, 1.0); return; }

            if (adx >= ady)
            {
                int sx = dx >= 0 ? 1 : -1;
                for (int i = 0; i <= adx; i++)
                {
                    double t = (double)i / adx;
                    int x = x0 + sx * i;
                    int y = RoundToInt(y0 + dy * t);
                    target.SetPixel(x, y, 1.0);
                    if (logger != null && i < 20) logger($"Шаг {i}: x={x}, y={y} (t={t:F3})");
                }
            }
            else
            {
                int sy = dy >= 0 ? 1 : -1;
                for (int i = 0; i <= ady; i++)
                {
                    double t = (double)i / ady;
                    int y = y0 + sy * i;
                    int x = RoundToInt(x0 + dx * t);
                    target.SetPixel(x, y, 1.0);
                    if (logger != null && i < 20) logger($"Шаг {i}: x={x}, y={y} (t={t:F3})");
                }
            }
        }
        #endregion

        #region ЦДА
        public static void DdaLine(IRasterTarget target, int x0, int y0, int x1, int y1, Action<string> logger = null)
        {
            if (target == null) return;

            int dx = x1 - x0, dy = y1 - y0;
            int steps = Math.Max(Math.Abs(dx), Math.Abs(dy));

            if (steps == 0) { target.SetPixel(x0, y0, 1.0); return; }

            double xInc = (double)dx / steps;
            double yInc = (double)dy / steps;
            double x = x0, y = y0;

            for (int i = 0; i <= steps; i++)
            {
                int px = RoundToInt(x), py = RoundToInt(y);
                target.SetPixel(px, py, 1.0);
                if (logger != null && i < 20) logger($"Шаг {i}: x={x:F3}, y={y:F3} -> пиксель ({px}, {py})");
                x += xInc; y += yInc;
            }
        }
        #endregion

        #region Брезенхем (линия)
        public static void BresenhamLine(IRasterTarget target, int x0, int y0, int x1, int y1, Action<string> logger = null)
        {
            if (target == null) return;

            int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            int step = 0;

            while (true)
            {
                target.SetPixel(x0, y0, 1.0);
                if (logger != null && step < 20) logger($"Шаг {step}: x={x0}, y={y0}, error={error}");
                step++;

                if (x0 == x1 && y0 == y1) break;

                int e2 = 2 * error;
                if (e2 >= dy) { error += dy; x0 += sx; }
                if (e2 <= dx) { error += dx; y0 += sy; }
            }
        }
        #endregion

        #region Брезенхем (окружность)
        public static void BresenhamCircle(IRasterTarget target, int cx, int cy, int radius, Action<string> logger = null)
        {
            if (target == null) return;
            radius = Math.Abs(radius);

            if (radius == 0) { target.SetPixel(cx, cy, 1.0); return; }

            int x = 0, y = radius;
            int d = 3 - 2 * radius;
            int step = 0;

            while (x <= y)
            {
                PlotCircle8(target, cx, cy, x, y);
                if (logger != null && step < 20) logger($"Шаг {step}: x={x}, y={y}, d={d}");
                step++;

                if (d < 0) d += 4 * x + 6;
                else { d += 4 * (x - y) + 10; y--; }
                x++;
            }
        }

        private static void PlotCircle8(IRasterTarget target, int cx, int cy, int x, int y)
        {
            target.SetPixel(cx + x, cy + y, 1.0);
            if (x != 0)
                target.SetPixel(cx - x, cy + y, 1.0);

            if (y != 0)
            {
                target.SetPixel(cx + x, cy - y, 1.0);
                if (x != 0)
                    target.SetPixel(cx - x, cy - y, 1.0);
            }

            if (x != y)
            {
                target.SetPixel(cx + y, cy + x, 1.0);
                target.SetPixel(cx - y, cy + x, 1.0);

                if (x != 0)
                {
                    target.SetPixel(cx + y, cy - x, 1.0);
                    target.SetPixel(cx - y, cy - x, 1.0);
                }
            }
        }
        #endregion

        #region Ву
        public static void WuLine(IRasterTarget target, int x0, int y0, int x1, int y1, Action<string> logger = null)
        {
            if (target == null) return;
            if (x0 == x1 && y0 == y1) { target.SetPixel(x0, y0, 1.0); return; }

            double ax = x0, ay = y0, bx = x1, by = y1;
            bool steep = Math.Abs(by - ay) > Math.Abs(bx - ax);

            if (steep) { Swap(ref ax, ref ay); Swap(ref bx, ref by); }
            if (ax > bx) { Swap(ref ax, ref bx); Swap(ref ay, ref by); }

            double dx = bx - ax, dy = by - ay;
            double gradient = Math.Abs(dx) < 1e-12 ? 1.0 : dy / dx;

            if (logger != null) logger($"Градиент: {gradient:F3}, steep: {steep}");

            double xend = Math.Floor(ax + 0.5);
            double yend = ay + gradient * (xend - ax);
            double xgap = RFPart(ax + 0.5);
            int xpxl1 = (int)xend, ypxl1 = IPart(yend);

            if (steep)
            {
                SetWu(target, ypxl1, xpxl1, RFPart(yend) * xgap);
                SetWu(target, ypxl1 + 1, xpxl1, FPart(yend) * xgap);
            }
            else
            {
                SetWu(target, xpxl1, ypxl1, RFPart(yend) * xgap);
                SetWu(target, xpxl1, ypxl1 + 1, FPart(yend) * xgap);
            }

            double intery = yend + gradient;

            xend = Math.Floor(bx + 0.5);
            yend = by + gradient * (xend - bx);
            xgap = FPart(bx + 0.5);
            int xpxl2 = (int)xend, ypxl2 = IPart(yend);

            if (steep)
            {
                SetWu(target, ypxl2, xpxl2, RFPart(yend) * xgap);
                SetWu(target, ypxl2 + 1, xpxl2, FPart(yend) * xgap);
            }
            else
            {
                SetWu(target, xpxl2, ypxl2, RFPart(yend) * xgap);
                SetWu(target, xpxl2, ypxl2 + 1, FPart(yend) * xgap);
            }

            int step = 0;
            for (int x = xpxl1 + 1; x <= xpxl2 - 1; x++)
            {
                if (logger != null && step < 20)
                {
                    logger($"Шаг {step}: intery={intery:F3} -> пиксели Y={IPart(intery)} (I={RFPart(intery):F3}) и Y={IPart(intery) + 1} (I={FPart(intery):F3})");
                    step++;
                }
                if (steep)
                {
                    SetWu(target, IPart(intery), x, RFPart(intery));
                    SetWu(target, IPart(intery) + 1, x, FPart(intery));
                }
                else
                {
                    SetWu(target, x, IPart(intery), RFPart(intery));
                    SetWu(target, x, IPart(intery) + 1, FPart(intery));
                }
                intery += gradient;
            }
        }

        private static void SetWu(IRasterTarget target, int x, int y, double intensity)
        {
            if (intensity < 0.002) return;
            target.SetPixel(x, y, intensity);
        }

        private static void Swap(ref double a, ref double b) { double temp = a; a = b; b = temp; }
        private static double FPart(double value) => value - Math.Floor(value);
        private static double RFPart(double value) => 1.0 - FPart(value);
        private static int IPart(double value) => (int)Math.Floor(value);
        #endregion

        #region Кастл-Питвей (Алоис Зингл)
        public static void CastlePitewayEllipse(IRasterTarget target, int cx, int cy, int rx, int ry, Action<string> logger = null)
        {
            if (target == null) return;
            rx = Math.Abs(rx); ry = Math.Abs(ry);

            if (rx == 0 && ry == 0) { target.SetPixel(cx, cy, 1.0); return; }
            if (rx == 0) { BresenhamLine(target, cx, cy - ry, cx, cy + ry, logger); return; }
            if (ry == 0) { BresenhamLine(target, cx - rx, cy, cx + rx, cy, logger); return; }

            long a = rx * 2, b = ry * 2, b1 = b & 1;
            long dx = 4 * (1 - a) * b * b, dy = 4 * (b1 + 1) * a * a;
            long err = dx + dy + b1 * a * a, e2;

            int x0 = cx - rx, x1 = cx + rx;
            int y0 = cy - ry, y1 = cy + ry;

            y0 += (int)((b + 1) / 2); y1 = y0 - (int)b1;
            a *= 8 * a; b1 = 8 * b * b;

            int step = 0;
            do
            {
                target.SetPixel(x1, y0, 1.0);
                target.SetPixel(x0, y0, 1.0);
                target.SetPixel(x0, y1, 1.0);
                target.SetPixel(x1, y1, 1.0);

                if (logger != null && step < 20)
                {
                    logger($"Шаг {step}: Рамка: x0={x0}, x1={x1}, y0={y0}, y1={y1}, err={err}");
                    step++;
                }

                e2 = 2 * err;
                if (e2 <= dy) { y0++; y1--; err += dy += a; }
                if (e2 >= dx || 2 * err > dy) { x0++; x1--; err += dx += b1; }
            } while (x0 <= x1);


            while (y0 - y1 <= b)
            {
                target.SetPixel(x0 - 1, y0, 1.0);
                target.SetPixel(x1 + 1, y0++, 1.0);
                target.SetPixel(x0 - 1, y1, 1.0);
                target.SetPixel(x1 + 1, y1--, 1.0);
            }
        }
        #endregion
    }
}
