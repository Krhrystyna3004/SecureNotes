using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class SettingsForm : Form
    {
        private RadioButton rbLight, rbDark;
        private Button btnSave;
        private DatabaseHelper _db = new DatabaseHelper();

        public SettingsForm()
        {
            Text = "Налаштування";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(320, 180);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            rbLight = new RadioButton { Text = "Світла тема", Location = new Point(16, 16), Checked = Program.CurrentTheme == Theme.Light };
            rbDark = new RadioButton { Text = "Темна тема", Location = new Point(16, 40), Checked = Program.CurrentTheme == Theme.Dark };

            btnSave = new Button { Text = "Зберегти", Location = new Point(16, 80), Width = 280 };
            btnSave.Click += (s, e) =>
            {
                Program.TouchActivity();

                var theme = rbDark.Checked ? "Dark" : "Light";
                _db.UpdateUserTheme(Program.CurrentUser.Id, theme);
                Program.CurrentTheme = theme == "Dark" ? Theme.Dark : Theme.Light;
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.AddRange(new Control[] { rbLight, rbDark, btnSave });
        }
    }
}