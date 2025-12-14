using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class NoteCard : Panel
    {
        private Label lblTitle = new Label();
        private Label lblContent = new Label();
        private Label lblDate = new Label();
        private Label lblType = new Label();
        private Panel pnlColor = new Panel();
        private Button btnDelete = new Button();

        public int NoteId { get; private set; }
        public event EventHandler<int> EditRequested;
        public event EventHandler<int> DeleteRequested;

        public NoteCard()
        {
            this.Size = new Size(280, 160);
            this.BackColor = Color.White;
            this.BorderStyle = BorderStyle.FixedSingle;
            this.Margin = new Padding(8);
            this.Cursor = Cursors.Hand;
            this.Click += NoteCard_Click;

            pnlColor.Dock = DockStyle.Top;
            pnlColor.Height = 6;
            pnlColor.BackColor = Color.White;
            pnlColor.Click += NoteCard_Click;

            lblTitle.Location = new Point(10, 14);
            lblTitle.Size = new Size(220, 20);
            lblTitle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblTitle.Click += NoteCard_Click;

            lblType.Location = new Point(240, 16);
            lblType.Font = new Font("Segoe UI", 8F);
            lblType.Click += NoteCard_Click;

            lblContent.Location = new Point(10, 40);
            lblContent.Size = new Size(260, 60);
            lblContent.Font = new Font("Segoe UI", 9F);
            lblContent.Click += NoteCard_Click;

            lblDate.Location = new Point(10, 110);
            lblDate.Font = new Font("Segoe UI", 8F);
            lblDate.ForeColor = Color.Gray;
            lblDate.Click += NoteCard_Click;

            btnDelete.Text = "Видалити";
            btnDelete.Location = new Point(200, 106);
            btnDelete.Size = new Size(70, 24);
            btnDelete.Click += btnDelete_Click;

            this.Controls.Add(pnlColor);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblType);
            this.Controls.Add(lblContent);
            this.Controls.Add(lblDate);
            this.Controls.Add(btnDelete);
        }

        public void Bind(Note note)
        {
            NoteId = note.Id;
            lblTitle.Text = note.Title;
            lblContent.Text = string.IsNullOrWhiteSpace(note.Content) ? "(без змісту)" : note.Content;
            lblDate.Text = note.CreatedAt.ToString("dd.MM.yyyy");

            lblType.Text = note.Type == "note" ? "Звичайна"
                         : note.Type == "password" ? "Пароль"
                         : note.Type == "shared" ? "Спільна" : note.Type;

            try { pnlColor.BackColor = ColorTranslator.FromHtml(note.Color); }
            catch { pnlColor.BackColor = Color.White; }
        }

        private void NoteCard_Click(object sender, EventArgs e)
        {
            EditRequested?.Invoke(this, NoteId);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteRequested?.Invoke(this, NoteId);
        }
    }
}