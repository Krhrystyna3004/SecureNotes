using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public enum Theme { Light, Dark }

    public static class ThemeManager
    {
        public static void Apply(Form f, Theme theme)
        {
            if (theme == Theme.Dark)
            {
                f.BackColor = Color.FromArgb(20, 22, 24);
                f.ForeColor = Color.Gainsboro;
            }
            else
            {
                f.BackColor = Color.FromArgb(248, 250, 252);
                f.ForeColor = Color.Black;
            }

            foreach (Control c in f.Controls) ApplyControl(c, theme);
        }

        private static void ApplyControl(Control c, Theme theme)
        {
            if (theme == Theme.Dark)
            {
                // Контрасти для читабельності
                if (c is Panel || c is FlowLayoutPanel || c is ListBox)
                    c.BackColor = Color.FromArgb(28, 30, 34);
                else if (c is Button)
                    c.BackColor = Color.FromArgb(45, 49, 54);

                c.ForeColor = Color.Gainsboro;

                if (c is TextBox tb)
                {
                    tb.BackColor = Color.FromArgb(34, 37, 41);
                    // Плейсхолдер — світліший сірий, звичайний текст — Gainsboro
                    tb.ForeColor = UIHelpers.IsPlaceholder(tb) ? Color.Silver : Color.Gainsboro;
                }

                if (c is ComboBox cb)
                {
                    cb.BackColor = Color.FromArgb(34, 37, 41);
                    cb.ForeColor = Color.Gainsboro;
                }

                if (c is Label lbl)
                {
                    // заголовки трохи світліші
                    lbl.ForeColor = Color.Gainsboro;
                }
            }
            else
            {
                if (c is Panel || c is FlowLayoutPanel || c is ListBox)
                    c.BackColor = Color.White;
                else if (c is Button)
                    c.BackColor = SystemColors.Control;

                c.ForeColor = Color.Black;

                if (c is TextBox tb)
                {
                    tb.BackColor = Color.White;
                    tb.ForeColor = UIHelpers.IsPlaceholder(tb) ? Color.Gray : Color.Black;
                }

                if (c is ComboBox cb)
                {
                    cb.BackColor = Color.White;
                    cb.ForeColor = Color.Black;
                }
            }

            foreach (Control child in c.Controls) ApplyControl(child, theme);
        }
    }
}