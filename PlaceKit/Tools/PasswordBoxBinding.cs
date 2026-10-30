using System.Windows;
using System.Windows.Controls;

namespace PlaceKit.Tools
{
    // PasswordBox.Password can't be bound, so the view model gets the text through this attached property.
    // The view model sets it only to clear the box.
    public static class PasswordBoxBinding
    {
        // The default is null, so that binding to an empty string still runs the callback that hooks up the box.
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxBinding),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPasswordChanged));

        public static string GetPassword(DependencyObject element) => (string)element.GetValue(PasswordProperty);

        public static void SetPassword(DependencyObject element, string value) => element.SetValue(PasswordProperty, value);

        static void OnPasswordChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
        {
            if (element is PasswordBox box)
            {
                box.PasswordChanged -= HandlePasswordChanged;
                string password = (string)e.NewValue ?? string.Empty;
                if (box.Password != password)
                {
                    box.Password = password;
                }

                box.PasswordChanged += HandlePasswordChanged;
            }
        }

        static void HandlePasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordBox box = (PasswordBox)sender;
            SetPassword(box, box.Password);
        }
    }
}
