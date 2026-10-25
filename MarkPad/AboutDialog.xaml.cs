using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace MarkPad;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
    }

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
