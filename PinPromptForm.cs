using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class PinPromptForm : Form
    {
        private TextBox txtPin;
        private Button btnOk;
        private Button btnSetPin;
        private readonly DatabaseHelper _db = new DatabaseHelper();
        private readonly User _user;

        public PinPromptForm(User user)
        {
            _user = user;
            Text = "PIN для доступу до паролів";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            var lbl = new Label { Text = "Введіть PIN:", Location = new Point(16, 20) };
            txtPin = new TextBox { Location = new Point(16, 40), Width = 340, UseSystemPasswordChar = true };

            btnOk = new Button { Text = "Розблокувати", Location = new Point(16, 80), Width = 160 };
            btnSetPin = new Button { Text = string.IsNullOrEmpty(_user.PinHash) ? "Створити PIN" : "Змінити PIN", Location = new Point(196, 80), Width = 160 };

            var lblInfo = new Label { Text = "Для зміни PIN потрібно ввести старий PIN у поле.", Location = new Point(16, 120), Width = 340, ForeColor = Color.Gray };

            btnOk.Click += BtnOk_Click;
            btnSetPin.Click += BtnSetPin_Click;

            Controls.AddRange(new Control[] { lbl, txtPin, btnOk, btnSetPin, lblInfo });
        }

        private void BtnSetPin_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();

            var pin = txtPin.Text.Trim();
            if (pin.Length < 4)
            {
                MessageBox.Show("PIN має бути щонайменше 4 цифри.");
                return;
            }

            if (!string.IsNullOrEmpty(_user.PinHash) && !string.IsNullOrEmpty(_user.PinSalt))
            {
                var oldHash = CryptoService.HashWithPBKDF2(pin, _user.PinSalt);
                if (oldHash != _user.PinHash)
                {
                    MessageBox.Show("Невірний старий PIN. Введіть старий PIN у поле та спробуйте ще раз.");
                    return;
                }
                var newPin = Prompt.Show("Введіть новий PIN:", "Новий PIN");
                if (string.IsNullOrWhiteSpace(newPin) || newPin.Length < 4)
                {
                    MessageBox.Show("Новий PIN має бути щонайменше 4 цифри.");
                    return;
                }
                var newSalt = CryptoService.GenerateSalt();
                var newHash = CryptoService.HashWithPBKDF2(newPin, newSalt);
                try
                {
                    _db.UpdateUserPin(_user.Id, oldHash, newHash, newSalt);
                    _user.PinSalt = newSalt;
                    _user.PinHash = newHash;
                    MessageBox.Show("PIN змінено.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не вдалося змінити PIN: " + ex.Message);
                }
            }
            else
            {
                var newSalt = CryptoService.GenerateSalt();
                var newHash = CryptoService.HashWithPBKDF2(pin, newSalt);
                try
                {
                    _db.UpdateUserPin(_user.Id, "", newHash, newSalt);
                    _user.PinSalt = newSalt;
                    _user.PinHash = newHash;
                    MessageBox.Show("PIN створено.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не вдалося створити PIN: " + ex.Message);
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            Program.TouchActivity();

            var pin = txtPin.Text.Trim();
            if (string.IsNullOrEmpty(_user.PinHash) || string.IsNullOrEmpty(_user.PinSalt))
            {
                MessageBox.Show("Спочатку встановіть PIN.");
                return;
            }

            var hash = CryptoService.HashWithPBKDF2(pin, _user.PinSalt);
            if (hash != _user.PinHash)
            {
                MessageBox.Show("Невірний PIN.");
                return;
            }

            Program.SessionKey = CryptoService.DeriveKeyFromPin(pin, _user.PinSalt);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PinPromptForm
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "PinPromptForm";
            this.Load += new System.EventHandler(this.PinPromptForm_Load);
            this.ResumeLayout(false);

        }

        private void PinPromptForm_Load(object sender, EventArgs e)
        {

        }
    }

    public static class Prompt
    {
        public static string Show(string text, string caption)
        {
            var form = new Form { Width = 380, Height = 160, Text = caption, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var lbl = new Label { Left = 16, Top = 16, Text = text, Width = 340 };
            var txt = new TextBox { Left = 16, Top = 40, Width = 340, UseSystemPasswordChar = true };
            var btnOk = new Button { Text = "OK", Left = 216, Width = 140, Top = 80, DialogResult = DialogResult.OK };
            form.Controls.AddRange(new Control[] { lbl, txt, btnOk });
            form.AcceptButton = btnOk;
            return form.ShowDialog() == DialogResult.OK ? txt.Text : "";
        }
    }
}