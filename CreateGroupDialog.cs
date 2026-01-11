using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class CreateGroupDialog : Form
    {
        private TextBox txtName;
        private Button btnOk, btnCancel;

        public string GroupName => txtName.Text.Trim();

        public CreateGroupDialog()
        {
            Text = "Створити групу";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 140);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label { Text = "Назва групи", Location = new Point(16, 16) };
            txtName = new TextBox { Location = new Point(16, 36), Width = 320 };

            btnOk = new Button { Text = "Створити", Location = new Point(176, 80), Width = 80 };
            btnCancel = new Button { Text = "Скасувати", Location = new Point(256, 80), Width = 80 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(GroupName))
                {
                    MessageBox.Show("Введіть назву групи.");
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lbl, txtName, btnOk, btnCancel });
        }
    }
}