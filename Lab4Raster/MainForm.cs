using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lab4Raster
{
    public class MainForm : Form
    {
        private const int SceneWidth = 500;
        private const int SceneHeight = 500;

        private readonly VirtualGrid _grid = new VirtualGrid(SceneWidth, SceneHeight);

        private GridPainter _canvas;
        private RichTextBox _logBox;
        private Label _status;
        private ComboBox _algorithm;

        private NumericUpDown _x0, _y0, _x1, _y1, _radius, _radiusY, _zoom;
        private CheckBox _showGrid, _showAxes, _showLabels;
        private Button _benchmarkButton;

        private bool _benchmarkRunning;

        public MainForm()
        {
            Text = "Лабораторная работа 4: базовые растровые алгоритмы";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 800);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(CreateTopPanel(), 0, 0);

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical };

            _canvas = new GridPainter { Dock = DockStyle.Fill, Zoom = (int)_zoom.Value, Grid = _grid };
            _canvas.HoverChanged += (s, e) => UpdateStatus();
            split.Panel1.Controls.Add(_canvas);

            _logBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9f), BackColor = Color.WhiteSmoke };
            split.Panel2.Controls.Add(_logBox);

            root.Controls.Add(split, 0, 1);

            _status = new Label { Dock = DockStyle.Fill, Height = 26, TextAlign = ContentAlignment.MiddleLeft, Text = "Готово." };
            root.Controls.Add(_status, 0, 2);

            Controls.Add(root);

            Load += (s, e) =>
            {
                if (split.Width > 500)
                    split.SplitterDistance = Math.Max(split.Panel1MinSize, Math.Min(900, split.Width - 320));
            };

            Shown += (s, e) =>
            {
                RedrawScene();
                _canvas.CenterOnOrigin();
            };

            UpdateParameterAvailability();
            UpdateStatus();
        }

        #region UI
        private Control CreateTopPanel()
        {
            var top = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Padding = new Padding(4) };
            top.Controls.Add(CreateAlgorithmGroup());
            top.Controls.Add(CreateParametersGroup());
            top.Controls.Add(CreateViewGroup());
            top.Controls.Add(CreateActionsGroup());
            return top;
        }

        private GroupBox CreateAlgorithmGroup()
        {
            var groupBox = new GroupBox { Text = "Алгоритм", AutoSize = true };
            var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, RowCount = 2 };

            _algorithm = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230 };
            _algorithm.Items.AddRange(new object[] { "Пошаговый (линия)", "ЦДА (линия)", "Брезенхем (линия)", "Ву (сглаживание)", "Брезенхем (окружность)", "Кастл-Питвей (эллипс)" });
            _algorithm.SelectedIndex = 2;
            _algorithm.SelectedIndexChanged += (s, e) =>
            {
                UpdateParameterAvailability();
                UpdateStatus();
            };

            var drawButton = new Button { Text = "Построить и вычислить", Width = 150 };
            drawButton.Click += DrawButton_Click;

            var clearButton = new Button { Text = "Очистить", Width = 110 };
            clearButton.Click += ClearButton_Click;

            layout.Controls.Add(new Label { Text = "Метод:", AutoSize = true }, 0, 0);
            layout.Controls.Add(_algorithm, 1, 0);
            layout.Controls.Add(drawButton, 0, 1);
            layout.Controls.Add(clearButton, 1, 1);

            groupBox.Controls.Add(layout);
            return groupBox;
        }

        private GroupBox CreateParametersGroup()
        {
            var groupBox = new GroupBox { Text = "Параметры", AutoSize = true };
            var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 8, RowCount = 2 };

            layout.Controls.Add(new Label { Text = "X0", AutoSize = true }, 0, 0);
            _x0 = CreateNumeric(-SceneWidth / 2, SceneWidth / 2 - 1, -80);
            layout.Controls.Add(_x0, 1, 0);

            layout.Controls.Add(new Label { Text = "Y0", AutoSize = true }, 2, 0);
            _y0 = CreateNumeric(-SceneHeight / 2 + 1, SceneHeight / 2, -40);
            layout.Controls.Add(_y0, 3, 0);

            layout.Controls.Add(new Label { Text = "X1", AutoSize = true }, 4, 0);
            _x1 = CreateNumeric(-SceneWidth / 2, SceneWidth / 2 - 1, 90);
            layout.Controls.Add(_x1, 5, 0);

            layout.Controls.Add(new Label { Text = "Y1", AutoSize = true }, 6, 0);
            _y1 = CreateNumeric(-SceneHeight / 2 + 1, SceneHeight / 2, 60);
            layout.Controls.Add(_y1, 7, 0);

            layout.Controls.Add(new Label { Text = "R", AutoSize = true }, 0, 1);
            _radius = CreateNumeric(0, 200, 50);
            layout.Controls.Add(_radius, 1, 1);

            layout.Controls.Add(new Label { Text = "RY", AutoSize = true }, 2, 1);
            _radiusY = CreateNumeric(0, 200, 30);
            layout.Controls.Add(_radiusY, 3, 1);

            var hint = new Label
            {
                Text = "Линии: (X0;Y0) -> (X1;Y1). Окружность и эллипс: (X0;Y0) - центр, R (и RY) - радиусы.",
                AutoSize = true,
                MaximumSize = new Size(260, 0)
            };
            layout.Controls.Add(hint, 4, 1);
            layout.SetColumnSpan(hint, 4);

            groupBox.Controls.Add(layout);
            return groupBox;
        }

        private GroupBox CreateViewGroup()
        {
            var groupBox = new GroupBox { Text = "Отображение", AutoSize = true };
            var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 4, RowCount = 2 };

            _showGrid = new CheckBox { Text = "Сетка", Checked = true, AutoSize = true };
            _showAxes = new CheckBox { Text = "Оси", Checked = true, AutoSize = true };
            _showLabels = new CheckBox { Text = "Подписи", Checked = true, AutoSize = true };
            _zoom = new NumericUpDown { Minimum = 2, Maximum = 24, Value = 6, Width = 55 };

            _showGrid.CheckedChanged += View_CheckedChanged;
            _showAxes.CheckedChanged += View_CheckedChanged;
            _showLabels.CheckedChanged += View_CheckedChanged;
            _zoom.ValueChanged += Zoom_ValueChanged;

            layout.Controls.Add(_showGrid, 0, 0);
            layout.Controls.Add(_showAxes, 1, 0);
            layout.Controls.Add(_showLabels, 2, 0);

            layout.Controls.Add(new Label { Text = "Масштаб, px:", AutoSize = true }, 0, 1);
            layout.Controls.Add(_zoom, 1, 1);

            var coordinateHint = new Label
            {
                Text = "Целые координаты - ЦЕНТРЫ клеток, линии сетки - их границы.",
                AutoSize = true,
                MaximumSize = new Size(260, 0)
            };
            layout.Controls.Add(coordinateHint, 2, 1);
            layout.SetColumnSpan(coordinateHint, 2);

            groupBox.Controls.Add(layout);
            return groupBox;
        }

        private GroupBox CreateActionsGroup()
        {
            var groupBox = new GroupBox { Text = "Бенчмарк", AutoSize = true };
            var layout = new TableLayoutPanel { AutoSize = true, ColumnCount = 1, RowCount = 2 };

            _benchmarkButton = new Button { Text = "Запустить бенчмарк", AutoSize = true };
            _benchmarkButton.Click += BenchmarkButton_Click;

            var info = new Label { Text = "100 000 линий + 50 000 окружностей.", AutoSize = true, MaximumSize = new Size(260, 0) };

            layout.Controls.Add(_benchmarkButton, 0, 0);
            layout.Controls.Add(info, 0, 1);

            groupBox.Controls.Add(layout);
            return groupBox;
        }

        private NumericUpDown CreateNumeric(int min, int max, int value)
        {
            return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = 60 };
        }
        #endregion

        #region События
        private void DrawButton_Click(object sender, EventArgs e)
        {
            RedrawScene();
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            _grid.Clear();
            _logBox.Clear();
            _canvas.RefreshPixels();
            UpdateStatus();
        }

        private void Zoom_ValueChanged(object sender, EventArgs e)
        {
            if (_canvas == null) return;

            _canvas.Zoom = (int)_zoom.Value;
            UpdateStatus();
        }

        private void View_CheckedChanged(object sender, EventArgs e)
        {
            if (_canvas == null) return;

            _canvas.ShowGrid = _showGrid.Checked;
            _canvas.ShowAxes = _showAxes.Checked;
            _canvas.ShowLabels = _showLabels.Checked;
        }

        private void BenchmarkButton_Click(object sender, EventArgs e)
        {
            _benchmarkRunning = true;
            _benchmarkButton.Enabled = false;
            _status.Text = "Выполняется бенчмарк...";

            Task.Run(() => BenchmarkRunner.RunAll(SceneWidth, SceneHeight))
                .ContinueWith(task =>
                {
                    _benchmarkRunning = false;

                    // Пользователь мог закрыть окно, пока шёл бенчмарк.
                    if (IsDisposed) return;

                    _benchmarkButton.Enabled = true;

                    if (task.IsFaulted)
                    {
                        _status.Text = "Ошибка бенчмарка.";
                        MessageBox.Show(task.Exception?.GetBaseException().Message ?? "Ошибка", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        UpdateStatus();
                        _status.Text = "Бенчмарк завершен. " + _status.Text;
                        using (var form = new BenchmarkForm(task.Result)) form.ShowDialog(this);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        #endregion

        #region Логика рисования
        private void RedrawScene()
        {
            _grid.Clear();
            _logBox.Clear();
            DrawSelectedAlgorithm();
            _canvas.RefreshPixels();
            UpdateStatus();
        }

        private void DrawSelectedAlgorithm()
        {
            // Мировые координаты (ось Y вверх) -> клетки растра (ось Y вниз).
            int x0 = _canvas.WorldToGridX((int)_x0.Value);
            int y0 = _canvas.WorldToGridY((int)_y0.Value);
            int x1 = _canvas.WorldToGridX((int)_x1.Value);
            int y1 = _canvas.WorldToGridY((int)_y1.Value);
            int r = (int)_radius.Value;
            int ry = (int)_radiusY.Value;

            var sb = new StringBuilder();
            Action<string> logger = (msg) => sb.AppendLine(msg);

            switch (_algorithm.SelectedIndex)
            {
                case 0: RasterEngine.StepLine(_grid, x0, y0, x1, y1, logger); break;
                case 1: RasterEngine.DdaLine(_grid, x0, y0, x1, y1, logger); break;
                case 2: RasterEngine.BresenhamLine(_grid, x0, y0, x1, y1, logger); break;
                case 3: RasterEngine.WuLine(_grid, x0, y0, x1, y1, logger); break;
                case 4: RasterEngine.BresenhamCircle(_grid, x0, y0, r, logger); break;
                case 5: RasterEngine.CastlePitewayEllipse(_grid, x0, y0, r, ry, logger); break;
            }

            string header = string.Format(
                "Вход (мировые): ({0}; {1}) -> ({2}; {3}), R={4}, RY={5}\r\nВход (клетки растра): [{6}; {7}] -> [{8}; {9}]\r\n\r\n",
                (int)_x0.Value, (int)_y0.Value, (int)_x1.Value, (int)_y1.Value, r, ry,
                x0, y0, x1, y1);

            if (sb.Length > 0)
            {
                _logBox.Text = header + "Пошаговые вычисления (первые 20 шагов; координаты - клетки растра):\r\n" + sb.ToString();
            }
            else
            {
                _logBox.Text = header + "Алгоритм выполнен без пошагового логирования.";
            }
        }

        private void UpdateParameterAvailability()
        {
            if (_algorithm == null || _x1 == null) return;

            int index = _algorithm.SelectedIndex;
            bool isLine = index >= 0 && index <= 3;

            _x1.Enabled = isLine;
            _y1.Enabled = isLine;
            _radius.Enabled = index == 4 || index == 5;
            _radiusY.Enabled = index == 5;
        }
        #endregion

        #region Служебные методы
        private void UpdateStatus()
        {
            if (_status == null || _algorithm == null || _canvas == null) return;
            if (_benchmarkRunning) return;

            string text = $"Метод: {_algorithm.SelectedItem}; закрашено: {_grid.ChangedPixels}; вызовов SetPixel: {_grid.SetPixelCalls}; масштаб: {_canvas.Zoom} px.";

            if (!string.IsNullOrEmpty(_canvas.HoverText))
                text += "   |   " + _canvas.HoverText;

            _status.Text = text;
        }
        #endregion
    }
}
