using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class DeleteAccountForm : Form
    {
        private TextBox txtPassword;
        private Button btnDelete;
        private DatabaseHelper _db = new DatabaseHelper();

        public DeleteAccountForm()
        {
            Text = "Видалити акаунт";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 160);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var lbl = new Label { Text = "Введіть пароль для підтвердження:", Location = new Point(16, 20), Width = 320 };
            txtPassword = new TextBox { Location = new Point(16, 50), Width = 320, UseSystemPasswordChar = true };

            btnDelete = new Button { Text = "Видалити акаунт", Location = new Point(16, 90), Width = 320 };
            btnDelete.Click += BtnDelete_Click;

            Controls.AddRange(new Control[] { lbl, txtPassword, btnDelete });
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();

            var pw = txtPassword.Text;
            var user = Program.CurrentUser;

            var hash = CryptoService.HashWithPBKDF2(pw, user.PasswordSalt);
            if (hash != user.PasswordHash)
            {
                MessageBox.Show("Невірний пароль.");
                return;
            }

            _db.DeleteUser(user.Id);
            MessageBox.Show("Акаунт видалено.");
            Application.Exit();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DeleteAccountForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "DeleteAccountForm";
            this.Load += new System.EventHandler(this.DeleteAccountForm_Load);
            this.ResumeLayout(false);

        }

        private void DeleteAccountForm_Load(object sender, EventArgs e)
        {

        }
    }
}