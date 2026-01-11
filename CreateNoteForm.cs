using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SecureNotes
{
    public class CreateNoteForm : Form
    {
        public Note CreatedOrUpdatedNote { get; private set; }
        private Note EditingNote;

        private TextBox txtTitle;
        private TextBox txtContent;
        private RadioButton rbNote;
        private RadioButton rbPassword;
        private ComboBox cmbColor;
        private TextBox txtTags;
        private CheckBox chkShared;
        private ComboBox cmbGroup;

        private readonly List<Group> _groups;
        private readonly int? _preselectGroupId;

        public CreateNoteForm(string defaultType, Note toEdit, List<Group> groups, int? preselectGroupId)
        {
            EditingNote = toEdit;
            _groups = groups ?? new List<Group>();
            _preselectGroupId = preselectGroupId;

            Text = toEdit == null ? "Створити нотатку" : "Редагувати нотатку";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var lblTitle = new Label { Text = "Заголовок", Location = new Point(16, 16) };
            txtTitle = new TextBox { Location = new Point(16, 36), Width = 480 };

            var lblContent = new Label { Text = "Зміст", Location = new Point(16, 68) };
            txtContent = new TextBox { Location = new Point(16, 88), Width = 480, Height = 180, Multiline = true, ScrollBars = ScrollBars.Vertical };

            var grpType = new GroupBox { Text = "Тип", Location = new Point(16, 274), Size = new Size(480, 50) };
            rbNote = new RadioButton { Text = "Звичайна", Location = new Point(12, 22), Checked = (toEdit?.Type ?? defaultType) == "note" };
            rbPassword = new RadioButton { Text = "Пароль", Location = new Point(120, 22), Checked = (toEdit?.Type ?? defaultType) == "password" };
            grpType.Controls.AddRange(new Control[] { rbNote, rbPassword });

            var lblColor = new Label { Text = "Колір", Location = new Point(16, 330) };
            cmbColor = new ComboBox { Location = new Point(16, 350), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbColor.Items.AddRange(new object[] { "#FFD6E8", "#E6F3FF", "#E8F5E9", "#FFF4E6", "#F3E5F5", "#FFF9C4", "#FFFFFF" });
            cmbColor.SelectedIndex = 0;

            var lblTags = new Label { Text = "Теги (через ;)", Location = new Point(216, 330) };
            txtTags = new TextBox { Location = new Point(216, 350), Width = 280 };

            chkShared = new CheckBox { Text = "Спільна нотатка", Location = new Point(16, 382), Width = 480 };
            var lblGroup = new Label { Text = "Група", Location = new Point(16, 410) };
            cmbGroup = new ComboBox { Location = new Point(16, 430), Width = 480, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = false };

            chkShared.CheckedChanged += (s, e) =>
            {
                cmbGroup.Enabled = chkShared.Checked;
                if (chkShared.Checked)
                {
                    PopulateGroups();
                    PreselectGroup();
                }
            };

            var btnCreate = new Button { Text = toEdit == null ? "Створити" : "Зберегти", Location = new Point(316, 490), Width = 180 };
            btnCreate.Click += BtnCreate_Click;

            Controls.AddRange(new Control[] { lblTitle, txtTitle, lblContent, txtContent, grpType, lblColor, cmbColor, lblTags, txtTags, chkShared, lblGroup, cmbGroup, btnCreate });

            // Ініціалізація значень при редагуванні
            if (toEdit != null)
            {
                txtTitle.Text = toEdit.Title;

                if (toEdit.Type == "password")
                {
                    if (Program.SessionKey != null && toEdit.IsEncrypted)
                    {
                        try
                        {
                            txtContent.Text = CryptoService.DecryptAes(toEdit.IvBase64, toEdit.Content, Program.SessionKey);
                        }
                        catch
                        {
                            txtContent.Text = "(не вдалося розшифрувати) — введіть новий зміст";
                        }
                    }
                    else
                    {
                        txtContent.Text = "•••••••••• (PIN не розблоковано) — введіть новий зміст";
                    }
                }
                else
                {
                    txtContent.Text = toEdit.Content ?? "";
                }

                cmbColor.SelectedItem = toEdit.Color;
                txtTags.Text = toEdit.Tags ?? "";

                if (toEdit.GroupId.HasValue)
                {
                    chkShared.Checked = true;
                    cmbGroup.Enabled = true;
                    PopulateGroups();
                    var idx = _groups.FindIndex(g => g.Id == toEdit.GroupId.Value);
                    if (idx >= 0) cmbGroup.SelectedIndex = idx;
                }
            }
            else
            {
                if (_preselectGroupId.HasValue)
                {
                    chkShared.Checked = true;
                    cmbGroup.Enabled = true;
                    PopulateGroups();
                    PreselectGroup();
                }
            }
        }

        private void PopulateGroups()
        {
            cmbGroup.Items.Clear();
            foreach (var g in _groups) cmbGroup.Items.Add(g);
            cmbGroup.DisplayMember = "Name";
        }

        private void PreselectGroup()
        {
            if (_preselectGroupId.HasValue)
            {
                var idx = _groups.FindIndex(g => g.Id == _preselectGroupId.Value);
                if (idx >= 0) cmbGroup.SelectedIndex = idx;
            }
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();

            var title = txtTitle.Text.Trim();
            var content = txtContent.Text;
            var type = rbPassword.Checked ? "password" : "note";
            var color = cmbColor.SelectedItem?.ToString() ?? "#FFFFFF";
            var tags = txtTags.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Введіть заголовок.");
                return;
            }

            int? groupId = null;
            if (chkShared.Checked)
            {
                if (cmbGroup.SelectedItem is Group g) groupId = g.Id;
                else { MessageBox.Show("Оберіть групу для спільної нотатки."); return; }
            }

            if (EditingNote == null)
            {
                var note = new Note
                {
                    OwnerId = Program.CurrentUser.Id,
                    Title = title,
                    Type = type,
                    Color = color,
                    Tags = tags,
                    GroupId = groupId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                if (type == "password")
                {
                    if (Program.SessionKey == null)
                    {
                        MessageBox.Show("Спочатку розблокуйте вкладку Паролі (PIN).");
                        return;
                    }
                    var enc = CryptoService.EncryptAes(content, Program.SessionKey);
                    note.Content = enc.cipherBase64;
                    note.IvBase64 = enc.ivBase64;
                }
                else
                {
                    note.Content = content;
                    note.IvBase64 = null;
                }

                CreatedOrUpdatedNote = note;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                // Редагування
                EditingNote.Title = title;
                EditingNote.Type = type;
                EditingNote.Color = color;
                EditingNote.Tags = tags;
                EditingNote.GroupId = groupId;
                EditingNote.UpdatedAt = DateTime.Now;

                if (type == "password")
                {
                    var isMasked = content.StartsWith("••") || content.StartsWith("(не вдалося");
                    if (!isMasked && !string.IsNullOrWhiteSpace(content))
                    {
                        if (Program.SessionKey == null)
                        {
                            MessageBox.Show("Спочатку розблокуйте вкладку Паролі (PIN).");
                            return;
                        }
                        var enc = CryptoService.EncryptAes(content, Program.SessionKey);
                        EditingNote.Content = enc.cipherBase64;
                        EditingNote.IvBase64 = enc.ivBase64;
                    }
                    // якщо залишили маску — не змінюємо шифр
                }
                else
                {
                    EditingNote.Content = content;
                    EditingNote.IvBase64 = null;
                }

                CreatedOrUpdatedNote = EditingNote;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}