using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SecureNotes
{
    public class MainForm : Form
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();
        private List<Note> _allNotes = new List<Note>();
        private List<Group> _myGroups = new List<Group>();

        private string _tab = "notes"; // notes | passwords | shared
        private bool _passwordsUnlocked = false;
        private int? _selectedGroupId = null;

        // Header / sidebar / filters / content
        private Panel header, sidebar;
        private Button btnNotes, btnPasswords, btnShared, btnCreate;
        private Label lblTitle, lblSection;
        private TextBox txtSearch;
        private ComboBox cmbTagFilter;
        private FlowLayoutPanel flowCards;
        private PictureBox iconSettings, iconAccount;
        private Timer idleLockTimer;

        // Shared UI (left list + header)
        private ListBox lstGroups;
        private Label lblGroupHeader;

        // Shared toolbar panel (always visible on Shared tab, pinned at top-right)
        private Panel sharedToolbar;
        private Button btnSharedCreateNote, btnSharedCreateGroup, btnSharedJoinGroup;
        private TextBox txtSharedGroupName, txtSharedJoinCode;

        public MainForm()
        {
            BuildUI();
            ThemeManager.Apply(this, Program.CurrentTheme);

            LoadGroups();
            LoadNotes();
            RenderCurrentTab();

            idleLockTimer = new Timer { Interval = 30_000 };
            idleLockTimer.Tick += (s, e) =>
            {
                var idle = DateTime.Now - Program.LastActivity;
                if (idle.TotalMinutes >= 5)
                {
                    _passwordsUnlocked = false;
                    Program.SessionKey = null;
                    if (_tab == "passwords") RenderCurrentTab();
                }
            };
            idleLockTimer.Start();

            this.MouseDown += (s, e) => Program.TouchActivity();
            this.KeyPress += (s, e) => Program.TouchActivity();
        }

        private void BuildUI()
        {
            Text = $"SecureNotes — {Program.CurrentUser.Username}";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1120, 720);

            // Header
            header = new Panel { Location = new Point(12, 12), Size = new Size(1096, 60), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            lblTitle = new Label { Text = "SecureNotes", Location = new Point(12, 16), Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            btnCreate = new Button { Text = "Створити нотатку", Location = new Point(946, 14), Size = new Size(140, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnCreate.Click += BtnCreate_Click;

            iconSettings = new PictureBox { Location = new Point(870, 14), Size = new Size(30, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand };
            iconAccount = new PictureBox { Location = new Point(910, 14), Size = new Size(30, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, Cursor = Cursors.Hand };
            iconSettings.Paint += (s, e) => DrawThemeIcon(e.Graphics, iconSettings.ClientRectangle, Program.CurrentTheme == Theme.Dark ? Color.WhiteSmoke : Color.Black);
            iconAccount.Paint += (s, e) => DrawUserIcon(e.Graphics, iconAccount.ClientRectangle, Program.CurrentTheme == Theme.Dark ? Color.WhiteSmoke : Color.Black);

            iconSettings.Click += (s, e) =>
            {
                using (var set = new SettingsForm())
                {
                    if (set.ShowDialog(this) == DialogResult.OK)
                    {
                        ThemeManager.Apply(this, Program.CurrentTheme);
                        RenderCurrentTab();
                    }
                }
            };
            iconAccount.Click += (s, e) => ShowAccountMenu();

            header.Controls.AddRange(new Control[] { lblTitle, btnCreate, iconSettings, iconAccount });
            Controls.Add(header);

            // Sidebar
            sidebar = new Panel { Location = new Point(12, 76), Size = new Size(220, 632), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left };
            btnNotes = new Button { Text = "Мої нотатки", Location = new Point(12, 12), Size = new Size(196, 36) };
            btnPasswords = new Button { Text = "Паролі", Location = new Point(12, 54), Size = new Size(196, 36) };
            btnShared = new Button { Text = "Спільні нотатки", Location = new Point(12, 96), Size = new Size(196, 36) };

            btnNotes.Click += (s, e) => { _tab = "notes"; lblSection.Text = "Мої нотатки"; RenderCurrentTab(); };
            btnPasswords.Click += BtnPasswords_Click;
            btnShared.Click += (s, e) => { _tab = "shared"; lblSection.Text = "Спільні нотатки"; RenderCurrentTab(); };

            sidebar.Controls.AddRange(new Control[] { btnNotes, btnPasswords, btnShared });
            Controls.Add(sidebar);

            // Filters
            lblSection = new Label { Text = "Мої нотатки", Location = new Point(246, 84), Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            Controls.Add(lblSection);

            txtSearch = new TextBox { Location = new Point(246, 110), Width = 320 };
            UIHelpers.SetPlaceholder(txtSearch, "Пошук...");
            txtSearch.TextChanged += (s, e) => RenderCurrentTab();
            Controls.Add(txtSearch);

            cmbTagFilter = new ComboBox { Location = new Point(576, 110), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbTagFilter.Items.Add("(усі теги)");
            cmbTagFilter.SelectedIndex = 0;
            cmbTagFilter.SelectedIndexChanged += (s, e) => RenderCurrentTab();
            Controls.Add(cmbTagFilter);

            // Content area — нижче тулбару
            flowCards = new FlowLayoutPanel
            {
                Location = new Point(246, 270),
                Size = new Size(862, 438),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = true
            };
            Controls.Add(flowCards);

            // Shared list and header
            lstGroups = new ListBox
            {
                Location = new Point(246, 140),
                Size = new Size(220, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Visible = false
            };
            lstGroups.SelectedIndexChanged += (s, e) =>
            {
                var grp = lstGroups.SelectedItem as Group;
                _selectedGroupId = grp?.Id;
                RenderSharedDashboard();
            };
            Controls.Add(lstGroups);

            lblGroupHeader = new Label
            {
                Text = "Нотатки групи",
                Location = new Point(476, 140),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Visible = false
            };
            Controls.Add(lblGroupHeader);

            // Shared toolbar panel (top-right)
            sharedToolbar = new Panel
            {
                Location = new Point(476, 170),
                Size = new Size(632, 90),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false
            };

            btnSharedCreateNote = new Button { Text = "Створити спільну нотатку", Location = new Point(8, 8), Size = new Size(200, 28) };

            txtSharedGroupName = new TextBox { Location = new Point(220, 8), Size = new Size(200, 24) };
            UIHelpers.SetPlaceholder(txtSharedGroupName, "Назва групи");
            btnSharedCreateGroup = new Button { Text = "Створити групу", Location = new Point(430, 8), Size = new Size(150, 28) };

            txtSharedJoinCode = new TextBox { Location = new Point(220, 46), Size = new Size(200, 24) };
            UIHelpers.SetPlaceholder(txtSharedJoinCode, "Код запрошення");
            btnSharedJoinGroup = new Button { Text = "Приєднатися", Location = new Point(430, 46), Size = new Size(150, 28) };

            btnSharedCreateNote.Click += (s, e) =>
            {
                Program.TouchActivity();
                if (!_selectedGroupId.HasValue) { MessageBox.Show("Оберіть групу зліва."); return; }
                var modal = new CreateNoteForm("note", null, _myGroups, _selectedGroupId);
                if (modal.ShowDialog(this) == DialogResult.OK)
                {
                    var note = modal.CreatedOrUpdatedNote;
                    note.GroupId = _selectedGroupId;
                    note.OwnerId = Program.CurrentUser.Id;
                    _db.AddNote(note);
                    LoadNotes();
                    RenderSharedDashboard();
                }
            };

            btnSharedCreateGroup.Click += (s, e) =>
            {
                Program.TouchActivity();
                var name = txtSharedGroupName.ForeColor == Color.Gray || txtSharedGroupName.ForeColor == Color.Silver
                    ? "Моя група"
                    : txtSharedGroupName.Text.Trim();
                var grp = _db.CreateGroup(Program.CurrentUser.Id, string.IsNullOrWhiteSpace(name) ? "Моя група" : name);
                _db.AddMember(grp.Id, Program.CurrentUser.Id, "edit");
                LoadGroups();
                _selectedGroupId = grp.Id;
                SelectGroupById(_selectedGroupId);
                _tab = "shared";
                lblSection.Text = "Спільні нотатки";
                RenderSharedDashboard();
                MessageBox.Show($"Групу створено. Код: {grp.InviteCode}");
            };

            btnSharedJoinGroup.Click += (s, e) =>
            {
                Program.TouchActivity();
                var code = txtSharedJoinCode.ForeColor == Color.Gray || txtSharedJoinCode.ForeColor == Color.Silver
                    ? ""
                    : txtSharedJoinCode.Text.Trim();
                if (string.IsNullOrWhiteSpace(code)) { MessageBox.Show("Введіть код запрошення."); return; }

                var grp = _db.GetGroupByInvite(code);
                if (grp == null) { MessageBox.Show("Група не знайдена."); return; }

                _db.AddMember(grp.Id, Program.CurrentUser.Id, "edit");
                LoadGroups();
                _selectedGroupId = grp.Id;
                SelectGroupById(_selectedGroupId);
                _tab = "shared";
                lblSection.Text = "Спільні нотатки";
                RenderSharedDashboard();
                MessageBox.Show("Ви приєдналися до групи.");
            };

            sharedToolbar.Controls.AddRange(new Control[]
            {
                btnSharedCreateNote, txtSharedGroupName, btnSharedCreateGroup,
                txtSharedJoinCode, btnSharedJoinGroup
            });

            Controls.Add(sharedToolbar);
            sharedToolbar.BringToFront();
        }

        private void BtnPasswords_Click(object sender, EventArgs e)
        {
            using (var pin = new PinPromptForm(Program.CurrentUser))
            {
                if (pin.ShowDialog(this) == DialogResult.OK)
                {
                    _passwordsUnlocked = true;
                    _tab = "passwords";
                    lblSection.Text = "Паролі";
                    RenderCurrentTab();
                }
                else
                {
                    _passwordsUnlocked = false;
                }
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();

            var defaultType = _tab == "passwords" ? "password" : "note";
            var preselectGroupId = _tab == "shared" && _selectedGroupId.HasValue ? _selectedGroupId : null;

            var modal = new CreateNoteForm(defaultType, null, _myGroups, preselectGroupId);
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                var note = modal.CreatedOrUpdatedNote;
                note.OwnerId = Program.CurrentUser.Id;
                if (preselectGroupId.HasValue) note.GroupId = preselectGroupId;

                note.Id = _db.AddNote(note);
                LoadNotes();

                if (note.GroupId.HasValue)
                {
                    _selectedGroupId = note.GroupId;
                    SelectGroupById(_selectedGroupId);
                    _tab = "shared";
                    lblSection.Text = "Спільні нотатки";
                    RenderSharedDashboard();
                }
                else
                {
                    _tab = "notes";
                    lblSection.Text = "Мої нотатки";
                    RenderCurrentTab();
                }
            }
        }

        private void RenderCurrentTab()
        {
            if (_tab == "shared") RenderSharedDashboard();
            else RenderNotesOrPasswords();
        }

        private void RenderNotesOrPasswords()
        {
            flowCards.SuspendLayout();
            flowCards.Controls.Clear();

            lstGroups.Visible = false;
            lblGroupHeader.Visible = false;
            sharedToolbar.Visible = false;

            IEnumerable<Note> filtered;
            if (_tab == "notes")
                filtered = _allNotes.Where(n => n.Type == "note" && n.GroupId == null && n.OwnerId == Program.CurrentUser.Id);
            else
                filtered = _allNotes.Where(n => n.Type == "password");

            filtered = filtered.Where(TagMatches).Where(TextMatches);

            foreach (var note in filtered)
            {
                var card = new NoteCard();
                card.ApplyTheme(Program.CurrentTheme);

                string decrypted = null;
                if (note.Type == "password")
                {
                    if (_passwordsUnlocked && note.IsEncrypted && Program.SessionKey != null)
                    {
                        try { decrypted = CryptoService.DecryptAes(note.IvBase64, note.Content, Program.SessionKey); }
                        catch { decrypted = "(не вдалося розшифрувати)"; }
                    }
                }

                card.Bind(note, decrypted);
                card.DeleteRequested += (s, id) =>
                {
                    _db.DeleteNote(id);
                    LoadNotes();
                    RenderCurrentTab();
                };
                card.EditRequested += (s, id) =>
                {
                    var n = _db.GetNoteById(id);
                    var modal = new CreateNoteForm(n.Type, n, _myGroups, n.GroupId);
                    if (modal.ShowDialog(this) == DialogResult.OK)
                    {
                        var updated = modal.CreatedOrUpdatedNote;
                        _db.UpdateNote(updated);
                        LoadNotes();

                        if (!updated.GroupId.HasValue)
                        {
                            _tab = "notes";
                            lblSection.Text = "Мої нотатки";
                            RenderCurrentTab();
                        }
                        else
                        {
                            _selectedGroupId = updated.GroupId;
                            SelectGroupById(_selectedGroupId);
                            _tab = "shared";
                            lblSection.Text = "Спільні нотатки";
                            RenderSharedDashboard();
                        }
                    }
                };

                flowCards.Controls.Add(card);
            }

            flowCards.ResumeLayout();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void RenderSharedDashboard()
        {
            flowCards.SuspendLayout();
            flowCards.Controls.Clear();

            // Show shared UI
            lstGroups.Visible = true;
            lblGroupHeader.Visible = true;
            sharedToolbar.Visible = true;

            // Select first group if none is selected
            if (_selectedGroupId == null && lstGroups.Items.Count > 0 && lstGroups.SelectedItem == null)
            {
                lstGroups.SelectedIndex = 0;
                _selectedGroupId = (lstGroups.SelectedItem as Group)?.Id;
            }

            // Resolve active group
            Group grp = null;
            if (_selectedGroupId.HasValue)
                grp = _myGroups.FirstOrDefault(g => g.Id == _selectedGroupId.Value);
            if (grp == null && lstGroups.SelectedItem is Group gSel)
            {
                grp = gSel;
                _selectedGroupId = grp.Id;
            }

            lblGroupHeader.Text = grp == null ? "Нотатки групи" : $"Нотатки групи: {grp.Name}";

            // If no group selected, show hint
            if (grp == null)
            {
                var hint = new Label
                {
                    Text = "Оберіть групу зліва або створіть нову/приєднайтесь за кодом.",
                    AutoSize = true,
                    Location = new Point(476, 270),
                    ForeColor = Program.CurrentTheme == Theme.Dark ? Color.Gainsboro : Color.DimGray
                };
                flowCards.Controls.Add(hint);
                flowCards.ResumeLayout();
                return;
            }

            var notes = _db.GetNotesForGroup(grp.Id).Where(TagMatches).Where(TextMatches);

            foreach (var note in notes)
            {
                var card = new NoteCard();
                card.ApplyTheme(Program.CurrentTheme);
                card.Bind(note, null); // не розкриваємо паролі у спільних

                card.DeleteRequested += (s, id) =>
                {
                    _db.DeleteNote(id);
                    LoadNotes();
                    RenderSharedDashboard();
                };

                card.EditRequested += (s, id) =>
                {
                    var n = _db.GetNoteById(id);
                    var modal = new CreateNoteForm(n.Type, n, _myGroups, n.GroupId);
                    if (modal.ShowDialog(this) == DialogResult.OK)
                    {
                        var updated = modal.CreatedOrUpdatedNote;
                        _db.UpdateNote(updated);
                        LoadNotes();

                        if (updated.GroupId.HasValue)
                        {
                            _selectedGroupId = updated.GroupId;
                            SelectGroupById(_selectedGroupId);
                            RenderSharedDashboard();
                        }
                        else
                        {
                            _tab = "notes";
                            lblSection.Text = "Мої нотатки";
                            RenderCurrentTab();
                        }
                    }
                };

                flowCards.Controls.Add(card);
            }

            flowCards.ResumeLayout();
        }

        private void LoadNotes()
        {
            _allNotes = _db.GetNotesForUser(Program.CurrentUser.Id);

            var tags = _allNotes
                .SelectMany(n => (n.Tags ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            cmbTagFilter.Items.Clear();
            cmbTagFilter.Items.Add("(усі теги)");
            foreach (var t in tags) cmbTagFilter.Items.Add(t);
            if (cmbTagFilter.Items.Count > 0) cmbTagFilter.SelectedIndex = 0;
        }

        private void LoadGroups()
        {
            _myGroups = _db.GetGroupsForUser(Program.CurrentUser.Id);
            lstGroups.Items.Clear();
            foreach (var g in _myGroups) lstGroups.Items.Add(g);
            lstGroups.DisplayMember = "Name";

            if (_selectedGroupId.HasValue) SelectGroupById(_selectedGroupId);
        }

        private void SelectGroupById(int? groupId)
        {
            if (!groupId.HasValue) return;
            var idx = _myGroups.FindIndex(g => g.Id == groupId.Value);
            if (idx >= 0) lstGroups.SelectedIndex = idx;
        }

        private bool TagMatches(Note n)
        {
            var selectedTag = cmbTagFilter.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedTag) || selectedTag == "(усі теги)") return true;
            var tags = (n.Tags ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
            return tags.Contains(selectedTag);
        }

        private bool TextMatches(Note n)
        {
            var isPlaceholder = txtSearch.ForeColor == Color.Gray || txtSearch.ForeColor == Color.Silver;
            var q = isPlaceholder ? "" : txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(q)) return true;

            return (n.Title ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                   || (n.Type == "password" ? "" : (n.Content ?? "")).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void ShowAccountMenu()
        {
            Program.TouchActivity();

            var menu = new ContextMenuStrip();
            menu.Items.Add($"Акаунт: {Program.CurrentUser.Username}");
            menu.Items.Add($"Створено: {Program.CurrentUser.CreatedAt}");
            menu.Items.Add(new ToolStripSeparator());

            var itemDelete = new ToolStripMenuItem("Видалити акаунт");
            itemDelete.Click += (s, e) => { using (var f = new DeleteAccountForm()) f.ShowDialog(this); };

            var itemSwitch = new ToolStripMenuItem("Перемкнути акаунт");
            itemSwitch.Click += (s, e) =>
            {
                Hide();
                using (var login = new LoginForm())
                {
                    if (login.ShowDialog() == DialogResult.OK)
                    {
                        Program.CurrentUser = login.LoggedInUser;
                        Program.SessionKey = null;
                        _passwordsUnlocked = false;
                        Show();

                        Program.CurrentTheme = (Program.CurrentUser.PreferredTheme ?? "Light") == "Dark" ? Theme.Dark : Theme.Light;
                        ThemeManager.Apply(this, Program.CurrentTheme);

                        _selectedGroupId = null;
                        LoadGroups();
                        LoadNotes();
                        _tab = "notes";
                        lblSection.Text = "Мої нотатки";
                        RenderCurrentTab();
                    }
                    else
                    {
                        Close();
                    }
                }
            };

            var itemExit = new ToolStripMenuItem("Вихід з програми");
            itemExit.Click += (s, e) => Close();

            menu.Items.Add(itemDelete);
            menu.Items.Add(itemSwitch);
            menu.Items.Add(itemExit);
            menu.Show(this, new Point(iconAccount.Left, iconAccount.Bottom));
        }

        private void DrawThemeIcon(Graphics g, Rectangle rect, Color color)
        {
            using var pen = new Pen(color, 2);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var cx = rect.Left + rect.Width / 2;
            var cy = rect.Top + rect.Height / 2;
            g.DrawEllipse(pen, cx - 10, cy - 10, 20, 20);
            using var brush = new SolidBrush(color);
            g.FillPie(brush, cx - 10, cy - 10, 20, 20, 270, 180);
        }

        private void DrawUserIcon(Graphics g, Rectangle rect, Color color)
        {
            using var pen = new Pen(color, 2);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var cx = rect.Left + rect.Width / 2;
            var cy = rect.Top + rect.Height / 2;
            g.DrawEllipse(pen, cx - 6, cy - 10, 12, 12); // голова
            g.DrawArc(pen, cx - 12, cy + 2, 24, 14, 20, 140); // плечі
        }
    }
}