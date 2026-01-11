using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class JoinGroupDialog : Form
    {
        private TextBox txtCode;
        private Button btnOk, btnCancel;

        public string InviteCode => txtCode.Text.Trim();

        public JoinGroupDialog()
        {
            Text = "Приєднатися до групи";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 140);
            FormBorderStyle = FormStartPosition.CenterParent == FormStartPosition.CenterParent ? FormBorderStyle.FixedDialog : FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lbl = new Label { Text = "Код запрошення", Location = new Point(16, 16) };
            txtCode = new TextBox { Location = new Point(16, 36), Width = 320 };

            btnOk = new Button { Text = "Приєднатися", Location = new Point(176, 80), Width = 100 };
            btnCancel = new Button { Text = "Скасувати", Location = new Point(286, 80), Width = 80 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(InviteCode))
                {
                    MessageBox.Show("Введіть код запрошення.");
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lbl, txtCode, btnOk, btnCancel });
        }
    }
}