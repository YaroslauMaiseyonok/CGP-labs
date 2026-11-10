using System;
using System.Drawing;
using System.Windows.Forms;
using ImageProcessingLab3.Common;
using ImageProcessingLab3.Imaging;
using ImageProcessingLab3.Processing;

namespace ImageProcessingLab3.UI;

public sealed class MainForm : Form
{
    private ImageBuffer? _original;
    private ImageBuffer? _working;
    private IntegralImage? _integral;
    private bool _integralValid;

    private PictureBox _beforePicture = null!;
    private PictureBox _afterPicture = null!;

    private ComboBox _paddingBox = null!;

    private TrackBar _gaussianSigmaBar = null!;
    private Label _gaussianSigmaValue = null!;

    private TrackBar _niblackWindowBar = null!;
    private Label _niblackWindowValue = null!;
    private TrackBar _niblackKBar = null!;
    private Label _niblackKValue = null!;

    private TrackBar _sauvolaWindowBar = null!;
    private Label _sauvolaWindowValue = null!;
    private TrackBar _sauvolaKBar = null!;
    private Label _sauvolaKValue = null!;

    private TrackBar _bradleyWindowBar = null!;
    private Label _bradleyWindowValue = null!;
    private TrackBar _bradleyDeltaBar = null!;
    private Label _bradleyDeltaValue = null!;

    public MainForm()
    {
        Text = "Лабораторная работа 3 — Вариант 12";
        StartPosition = FormStartPosition.CenterScreen;

        MinimumSize = new Size(1180, 860);
        ClientSize = new Size(1280, 900);

        Font = new Font("Segoe UI", 9.25F);
        BackColor = SystemColors.Control;

        CreateUi();
    }

    private EdgePadding SelectedPadding =>
        _paddingBox.SelectedIndex == 0
            ? EdgePadding.Reflect
            : EdgePadding.Clamp;

    private void CreateUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(10, 10, 10, 8)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Percent, 56F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 44F));

        var imagesPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 4)
        };

        imagesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        imagesPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        var beforePanel = CreateImagePanel("Исходное изображение");
        var afterPanel = CreateImagePanel("Результат");

        _beforePicture = beforePanel.Picture;
        _afterPicture = afterPanel.Picture;

        imagesPanel.Controls.Add(beforePanel.Container, 0, 0);
        imagesPanel.Controls.Add(afterPanel.Container, 1, 0);

        var controlsGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoScroll = true
        };

        controlsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        controlsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));


        controlsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        controlsGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        controlsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var actionsGroup = CreateActionsGroup();
        controlsGrid.Controls.Add(actionsGroup, 0, 0);
        controlsGrid.SetColumnSpan(actionsGroup, 2);

        var gaussianGroup = CreateGaussianGroup();
        controlsGrid.Controls.Add(gaussianGroup, 0, 1);
        controlsGrid.SetColumnSpan(gaussianGroup, 2);

        var thresholdsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            MinimumSize = new Size(0, 210),
            Margin = new Padding(0, 4, 0, 0)
        };

        thresholdsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        thresholdsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

        thresholdsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        thresholdsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        thresholdsPanel.Controls.Add(CreateNiblackGroup(), 0, 0);
        thresholdsPanel.Controls.Add(CreateSauvolaGroup(), 1, 0);
        thresholdsPanel.Controls.Add(CreateBradleyGroup(), 0, 1);

        controlsGrid.Controls.Add(thresholdsPanel, 0, 2);
        controlsGrid.SetColumnSpan(thresholdsPanel, 2);

        root.Controls.Add(imagesPanel, 0, 0);
        root.Controls.Add(controlsGrid, 0, 1);

        Controls.Add(root);
    }

    private static (Control Container, PictureBox Picture) CreateImagePanel(string title)
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 8, 0)
        };

        container.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        container.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var label = new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            AutoSize = false
        };

        var picture = new PictureBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.FixedSingle,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = SystemColors.AppWorkspace
        };

        container.Controls.Add(label, 0, 0);
        container.Controls.Add(picture, 0, 1);

        return (container, picture);
    }

    private GroupBox CreateActionsGroup()
    {
        var group = CreateGroupBox("Основные действия");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var loadButton = CreateCompactButton("Загрузить изображение");
        loadButton.Click += LoadButton_Click;

        var resetButton = CreateCompactButton("Сбросить к исходному");
        resetButton.Click += ResetButton_Click;

        _paddingBox = new ComboBox
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.25F),
            Margin = new Padding(4, 4, 4, 4),
            Height = 26
        };

        _paddingBox.Items.AddRange(new object[]
        {
            EdgePadding.Reflect.ToString(),
            EdgePadding.Clamp.ToString()
        });

        _paddingBox.SelectedIndex = 0;

        var hint = new Label
        {
            Text = "Reflect — зеркальное отражение, Clamp — дублирование края.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            AutoEllipsis = true,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.75F),
            Margin = new Padding(10, 4, 4, 4)
        };

        layout.Controls.Add(loadButton, 0, 0);
        layout.Controls.Add(resetButton, 1, 0);
        layout.Controls.Add(_paddingBox, 3, 0);
        layout.Controls.Add(hint, 4, 0);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreateGaussianGroup()
    {
        var group = CreateGroupBox("Низкочастотный фильтр: Гауссово сглаживание");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateCaption("Сигма (σ):"), 0, 0);

        _gaussianSigmaBar = CreateSlider(1, 100, 10);
        _gaussianSigmaBar.ValueChanged += (_, _) =>
        {
            _gaussianSigmaValue.Text = (_gaussianSigmaBar.Value / 10F).ToString("F1");
        };

        layout.Controls.Add(_gaussianSigmaBar, 1, 0);

        _gaussianSigmaValue = CreateValueLabel("1.0");
        layout.Controls.Add(_gaussianSigmaValue, 2, 0);

        var gaussianButton = CreateApplyButton("Применить Гаусс");
        gaussianButton.Click += GaussianButton_Click;

        layout.Controls.Add(gaussianButton, 3, 0);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreateNiblackGroup()
    {
        var group = CreateGroupBox("Локальная пороговая обработка: Ниблэк");

        var layout = CreateThresholdLayout();

        layout.Controls.Add(CreateCaption("Размер окна:"), 0, 0);

        _niblackWindowBar = CreateSlider(3, 201, 31);
        _niblackWindowBar.ValueChanged += (_, _) =>
        {
            _niblackWindowValue.Text = Odd(_niblackWindowBar.Value).ToString();
        };

        layout.Controls.Add(_niblackWindowBar, 1, 0);

        _niblackWindowValue = CreateValueLabel("31");
        layout.Controls.Add(_niblackWindowValue, 2, 0);

        layout.Controls.Add(CreateCaption("Параметр k:"), 0, 1);

        _niblackKBar = CreateSlider(-100, 100, -20);
        _niblackKBar.ValueChanged += (_, _) =>
        {
            _niblackKValue.Text = (_niblackKBar.Value / 100F).ToString("F2");
        };

        layout.Controls.Add(_niblackKBar, 1, 1);

        _niblackKValue = CreateValueLabel("-0.20");
        layout.Controls.Add(_niblackKValue, 2, 1);

        var button = CreateApplyButton("Ниблэк");
        button.Click += NiblackButton_Click;

        layout.Controls.Add(button, 3, 0);
        layout.SetRowSpan(button, 2);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreateSauvolaGroup()
    {
        var group = CreateGroupBox("Локальная пороговая обработка: Савуола");

        var layout = CreateThresholdLayout();

        layout.Controls.Add(CreateCaption("Размер окна:"), 0, 0);

        _sauvolaWindowBar = CreateSlider(3, 201, 31);
        _sauvolaWindowBar.ValueChanged += (_, _) =>
        {
            _sauvolaWindowValue.Text = Odd(_sauvolaWindowBar.Value).ToString();
        };

        layout.Controls.Add(_sauvolaWindowBar, 1, 0);

        _sauvolaWindowValue = CreateValueLabel("31");
        layout.Controls.Add(_sauvolaWindowValue, 2, 0);

        layout.Controls.Add(CreateCaption("Параметр k:"), 0, 1);

        _sauvolaKBar = CreateSlider(0, 100, 20);
        _sauvolaKBar.ValueChanged += (_, _) =>
        {
            _sauvolaKValue.Text = (_sauvolaKBar.Value / 100F).ToString("F2");
        };

        layout.Controls.Add(_sauvolaKBar, 1, 1);

        _sauvolaKValue = CreateValueLabel("0.20");
        layout.Controls.Add(_sauvolaKValue, 2, 1);

        var button = CreateApplyButton("Савуола");
        button.Click += SauvolaButton_Click;

        layout.Controls.Add(button, 3, 0);
        layout.SetRowSpan(button, 2);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox CreateBradleyGroup()
    {
        var group = CreateGroupBox("Адаптивная пороговая обработка: Брэдли");

        var layout = CreateThresholdLayout();

        layout.Controls.Add(CreateCaption("Размер окна:"), 0, 0);

        _bradleyWindowBar = CreateSlider(3, 201, 31);
        _bradleyWindowBar.ValueChanged += (_, _) =>
        {
            _bradleyWindowValue.Text = Odd(_bradleyWindowBar.Value).ToString();
        };

        layout.Controls.Add(_bradleyWindowBar, 1, 0);

        _bradleyWindowValue = CreateValueLabel("31");
        layout.Controls.Add(_bradleyWindowValue, 2, 0);

        layout.Controls.Add(CreateCaption("Параметр delta:"), 0, 1);

        _bradleyDeltaBar = CreateSlider(0, 100, 15);
        _bradleyDeltaBar.ValueChanged += (_, _) =>
        {
            _bradleyDeltaValue.Text = (_bradleyDeltaBar.Value / 100F).ToString("F2");
        };

        layout.Controls.Add(_bradleyDeltaBar, 1, 1);

        _bradleyDeltaValue = CreateValueLabel("0.15");
        layout.Controls.Add(_bradleyDeltaValue, 2, 1);

        var button = CreateApplyButton("Брэдли");
        button.Click += BradleyButton_Click;

        layout.Controls.Add(button, 3, 0);
        layout.SetRowSpan(button, 2);

        group.Controls.Add(layout);
        return group;
    }


    private static GroupBox CreateGroupBox(string title)
    {
        return new GroupBox
        {
            Text = title,
            Dock = DockStyle.Fill,
            Padding = new Padding(10, 18, 10, 6),
            Margin = new Padding(0, 0, 8, 8),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            AutoSize = false
        };
    }

    private static TableLayoutPanel CreateThresholdLayout()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 2
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 65));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));

        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

        return layout;
    }

    private static Label CreateCaption(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoSize = false,
            Font = new Font("Segoe UI", 9.25F),
            Margin = new Padding(0, 2, 4, 2)
        };
    }

    private static Label CreateValueLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize = false,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Margin = new Padding(2, 2, 2, 2)
        };
    }

    private static TrackBar CreateSlider(int minimum, int maximum, int value)
    {
        return new TrackBar
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            TickStyle = TickStyle.None,
            AutoSize = false,
            Height = 32,
            SmallChange = 1,
            LargeChange = 5,
            Margin = new Padding(4, 3, 4, 3)
        };
    }

    private static Button CreateCompactButton(string text)
    {
        return new Button
        {
            Text = text,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Height = 30,
            Font = new Font("Segoe UI", 9.25F),
            Margin = new Padding(4, 3, 4, 3),
            AutoSize = false,
            MinimumSize = new Size(0, 30),
            MaximumSize = new Size(0, 30)
        };
    }

    private static Button CreateApplyButton(string text)
    {
        return new Button
        {
            Text = text,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Height = 42,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
            Margin = new Padding(8, 6, 8, 6),
            AutoSize = false,
            MinimumSize = new Size(0, 42),
            MaximumSize = new Size(0, 42)
        };
    }

    private static int Odd(int value)
    {
        return value | 1;
    }

    private void LoadButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Изображения|*.bmp;*.png;*.jpg;*.jpeg;*.tif;*.tiff",
            Title = "Выберите изображение"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            Cursor.Current = Cursors.WaitCursor;

            using var bitmap = new Bitmap(dialog.FileName);

            _original = BitmapConverter.FromBitmap(bitmap);
            _working = _original.Clone();

            _integral = IntegralImage.Compute(_working);
            _integralValid = true;

            SetImage(_beforePicture, _original);
            SetImage(_afterPicture, _working);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                $"Не удалось открыть изображение:{Environment.NewLine}{ex.Message}",
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void ResetButton_Click(object? sender, EventArgs e)
    {
        if (_original is null)
        {
            return;
        }

        Cursor.Current = Cursors.WaitCursor;

        try
        {
            _working = _original.Clone();
            _integral = IntegralImage.Compute(_working);
            _integralValid = true;

            SetImage(_afterPicture, _working);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void GaussianButton_Click(object? sender, EventArgs e)
    {
        if (_working is null)
        {
            return;
        }

        Cursor.Current = Cursors.WaitCursor;

        try
        {
            float sigma = _gaussianSigmaBar.Value / 10F;

            _working = GaussianFilter.Apply(_working, sigma, SelectedPadding);
            _integralValid = false;

            SetImage(_afterPicture, _working);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void NiblackButton_Click(object? sender, EventArgs e)
    {
        if (_working is null)
        {
            return;
        }

        Cursor.Current = Cursors.WaitCursor;

        try
        {
            EnsureIntegral();

            var result = ThresholdAlgorithms.Niblack(
                _working,
                _integral!,
                Odd(_niblackWindowBar.Value),
                _niblackKBar.Value / 100F);

            SetImage(_afterPicture, result);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void SauvolaButton_Click(object? sender, EventArgs e)
    {
        if (_working is null)
        {
            return;
        }

        Cursor.Current = Cursors.WaitCursor;

        try
        {
            EnsureIntegral();

            var result = ThresholdAlgorithms.Sauvola(
                _working,
                _integral!,
                Odd(_sauvolaWindowBar.Value),
                _sauvolaKBar.Value / 100F,
                128F);

            SetImage(_afterPicture, result);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void BradleyButton_Click(object? sender, EventArgs e)
    {
        if (_working is null)
        {
            return;
        }

        Cursor.Current = Cursors.WaitCursor;

        try
        {
            EnsureIntegral();

            var result = ThresholdAlgorithms.Bradley(
                _working,
                _integral!,
                Odd(_bradleyWindowBar.Value),
                _bradleyDeltaBar.Value / 100F);

            SetImage(_afterPicture, result);
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }
    }

    private void EnsureIntegral()
    {
        if (_working is null)
        {
            return;
        }

        if (_integralValid && _integral is not null)
        {
            return;
        }

        _integral = IntegralImage.Compute(_working);
        _integralValid = true;
    }

    private static void SetImage(PictureBox pictureBox, ImageBuffer? buffer)
    {
        Image? old = pictureBox.Image;

        pictureBox.Image = buffer is null
            ? null
            : BitmapConverter.ToBitmap(buffer);

        old?.Dispose();
    }
}