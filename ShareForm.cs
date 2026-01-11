using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public class ShareForm : Form
    {
        private TextBox txtInviteCode;
        private Button btnCreateGroup;
        private Button btnJoinGroup;
        private Label lblYourCode;
        private TextBox txtGroupName;
        private DatabaseHelper _db = new DatabaseHelper();

        public ShareForm()
        {
            Text = "Спільні нотатки";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            lblYourCode = new Label { Text = "Ваш код: (ще не створено)", Location = new Point(16, 16), Width = 380 };

            txtGroupName = new TextBox { Location = new Point(16, 40), Width = 380 };
            UIHelpers.SetPlaceholder(txtGroupName, "Назва групи (необов'язково)");

            btnCreateGroup = new Button { Text = "Створити групу", Location = new Point(16, 72), Width = 380 };
            btnCreateGroup.Click += (s, e) =>
            {
                Program.TouchActivity();

                var grp = _db.CreateGroup(Program.CurrentUser.Id,
                    string.IsNullOrWhiteSpace(txtGroupName.Text) || txtGroupName.ForeColor == Color.Gray
                        ? "Моя група"
                        : txtGroupName.Text.Trim());

                _db.AddMember(grp.Id, Program.CurrentUser.Id, "edit");
                lblYourCode.Text = $"Ваш код: {grp.InviteCode}";
                MessageBox.Show("Групу створено. Поділися кодом з іншими.");
            };

            var lblJoin = new Label { Text = "Введіть код запрошення для приєднання", Location = new Point(16, 112), Width = 380 };
            txtInviteCode = new TextBox { Location = new Point(16, 132), Width = 380 };
            UIHelpers.SetPlaceholder(txtInviteCode, "Код запрошення групи");

            btnJoinGroup = new Button { Text = "Приєднатися до групи", Location = new Point(16, 164), Width = 380 };
            btnJoinGroup.Click += (s, e) =>
            {
                Program.TouchActivity();

                var code = txtInviteCode.Text.Trim();
                if (txtInviteCode.ForeColor == Color.Gray) code = ""; // якщо залишився плейсхолдер

                var grp = _db.GetGroupByInvite(code);
                if (grp == null)
                {
                    MessageBox.Show("Група не знайдена.");
                    return;
                }
                _db.AddMember(grp.Id, Program.CurrentUser.Id, "edit");
                MessageBox.Show("Ви приєдналися до групи. Перевірте вкладку «Спільні нотатки».");
            };

            var btnClose = new Button { Text = "Закрити", Location = new Point(16, 204), Width = 380 };
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { lblYourCode, txtGroupName, btnCreateGroup, lblJoin, txtInviteCode, btnJoinGroup, btnClose });
        }
    }
}