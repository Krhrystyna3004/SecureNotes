using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SecureNotes
{
    public class MainForm : Form
    {
        private readonly DatabaseHelper _db;
        private List<Note> _allNotes = new List<Note>();
        private string _activeTab = "notes"; // notes | passwords | shared

        private Panel sidebar;
        private Button btnNotes;
        private Button btnPasswords;
        private Button btnShared;
        private Label lblTipHeader;
        private Label lblTipText;

        private Panel header;
        private Label lblTitle;
        private Button btnCreate;

        private Label lblSection;
        private FlowLayoutPanel flowCards;

        public MainForm()
        {
            _db = new DatabaseHelper();
            BuildUI();
            LoadNotes();
            RenderNotes();
        }

        private void BuildUI()
        {
            Text = "SecureNotes";
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(248, 250, 252);
            ClientSize = new Size(1000, 640);

            // Верхня панель
            header = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(12, 12),
                Size = new Size(976, 50),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lblTitle = new Label
            {
                Text = "SecureNotes",
                Location = new Point(12, 14),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            btnCreate = new Button
            {
                Text = "Створити нотатку",
                Location = new Point(820, 10),
                Size = new Size(140, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCreate.Click += btnCreate_Click;
            header.Controls.Add(lblTitle);
            header.Controls.Add(btnCreate);
            Controls.Add(header);

            // Ліва панель
            sidebar = new Panel
            {
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(12, 68),
                Size = new Size(220, 560),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnNotes = new Button { Text = "Мої нотатки", Location = new Point(12, 12), Size = new Size(196, 36) };
            btnPasswords = new Button { Text = "Паролі", Location = new Point(12, 54), Size = new Size(196, 36) };
            btnShared = new Button { Text = "Спільні нотатки", Location = new Point(12, 96), Size = new Size(196, 36) };
            btnNotes.Click += (s, e) => { _activeTab = "notes"; RenderNotes(); };
            btnPasswords.Click += (s, e) => { _activeTab = "passwords"; RenderNotes(); };
            btnShared.Click += (s, e) => { _activeTab = "shared"; RenderNotes(); };
            lblTipHeader = new Label { Text = "💡 Підказка", Location = new Point(12, 160), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblTipText = new Label { Text = "Використовуйте кольори для організації нотаток", Location = new Point(12, 180), Size = new Size(196, 40) };
            sidebar.Controls.AddRange(new Control[] { btnNotes, btnPasswords, btnShared, lblTipHeader, lblTipText });
            Controls.Add(sidebar);

            // Заголовок секції
            lblSection = new Label { Text = "Мої нотатки", Location = new Point(246, 74), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            Controls.Add(lblSection);

            // Панель карток
            flowCards = new FlowLayoutPanel
            {
                Location = new Point(246, 102),
                Size = new Size(742, 526),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true
            };
            Controls.Add(flowCards);
        }

        private void LoadNotes()
        {
            _allNotes = _db.GetNotes();
        }

        private void RenderNotes()
        {
            flowCards.SuspendLayout();
            flowCards.Controls.Clear();

            IEnumerable<Note> filtered = _activeTab == "notes"
                ? _allNotes.Where(n => n.Type == "note")
                : _activeTab == "passwords"
                    ? _allNotes.Where(n => n.Type == "password")
                    : _allNotes.Where(n => n.Type == "shared");

            foreach (var note in filtered)
            {
                var card = new NoteCard();
                card.Bind(note);
                card.EditRequested += (s, id) => OpenEdit(id);
                card.DeleteRequested += (s, id) => DeleteNote(id);
                flowCards.Controls.Add(card);
            }

            flowCards.ResumeLayout();
            lblSection.Text = _activeTab == "notes" ? "Мої нотатки"
                             : _activeTab == "passwords" ? "Паролі" : "Спільні нотатки";
        }

        private void OpenEdit(int id)
        {
            var note = _db.GetNotes().FirstOrDefault(n => n.Id == id);
            if (note == null) return;

            var modal = new CreateNoteForm(note.Type);
            modal.LoadForEdit(note);

            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                var updated = modal.CreatedNote;
                updated.Id = note.Id;
                updated.CreatedAt = note.CreatedAt;
                _db.DeleteNote(note.Id);
                _db.AddNote(updated);
                LoadNotes();
                RenderNotes();
            }
        }

        private void DeleteNote(int id)
        {
            var confirm = MessageBox.Show("Видалити нотатку?", "Підтвердження",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                _db.DeleteNote(id);
                LoadNotes();
                RenderNotes();
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            string defaultType = _activeTab == "notes" ? "note" :
                                 _activeTab == "passwords" ? "password" : "shared";
            var modal = new CreateNoteForm(defaultType);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                var note = modal.CreatedNote;
                _db.AddNote(note);
                LoadNotes();
                RenderNotes();
            }
        }
    }
}