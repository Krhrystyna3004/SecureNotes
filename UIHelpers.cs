using System.Drawing;
using System.Windows.Forms;

namespace SecureNotes
{
    public static class UIHelpers
    {
        // Плейсхолдер керується через Tag, щоб не залежати від кольорів теми
        public static void SetPlaceholder(TextBox box, string placeholderText)
        {
            box.Tag = new PlaceholderState { Text = placeholderText, IsActive = true };
            ApplyPlaceholder(box);

            box.GotFocus += (s, e) =>
            {
                var st = box.Tag as PlaceholderState;
                if (st != null && st.IsActive)
                {
                    box.Text = "";
                    st.IsActive = false;
                    box.ForeColor = ThemeIsDark() ? Color.WhiteSmoke : Color.Black;
                }
            };

            box.LostFocus += (s, e) =>
            {
                var st = box.Tag as PlaceholderState;
                if (st != null && string.IsNullOrWhiteSpace(box.Text))
                {
                    st.IsActive = true;
                    ApplyPlaceholder(box);
                }
            };
        }

        public static bool IsPlaceholder(TextBox box)
        {
            var st = box.Tag as PlaceholderState;
            return st != null && st.IsActive;
        }

        private static void ApplyPlaceholder(TextBox box)
        {
            var st = box.Tag as PlaceholderState;
            if (st == null) return;
            box.Text = st.Text;
            box.ForeColor = ThemeIsDark() ? Color.Silver : Color.Gray;
        }

        private static bool ThemeIsDark() => Program.CurrentTheme == Theme.Dark;

        private class PlaceholderState
        {
            public string Text { get; set; }
            public bool IsActive { get; set; }
        }
    }
}