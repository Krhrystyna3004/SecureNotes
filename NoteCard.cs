using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class NoteCard : Panel
    {
        private Label lblTitle = new Label();
        private Label lblContent = new Label();
        private Label lblMeta = new Label();

        private Button btnDelete = new Button();
        private Button btnCopy = new Button();
        private Button btnEdit = new Button();

        public int NoteId { get; private set; }

        public event EventHandler<int> DeleteRequested;
        public event EventHandler<int> EditRequested;

        public NoteCard()
        {
            Size = new Size(300, 190);
            Margin = new Padding(10);
            BorderStyle = BorderStyle.FixedSingle;

            lblTitle.Location = new Point(10, 10);
            lblTitle.Size = new Size(280, 20);
            lblTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            lblContent.Location = new Point(10, 36);
            lblContent.Size = new Size(280, 84);
            lblContent.Font = new Font("Segoe UI", 9F);

            lblMeta.Location = new Point(10, 124);
            lblMeta.Size = new Size(180, 20);
            lblMeta.Font = new Font("Segoe UI", 8F);

            btnEdit.Text = "Редагувати";
            btnEdit.Location = new Point(10, 150);
            btnEdit.Size = new Size(80, 26);
            btnEdit.Click += (s, e) => EditRequested?.Invoke(this, NoteId);

            btnCopy.Text = "Копіювати";
            btnCopy.Location = new Point(100, 150);
            btnCopy.Size = new Size(80, 26);
            btnCopy.Click += BtnCopy_Click;

            btnDelete.Text = "Видалити";
            btnDelete.Location = new Point(190, 150);
            btnDelete.Size = new Size(80, 26);
            btnDelete.Click += (s, e) => DeleteRequested?.Invoke(this, NoteId);

            Controls.AddRange(new Control[] { lblTitle, lblContent, lblMeta, btnEdit, btnCopy, btnDelete });
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();
            var text = lblContent.Tag as string ?? lblContent.Text;
            Clipboard.SetText(text ?? "");
            var timer = new Timer { Interval = 15000 };
            timer.Tick += (s, ev) =>
            {
                try { if (Clipboard.GetText() == text) Clipboard.Clear(); } catch { }
                timer.Stop(); timer.Dispose();
            };
            timer.Start();
        }

        public void Bind(Note note, string decryptedIfPassword = null)
        {
            NoteId = note.Id;

            // Фон картки = колір нотатки
            Color bg;
            try { bg = ColorTranslator.FromHtml(note.Color ?? "#FFFFFF"); }
            catch { bg = Color.White; }
            BackColor = bg;

            // Авто-контраст тексту
            var fg = GetContrastingColor(bg);
            ForeColor = fg;
            lblTitle.ForeColor = fg;
            lblContent.ForeColor = fg;
            lblMeta.ForeColor = fg == Color.Black ? Color.FromArgb(120, 0, 0, 0) : Color.FromArgb(200, 255, 255, 255);

            lblTitle.Text = note.Title;
            lblMeta.Text = note.Type == "password"
                ? (note.GroupId.HasValue ? "Пароль • Спільна" : "Пароль")
                : (note.GroupId.HasValue ? "Нотатка • Спільна" : "Нотатка");

            if (note.Type == "password")
            {
                if (decryptedIfPassword == null)
                {
                    lblContent.Text = "••••••••••";
                    lblContent.Tag = null;
                }
                else
                {
                    lblContent.Text = decryptedIfPassword;
                    lblContent.Tag = decryptedIfPassword;
                }
            }
            else
            {
                lblContent.Text = string.IsNullOrWhiteSpace(note.Content) ? "(порожньо)" : note.Content;
                lblContent.Tag = lblContent.Text;
            }
        }

        public void ApplyTheme(Theme theme)
        {
            // Фон залишається кольором нотатки; кнопки підлаштуємо
            var buttonBg = theme == Theme.Dark ? Color.FromArgb(40, 44, 45) : SystemColors.Control;
            btnEdit.BackColor = buttonBg;
            btnCopy.BackColor = buttonBg;
            btnDelete.BackColor = buttonBg;
        }

        private Color GetContrastingColor(Color bg)
        {
            var yiq = ((bg.R * 299) + (bg.G * 587) + (bg.B * 114)) / 1000;
            return yiq >= 128 ? Color.Black : Color.WhiteSmoke;
        }
    }
}