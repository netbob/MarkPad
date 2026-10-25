using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MarkPad.Git;

namespace MarkPad;

public partial class CommitDialog : Window
{
    public string CommitMessage => MessageTextBox.Text.Trim();
    public List<string> SelectedFiles { get; } = new();

    public CommitDialog(List<GitStatusEntry> entries)
    {
        InitializeComponent();
        foreach (var entry in entries)
        {
            var checkBox = new CheckBox
            {
                Content = $"{entry.Status}  {entry.Path}",
                Tag = entry.Path,
                IsChecked = true,
                Margin = new Thickness(0, 2, 0, 2)
            };
            FileList.Items.Add(checkBox);
        }

        MessageTextBox.Focus();
    }

    private void Commit_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CommitMessage))
        {
            MessageBox.Show(this, "Enter a commit message.", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var item in FileList.Items.OfType<CheckBox>())
        {
            if (item.IsChecked == true && item.Tag is string path)
                SelectedFiles.Add(path);
        }

        if (SelectedFiles.Count == 0)
        {
            MessageBox.Show(this, "Check at least one file.", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}
