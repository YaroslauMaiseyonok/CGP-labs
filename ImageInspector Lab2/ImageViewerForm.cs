using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ImageInspector
{
    public class ImageViewerForm : Form
    {
        private readonly string _path;
        private Panel _panel;
        private PictureBox _pictureBox;
        private CheckBox _chkActualSize;
        private Label _lblZoom;
        private Image _image;

        private float _zoom = 1.0f;

        public ImageViewerForm(string path)
        {
            _path = path;
            Text = "Просмотр: " + Path.GetFileName(path);
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterParent;
            KeyPreview = true;
            BackColor = Color.DimGray;

            BuildUi();
            LoadImage();
        }

        private void BuildUi()
        {
            var topPanel = new Panel { Dock = DockStyle.Top, Height = 38 };

            _chkActualSize = new CheckBox
            {
                Text = "Реальный размер (1:1)",
                Location = new Point(10, 10),
                AutoSize = true
            };
            _chkActualSize.CheckedChanged += (s, e) =>
            {
                _zoom = 1.0f;
                ApplySizeMode();
            };

            _lblZoom = new Label
            {
                Location = new Point(200, 12),
                AutoSize = true,
                Text = "Масштаб: 100%"
            };

            var lblHint = new Label
            {
                Location = new Point(320, 12),
                AutoSize = true,
                ForeColor = Color.White,
                Text = "Колёсико — масштаб (в режиме 1:1); Esc — закрыть"
            };

            topPanel.Controls.Add(_chkActualSize);
            topPanel.Controls.Add(_lblZoom);
            topPanel.Controls.Add(lblHint);

            _panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.DimGray
            };

            _pictureBox = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(0, 0),
                BackColor = Color.Black
            };
            _panel.Controls.Add(_pictureBox);

            Controls.Add(_panel);
            Controls.Add(topPanel);

            MouseWheel += OnMouseWheelZoom;
            _panel.MouseWheel += OnMouseWheelZoom;
            _pictureBox.MouseWheel += OnMouseWheelZoom;

            _panel.Resize += (s, e) => { if (!_chkActualSize.Checked) ApplySizeMode(); };

            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
                else if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus) ZoomBy(1.1f);
                else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus) ZoomBy(0.9f);
                else if (e.KeyCode == Keys.D0) { _zoom = 1.0f; ApplySizeMode(); }
            };
        }

        private void OnMouseWheelZoom(object sender, MouseEventArgs e)
        {
            if (!_chkActualSize.Checked) return;
            ZoomBy(e.Delta > 0 ? 1.1f : 0.9f);
        }

        private void ZoomBy(float factor)
        {
            _zoom = Math.Max(0.05f, Math.Min(20f, _zoom * factor));
            ApplySizeMode();
        }

        private void LoadImage()
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(_path);
                using var ms = new MemoryStream(bytes);
                _image = new Bitmap(ms);

                _pictureBox.Image = _image;
                Text = $"Просмотр: {Path.GetFileName(_path)}  —  {_image.Width}×{_image.Height}";
                ApplySizeMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить изображение: " + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                BeginInvoke(new Action(Close));
            }
        }

        private void ApplySizeMode()
        {
            if (_image == null) return;

            if (_chkActualSize.Checked)
            {
                _pictureBox.Dock = DockStyle.None;
                _pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
                _pictureBox.Size = new Size(
                    Math.Max(1, (int)(_image.Width * _zoom)),
                    Math.Max(1, (int)(_image.Height * _zoom)));
                _lblZoom.Text = $"Масштаб: {(_zoom * 100):0}%";
            }
            else
            {
                _pictureBox.Dock = DockStyle.Fill;
                _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                _lblZoom.Text = "Масштаб: по окну";
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_pictureBox != null) _pictureBox.Image = null;
                _image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}