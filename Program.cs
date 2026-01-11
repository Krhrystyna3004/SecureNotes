using System;
using System.Windows.Forms;

namespace SecureNotes
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1) Запускаємо логін, щоб отримати реального користувача з БД
            User logged = null;
            using (var login = new LoginForm())
            {
                if (login.ShowDialog() == DialogResult.OK)
                {
                    logged = login.LoggedInUser;
                }
            }

            if (logged == null)
            {
                // Якщо користувач не увійшов — завершити програму
                return;
            }

            // 2) Ініціалізуємо контекст сесії коректним користувачем і темою з БД
            CurrentUser = logged;
            CurrentTheme = (logged.PreferredTheme ?? "Light") == "Dark" ? Theme.Dark : Theme.Light;
            LastActivity = DateTime.Now;
            SessionKey = null; // буде встановлено після вводу PIN у PinPromptForm

            // 3) Запускаємо головну форму вже після логіну
            Application.Run(new MainForm());
        }

        public static User CurrentUser { get; set; }
        public static Theme CurrentTheme { get; set; }
        public static DateTime LastActivity { get; set; } = DateTime.Now;
        public static byte[] SessionKey { get; set; }

        public static void TouchActivity()
        {
            LastActivity = DateTime.Now;
        }
    }
}