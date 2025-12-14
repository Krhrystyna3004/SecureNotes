using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class CreateNoteForm : Form
    {
        public Note CreatedNote { get; private set; }

        private TextBox txtTitle;
        private TextBox txtContent;
        private RadioButton rbNote;
        private RadioButton rbPassword;
        private RadioButton rbShared;
        private Panel pnlShared;
        private TextBox txtShared;
        private FlowLayoutPanel pnlColors;
        private Panel pnlSelectedColor;
        private Label lblSelectedColor;
        private Button btnCreate;
        private Button btnCancel;

        private string selectedType = "note";
        private string selectedColor = "#FFD6E8";
        private readonly string[] PALETTE = { "#FFD6E8", "#E6F3FF", "#E8F5E9", "#FFF4E6", "#F3E5F5", "#FFF9C4", "#FFFFFF" };

        public CreateNoteForm(string defaultType = "note")
        {
            Text = "Створити нотатку";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            ClientSize = new Size(560, 560);

            BuildUI();
            SetType(defaultType);
            UpdateColorPreview();
        }

        public void LoadForEdit(Note note)
        {
            Text = "Редагувати нотатку";
            txtTitle.Text = note.Title;
            txtContent.Text = note.Content;
            SetType(note.Type);
            selectedColor = string.IsNullOrWhiteSpace(note.Color) ? "#FFFFFF" : note.Color;
            UpdateColorPreview();

            if (note.Type == "shared" && !string.IsNullOrWhiteSpace(note.SharedWith))
                txtShared.Text = note.SharedWith;

            btnCreate.Text = "Зберегти";
        }

        private void BuildUI()
        {
            var lblHeader = new Label
            {
                Text = "Створити нотатку",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                Location = new Point(16, 12),
                Size = new Size(300, 28)
            };
            Controls.Add(lblHeader);

            var lblTitle = new Label { Text = "Заголовок", Location = new Point(16, 52) };
            txtTitle = new TextBox { Location = new Point(16, 70), Size = new Size(520, 23) };
            Controls.Add(lblTitle);
            Controls.Add(txtTitle);

            var lblContent = new Label { Text = "Зміст", Location = new Point(16, 102) };
            txtContent = new TextBox
            {
                Location = new Point(16, 120),
                Multiline = true,
                Size = new Size(520, 150),
                ScrollBars = ScrollBars.Vertical
            };
            Controls.Add(lblContent);
            Controls.Add(txtContent);

            var grpType = new GroupBox { Text = "Тип нотатки", Location = new Point(16, 280), Size = new Size(520, 56) };
            rbNote = new RadioButton { Text = "Звичайна", Location = new Point(12, 22), Checked = true };
            rbPassword = new RadioButton { Text = "Пароль", Location = new Point(120, 22) };
            rbShared = new RadioButton { Text = "Спільна", Location = new Point(210, 22) };
            rbNote.CheckedChanged += (s, e) => { if (rbNote.Checked) SetType("note"); };
            rbPassword.CheckedChanged += (s, e) => { if (rbPassword.Checked) SetType("password"); };
            rbShared.CheckedChanged += (s, e) => { if (rbShared.Checked) SetType("shared"); };
            grpType.Controls.Add(rbNote);
            grpType.Controls.Add(rbPassword);
            grpType.Controls.Add(rbShared);
            Controls.Add(grpType);

            var lblColors = new Label { Text = "Колір", Location = new Point(16, 344) };
            pnlColors = new FlowLayoutPanel { Location = new Point(16, 364), Size = new Size(420, 48) };
            Controls.Add(lblColors);
            Controls.Add(pnlColors);

            foreach (var hex in PALETTE)
            {
                var btn = new Button
                {
                    Width = 40,
                    Height = 40,
                    Margin = new Padding(6),
                    BackColor = ColorTranslator.FromHtml(hex),
                    FlatStyle = FlatStyle.Flat
                };
                btn.FlatAppearance.BorderColor = Color.Silver;
                btn.Click += ColorButton_Click;
                pnlColors.Controls.Add(btn);
            }

            pnlSelectedColor = new Panel { Location = new Point(448, 364), Size = new Size(88, 24), BackColor = ColorTranslator.FromHtml(selectedColor) };
            lblSelectedColor = new Label { Text = $"Колір: {selectedColor}", Location = new Point(448, 392), Size = new Size(120, 20) };
            Controls.Add(pnlSelectedColor);
            Controls.Add(lblSelectedColor);

            pnlShared = new Panel { Location = new Point(16, 420), Size = new Size(520, 50), Visible = false };
            var lblShared = new Label { Text = "Спільний доступ (через кому)", Location = new Point(0, 0) };
            txtShared = new TextBox { Location = new Point(0, 20), Size = new Size(520, 23) };
            pnlShared.Controls.Add(lblShared);
            pnlShared.Controls.Add(txtShared);
            Controls.Add(pnlShared);

            btnCreate = new Button { Text = "Створити", Location = new Point(16, 488), Size = new Size(120, 32) };
            btnCancel = new Button { Text = "Скасувати", Location = new Point(144, 488), Size = new Size(120, 32) };
            btnCreate.Click += btnCreate_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnCreate);
            Controls.Add(btnCancel);
        }

        private void SetType(string type)
        {
            selectedType = type;
            rbNote.Checked = type == "note";
            rbPassword.Checked = type == "password";
            rbShared.Checked = type == "shared";
            pnlShared.Visible = selectedType == "shared";
        }

        private void UpdateColorPreview()
        {
            try
            {
                pnlSelectedColor.BackColor = ColorTranslator.FromHtml(selectedColor);
                lblSelectedColor.Text = $"Колір: {selectedColor}";
            }
            catch
            {
                pnlSelectedColor.BackColor = Color.White;
                lblSelectedColor.Text = "Колір: #FFFFFF";
            }
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                selectedColor = ColorTranslator.ToHtml(btn.BackColor);
                UpdateColorPreview();
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            var title = txtTitle.Text.Trim();
            var content = txtContent.Text;

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Введіть заголовок.", "Помилка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            CreatedNote = new Note
            {
                Title = title,
                Content = content,
                Type = selectedType,
                Color = selectedColor,
                SharedWith = selectedType == "shared" ? txtShared.Text.Trim() : null,
                CreatedAt = DateTime.Now
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}