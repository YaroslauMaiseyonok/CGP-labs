using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Lab4Raster
{
    public sealed class GridPainter : Panel
    {
        private const int OriginOffset = 30;

        private VirtualGrid _grid;
        private Bitmap _pixelBitmap;
        private int _zoom = 6;
        private bool _showGrid = true;
        private bool _showAxes = true;
        private bool _showLabels = true;

        public GridPainter()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            UpdateStyles();

            BackColor = Color.White;
            AutoScroll = true;
        }

        #region Свойства

        public VirtualGrid Grid
        {
            get { return _grid; }
            set
            {
                _grid = value;
                UpdateScrollSize();
                RefreshPixels();
            }
        }

        public int Zoom
        {
            get { return _zoom; }
            set
            {
                int newZoom = Math.Max(1, Math.Min(64, value));

                if (newZoom == _zoom)
                    return;

                if (_grid == null)
                {
                    _zoom = newZoom;
                    return;
                }

                double cellX = (ClientSize.Width / 2.0 - AutoScrollPosition.X - OriginOffset) / _zoom;
                double cellY = (ClientSize.Height / 2.0 - AutoScrollPosition.Y - OriginOffset) / _zoom;

                _zoom = newZoom;
                UpdateScrollSize();
                ScrollToCell(cellX, cellY);
            }
        }

        public bool ShowGrid
        {
            get { return _showGrid; }
            set { _showGrid = value; Invalidate(); }
        }

        public bool ShowAxes
        {
            get { return _showAxes; }
            set { _showAxes = value; Invalidate(); }
        }

        public bool ShowLabels
        {
            get { return _showLabels; }
            set { _showLabels = value; Invalidate(); }
        }

        public string HoverText { get; private set; } = "";

        public event EventHandler HoverChanged;

        #endregion

        #region Координаты

        public int WorldToGridX(int worldX)
        {
            return _grid.Width / 2 + worldX;
        }

        public int WorldToGridY(int worldY)
        {
            return _grid.Height / 2 - worldY;
        }

        #endregion

        #region Управление
        public void RefreshPixels()
        {
            RebuildPixelBitmap();
            Invalidate();
        }
        public void CenterOnOrigin()
        {
            if (_grid == null)
                return;

            ScrollToCell(_grid.Width / 2 + 0.5, _grid.Height / 2 + 0.5);
        }

        private void ScrollToCell(double cellX, double cellY)
        {
            int x = (int)Math.Round(OriginOffset + cellX * _zoom - ClientSize.Width / 2.0);
            int y = (int)Math.Round(OriginOffset + cellY * _zoom - ClientSize.Height / 2.0);

            AutoScrollPosition = new Point(Math.Max(0, x), Math.Max(0, y));
            Invalidate();
        }

        private void UpdateScrollSize()
        {
            if (_grid == null)
                return;

            AutoScrollMinSize = new Size(
                _grid.Width * _zoom + 2 * OriginOffset,
                _grid.Height * _zoom + 2 * OriginOffset);

            Invalidate();
        }

        #endregion

        #region События мыши и прокрутки
        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            SetHoverText(DescribeCell(e.X, e.Y));
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            SetHoverText("");
        }

        private string DescribeCell(int mouseX, int mouseY)
        {
            if (_grid == null)
                return "";

            int gx = (int)Math.Floor((mouseX - (OriginOffset + AutoScrollPosition.X)) / (double)_zoom);
            int gy = (int)Math.Floor((mouseY - (OriginOffset + AutoScrollPosition.Y)) / (double)_zoom);

            if (gx < 0 || gy < 0 || gx >= _grid.Width || gy >= _grid.Height)
                return "";

            int worldX = gx - _grid.Width / 2;
            int worldY = _grid.Height / 2 - gy;

            return string.Format(
                "Курсор: клетка [{0}; {1}] = мировые ({2}; {3}), яркость {4:F2}",
                gx, gy, worldX, worldY, _grid.GetPixel(gx, gy));
        }

        private void SetHoverText(string text)
        {
            if (text == HoverText)
                return;

            HoverText = text;

            EventHandler handler = HoverChanged;

            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        #endregion

        #region Рисование

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);

            if (_grid == null)
                return;

            int zoom = _zoom;
            int ox = OriginOffset + AutoScrollPosition.X;
            int oy = OriginOffset + AutoScrollPosition.Y;
            Rectangle clip = e.ClipRectangle;
            int totalWidth = _grid.Width * zoom;
            int totalHeight = _grid.Height * zoom;

            DrawPixels(g, ox, oy, zoom, clip);

            if (_showGrid && zoom >= 4)
                DrawGridLines(g, ox, oy, zoom, clip, totalWidth, totalHeight);

            if (_showAxes)
                DrawAxes(g, ox, oy, zoom);

            if (_showLabels)
                DrawLabels(g, ox, oy, zoom, clip);

            g.DrawRectangle(Pens.Black, ox, oy, totalWidth, totalHeight);
        }

        private void RebuildPixelBitmap()
        {
            if (_grid == null)
                return;

            int width = _grid.Width;
            int height = _grid.Height;

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppPArgb);

            try
            {
                int stride = Math.Abs(data.Stride);
                byte[] buffer = new byte[stride * height];
                float[] pixels = _grid.Pixels;

                for (int y = 0; y < height; y++)
                {
                    int sourceRow = y * width;
                    int targetRow = y * stride;

                    for (int x = 0; x < width; x++)
                    {
                        float value = pixels[sourceRow + x];
                        byte alpha = value <= 0f ? (byte)0 : value >= 1f ? (byte)255 : (byte)(value * 255f + 0.5f);

                        buffer[targetRow + x * 4 + 3] = alpha;
                    }
                }

                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            Bitmap old = _pixelBitmap;
            _pixelBitmap = bmp;

            if (old != null)
                old.Dispose();
        }

        private void DrawPixels(Graphics g, int ox, int oy, int zoom, Rectangle clip)
        {
            if (_pixelBitmap == null)
                return;

            Rectangle totalRect = new Rectangle(ox, oy, _grid.Width * zoom, _grid.Height * zoom);
            Rectangle visible = Rectangle.Intersect(totalRect, clip);

            if (visible.IsEmpty)
                return;

            int srcX = Math.Max(0, (int)Math.Floor((visible.Left - ox) / (double)zoom));
            int srcY = Math.Max(0, (int)Math.Floor((visible.Top - oy) / (double)zoom));
            int srcRight = Math.Min(_grid.Width, (int)Math.Ceiling((visible.Right - ox) / (double)zoom));
            int srcBottom = Math.Min(_grid.Height, (int)Math.Ceiling((visible.Bottom - oy) / (double)zoom));

            int srcWidth = srcRight - srcX;
            int srcHeight = srcBottom - srcY;

            if (srcWidth <= 0 || srcHeight <= 0)
                return;

            int destX = ox + srcX * zoom;
            int destY = oy + srcY * zoom;

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.SmoothingMode = SmoothingMode.None;
            g.CompositingQuality = CompositingQuality.HighSpeed;

            using (var attributes = new ImageAttributes())
            {
                attributes.SetWrapMode(WrapMode.TileFlipXY);

                g.DrawImage(
                    _pixelBitmap,
                    new Rectangle(destX, destY, srcWidth * zoom, srcHeight * zoom),
                    srcX, srcY, srcWidth, srcHeight,
                    GraphicsUnit.Pixel,
                    attributes);
            }
        }

        private void DrawGridLines(Graphics g, int ox, int oy, int zoom, Rectangle clip, int totalWidth, int totalHeight)
        {
            Rectangle visible = Rectangle.Intersect(new Rectangle(ox, oy, totalWidth, totalHeight), clip);

            if (visible.IsEmpty)
                return;

            using (var pen = new Pen(Color.FromArgb(70, 120, 120, 120), 1f))
            {
                int startX = Math.Max(0, (visible.Left - ox) / zoom);
                int endX = Math.Min(_grid.Width, (visible.Right - ox + zoom - 1) / zoom);
                int verticalTop = Math.Max(oy, clip.Top);
                int verticalBottom = Math.Min(oy + totalHeight, clip.Bottom);

                for (int x = startX; x <= endX; x++)
                {
                    int sx = ox + x * zoom;
                    g.DrawLine(pen, sx, verticalTop, sx, verticalBottom);
                }

                int startY = Math.Max(0, (visible.Top - oy) / zoom);
                int endY = Math.Min(_grid.Height, (visible.Bottom - oy + zoom - 1) / zoom);
                int horizontalLeft = Math.Max(ox, clip.Left);
                int horizontalRight = Math.Min(ox + totalWidth, clip.Right);

                for (int y = startY; y <= endY; y++)
                {
                    int sy = oy + y * zoom;
                    g.DrawLine(pen, horizontalLeft, sy, horizontalRight, sy);
                }
            }
        }

        private void DrawAxes(Graphics g, int ox, int oy, int zoom)
        {
            int axisX = ox + (_grid.Width / 2) * zoom + zoom / 2;
            int axisY = oy + (_grid.Height / 2) * zoom + zoom / 2;

            using (var penX = new Pen(Color.FromArgb(200, 30, 30, 200), 2f))
            using (var penY = new Pen(Color.FromArgb(200, 200, 30, 30), 2f))
            {
                g.DrawLine(penX, ox, axisY, ox + _grid.Width * zoom, axisY);
                g.DrawLine(penY, axisX, oy, axisX, oy + _grid.Height * zoom);
            }
        }

        private static int ChooseLabelStep(int zoom)
        {
            int[] steps = { 1, 2, 5, 10, 20, 50 };

            foreach (int step in steps)
            {
                if (step * zoom >= 28)
                    return step;
            }

            return 50;
        }

        private void DrawLabels(Graphics g, int ox, int oy, int zoom, Rectangle clip)
        {
            int axisXScreen = ox + (_grid.Width / 2) * zoom + zoom / 2;
            int axisYScreen = oy + (_grid.Height / 2) * zoom + zoom / 2;

            Rectangle view = ClientRectangle;

            int xLabelTop = Math.Max(2, Math.Min(axisYScreen + 3, view.Bottom - 16));
            int yLabelLeft = Math.Max(2, Math.Min(axisXScreen + 3, view.Right - 44));

            int labelStep = ChooseLabelStep(zoom);

            using (var font = new Font(FontFamily.GenericSansSerif, 7.5f))
            using (var captionFont = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(70, 70, 70)))
            using (var xBrush = new SolidBrush(Color.FromArgb(30, 30, 200)))
            using (var yBrush = new SolidBrush(Color.FromArgb(200, 30, 30)))
            using (var centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            using (var leftFormat = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center })
            {
                Rectangle xRow = new Rectangle(view.Left, xLabelTop, view.Width, 14);

                if (xRow.IntersectsWith(clip))
                {
                    int minGridX = Math.Max(0, (clip.Left - ox) / zoom - 2);
                    int maxGridX = Math.Min(_grid.Width - 1, (clip.Right - ox) / zoom + 2);

                    for (int gx = minGridX; gx <= maxGridX; gx++)
                    {
                        int worldX = gx - _grid.Width / 2;

                        if (worldX % labelStep != 0)
                            continue;

                        float sx = ox + gx * zoom + zoom / 2f;
                        g.DrawString(worldX.ToString(), font, brush, sx, xLabelTop, centerFormat);
                    }
                }

                Rectangle yColumn = new Rectangle(yLabelLeft, view.Top, 44, view.Height);

                if (yColumn.IntersectsWith(clip))
                {
                    int minGridY = Math.Max(0, (clip.Top - oy) / zoom - 2);
                    int maxGridY = Math.Min(_grid.Height - 1, (clip.Bottom - oy) / zoom + 2);

                    for (int gy = minGridY; gy <= maxGridY; gy++)
                    {
                        int worldY = _grid.Height / 2 - gy;

                        if (worldY == 0 || worldY % labelStep != 0)
                            continue;

                        float sy = oy + gy * zoom + zoom / 2f;
                        g.DrawString(worldY.ToString(), font, brush, yLabelLeft, sy, leftFormat);
                    }
                }

                float captionY = Math.Max(2, Math.Min(axisYScreen - 18, view.Bottom - 34));
                float captionX = Math.Max(2, Math.Min(axisXScreen + 8, view.Right - 20));

                g.DrawString("X", captionFont, xBrush, view.Right - 18, captionY);
                g.DrawString("Y", captionFont, yBrush, captionX, view.Top + 2);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && _pixelBitmap != null)
            {
                _pixelBitmap.Dispose();
                _pixelBitmap = null;
            }

            base.Dispose(disposing);
        }
    }
}
