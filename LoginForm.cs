using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class LoginForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnRegister;
        private DatabaseHelper _db = new DatabaseHelper();

        public User LoggedInUser { get; private set; }

        public LoginForm()
        {
            Text = "Вхід";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(360, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var lblU = new Label { Text = "Логін", Location = new Point(16, 20) };
            txtUsername = new TextBox { Location = new Point(16, 40), Width = 320 };
            var lblP = new Label { Text = "Пароль", Location = new Point(16, 72) };
            txtPassword = new TextBox { Location = new Point(16, 92), Width = 320, UseSystemPasswordChar = true };

            btnLogin = new Button { Text = "Увійти", Location = new Point(16, 130), Width = 150 };
            btnRegister = new Button { Text = "Зареєструватися", Location = new Point(186, 130), Width = 150 };

            btnLogin.Click += BtnLogin_Click;
            btnRegister.Click += BtnRegister_Click;

            Controls.AddRange(new Control[] { lblU, txtUsername, lblP, txtPassword, btnLogin, btnRegister });
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();

            var un = txtUsername.Text.Trim();
            var pw = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(un) || string.IsNullOrWhiteSpace(pw))
            {
                MessageBox.Show("Введіть логін і пароль.");
                return;
            }

            var salt = CryptoService.GenerateSalt();
            var hash = CryptoService.HashWithPBKDF2(pw, salt);

            var user = new User
            {
                Username = un,
                PasswordSalt = salt,
                PasswordHash = hash,
                PreferredTheme = "Light"
            };

            try
            {
                user.Id = _db.CreateUser(user);
                LoggedInUser = user;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Користувач вже існує або помилка БД.\n" + ex.Message);
            }
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();

            var un = txtUsername.Text.Trim();
            var pw = txtPassword.Text;

            var user = _db.GetUserByUsername(un);
            if (user == null)
            {
                MessageBox.Show("Користувач не знайдений.");
                return;
            }

            var hash = CryptoService.HashWithPBKDF2(pw, user.PasswordSalt);
            if (hash != user.PasswordHash)
            {
                MessageBox.Show("Невірний пароль.");
                return;
            }

            LoggedInUser = user;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // LoginForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "LoginForm";
            this.Load += new System.EventHandler(this.LoginForm_Load);
            this.ResumeLayout(false);

        }

        private void LoginForm_Load(object sender, EventArgs e)
        {

        }
    }
}