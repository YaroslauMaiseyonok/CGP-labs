using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageInspector.Core;
using ImageInspector.Models;

namespace ImageInspector
{
    public class MainForm : Form
    {
        private Button btnSelectFolder;
        private Button btnCancel;
        private ProgressBar progressBar;
        private Label lblStatus;
        private DataGridView dataGrid;

        private readonly BindingList<ImageInfo> _results = new BindingList<ImageInfo>();
        private CancellationTokenSource _cts;

        public MainForm()
        {
            Text = "Maiseyonok Lab#2";
            Width = 1400;
            Height = 750;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUi();
        }

        private void BuildUi()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };

            btnSelectFolder = new Button
            {
                Text = "Выбрать папку...",
                Width = 160,
                Height = 30,
                Location = new Point(10, 15)
            };
            btnSelectFolder.Click += BtnSelectFolder_Click;

            btnCancel = new Button
            {
                Text = "Отмена",
                Width = 100,
                Height = 30,
                Location = new Point(180, 15),
                Enabled = false
            };
            btnCancel.Click += (s, e) => _cts?.Cancel();

            progressBar = new ProgressBar
            {
                Location = new Point(290, 18),
                Width = 500,
                Height = 24
            };

            lblStatus = new Label
            {
                Location = new Point(810, 22),
                AutoSize = true,
                Text = "Готов"
            };

            topPanel.Controls.Add(btnSelectFolder);
            topPanel.Controls.Add(btnCancel);
            topPanel.Controls.Add(progressBar);
            topPanel.Controls.Add(lblStatus);

            dataGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AllowUserToResizeRows = false
            };

            dataGrid.CellDoubleClick += DataGrid_CellDoubleClick;

            AddColumns();
            dataGrid.DataSource = _results;

            Controls.Add(dataGrid);
            Controls.Add(topPanel);
        }

        private void AddColumns()
        {
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Имя файла", DataPropertyName = "FileName", Width = 240 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Формат", DataPropertyName = "Format", Width = 70 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Ширина", DataPropertyName = "Width", Width = 80 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Высота", DataPropertyName = "Height", Width = 80 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "DPI X", DataPropertyName = "DpiX", Width = 70 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "DPI Y", DataPropertyName = "DpiY", Width = 70 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Глубина", DataPropertyName = "BitDepth", Width = 80 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Сжатие", DataPropertyName = "Compression", Width = 180 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Доп. инфо", DataPropertyName = "Extra", Width = 300 });
            dataGrid.Columns.Add(new DataGridViewTextBoxColumn
            { HeaderText = "Статус", DataPropertyName = "Status", Width = 200 });
        }

        private async void BtnSelectFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() != DialogResult.OK) return;

            string folder = dialog.SelectedPath;

            _results.Clear();
            progressBar.Value = 0;
            btnSelectFolder.Enabled = false;
            btnCancel.Enabled = true;
            lblStatus.Text = "Сканирование...";
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            string[] files;
            try
            {
                files = await Task.Run(() =>
                {
                    var list = new System.Collections.Generic.List<string>();
                    foreach (var f in Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories))
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" ||
                            ext == ".bmp" || ext == ".gif" || ext == ".tif" ||
                            ext == ".tiff" || ext == ".pcx")
                            list.Add(f);
                    }
                    return list.ToArray();
                }, token);
            }
            catch (OperationCanceledException)
            {
                FinishScanning("Отменено пользователем");
                return;
            }

            int total = files.Length;
            if (total == 0)
            {
                FinishScanning("В папке нет графических файлов поддерживаемых форматов");
                return;
            }

            int processed = 0;

            var progress = new Progress<int>(p =>
            {
                progressBar.Value = Math.Min(100, p);
            });

            var collected = new ConcurrentBag<ImageInfo>();

            try
            {
                await Task.Run(() =>
                {
                    var options = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = token
                    };

                    Parallel.ForEach(files, options, file =>
                    {
                        var info = ImageMetadataReader.ReadMetadata(file);
                        collected.Add(info);

                        int done = Interlocked.Increment(ref processed);
                        if (done % 50 == 0 || done == total)
                            ((IProgress<int>)progress).Report(done * 100 / total);
                    });
                }, token);

                _results.RaiseListChangedEvents = false;
                foreach (var r in collected)
                    _results.Add(r);
                _results.RaiseListChangedEvents = true;
                _results.ResetBindings();

                progressBar.Value = 100;
                FinishScanning($"Готово. Обработано файлов: {_results.Count}");
            }
            catch (OperationCanceledException)
            {
                FinishScanning("Отменено пользователем");
            }
            catch (Exception ex)
            {
                FinishScanning("Ошибка: " + ex.Message);
            }
        }

        private void DataGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _results.Count) return;

            var info = _results[e.RowIndex];

            if (string.IsNullOrEmpty(info.FullPath) || !File.Exists(info.FullPath))
            {
                MessageBox.Show("Файл недоступен:\n" + info.FullPath,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var viewer = new ImageViewerForm(info.FullPath);
                viewer.Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть изображение: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FinishScanning(string status)
        {
            btnSelectFolder.Enabled = true;
            btnCancel.Enabled = false;
            lblStatus.Text = status;
        }
    }
}