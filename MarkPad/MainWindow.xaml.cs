using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Search;
using MarkPad.Git;
using MarkPad.Highlighting;
using Microsoft.Win32;

namespace MarkPad;

/// <summary>
/// One window, one view: tabs of markdown source, a menu, a status bar.
/// No preview pane, no mode switching — that is the whole point.
/// Code-behind on purpose: this app is small, and the tab/editor wiring
/// is easier to read here than spread across a view-model layer.
/// </summary>
public partial class MainWindow : Window
{
    private sealed class EditorTab
    {
        public required TabItem Tab { get; init; }
        public required TextEditor Editor { get; init; }
        public required EditorDocument Document { get; init; }
        public required TextBlock HeaderText { get; init; }
    }

    private readonly List<EditorTab> _tabs = new();
    private readonly AppSettings _settings;
    private string _theme;
    private bool _wordWrap = true;

    private EditorTab? ActiveTab
    {
        get
        {
            if (TabControl.SelectedItem is not TabItem selected)
                return null;
            return _tabs.FirstOrDefault(t => t.Tab == selected);
        }
    }

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _theme = _settings.Theme == "Light" ? "Light" : "Dark";

        TabControl.SelectionChanged += OnTabSelectionChanged;
        PreviewKeyDown += OnPreviewKeyDown;
        Closing += OnClosing;
        Drop += OnDrop;

        ApplyTheme();
        DarkModeMenuItem.IsChecked = _theme == "Dark";
        WordWrapMenuItem.IsChecked = _wordWrap;

        NewTab();
    }

    // ---------- Tabs ----------

    private EditorTab NewTab()
    {
        var doc = new EditorDocument();
        var editor = CreateEditor();

        var headerText = new TextBlock { Text = doc.DisplayName, Margin = new Thickness(0, 0, 6, 0) };
        var closeButton = new Button
        {
            Content = "×",
            Width = 18,
            Height = 18,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            ToolTip = "Close tab"
        };
        var header = new StackPanel { Orientation = Orientation.Horizontal };
        header.Children.Add(headerText);
        header.Children.Add(closeButton);

        var tabItem = new TabItem { Header = header, Content = editor };
        var tab = new EditorTab { Tab = tabItem, Editor = editor, Document = doc, HeaderText = headerText };
        closeButton.Click += (_, _) => CloseTab(tab);

        _tabs.Add(tab);
        TabControl.Items.Add(tabItem);
        TabControl.SelectedItem = tabItem;
        return tab;
    }

    private TextEditor CreateEditor()
    {
        var editor = new TextEditor
        {
            ShowLineNumbers = true,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            WordWrap = _wordWrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        // Ctrl+F find panel, built into AvalonEdit.
        SearchPanel.Install(editor.TextArea);

        editor.TextArea.Caret.PositionChanged += (_, _) => UpdateCaretStatus(editor);
        editor.TextChanged += (_, _) => OnEditorTextChanged(editor);
        ApplyEditorTheme(editor);
        return editor;
    }

    private void OnEditorTextChanged(TextEditor editor)
    {
        var tab = _tabs.FirstOrDefault(t => t.Editor == editor);
        if (tab == null || tab.Document.Loading)
            return;

        tab.Document.IsDirty = true;
        UpdateTabHeader(tab);
        UpdateTitle();
    }

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateTitle();
        var tab = ActiveTab;
        if (tab != null)
            UpdateCaretStatus(tab.Editor);
        _ = RefreshGitStatusAsync();
    }

    private void UpdateTabHeader(EditorTab tab)
    {
        tab.HeaderText.Text = tab.Document.DisplayName + (tab.Document.IsDirty ? " *" : "");
    }

    private void UpdateTitle()
    {
        var tab = ActiveTab;
        Title = tab == null
            ? "MarkPad"
            : $"MarkPad - {tab.Document.DisplayName}" + (tab.Document.IsDirty ? " *" : "");
    }

    private void UpdateCaretStatus(TextEditor editor)
    {
        var location = editor.Document.GetLocation(editor.CaretOffset);
        CaretText.Text = $"Ln {location.Line}, Col {location.Column}";
    }

    private void CloseTab(EditorTab? tab)
    {
        if (tab == null)
            return;
        if (!SaveTabWithPrompt(tab))
            return;

        _tabs.Remove(tab);
        TabControl.Items.Remove(tab.Tab);
        if (_tabs.Count == 0)
            NewTab();
    }

    // ---------- File operations ----------

    private void New_Click(object sender, RoutedEventArgs e) => NewTab();

    private void Open_Click(object sender, RoutedEventArgs e) => OpenFile();

    private void OpenFile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Multiselect = true
        };
        if (!string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder))
            dlg.InitialDirectory = _settings.LastFolder;

        if (dlg.ShowDialog(this) != true)
            return;

        foreach (var file in dlg.FileNames)
            LoadFile(file);
    }

    private void LoadFile(string path)
    {
        var tab = ActiveTab;
        if (tab != null && tab.Document.FilePath == null && !tab.Document.IsDirty
            && string.IsNullOrEmpty(tab.Editor.Text))
        {
            LoadIntoTab(tab, path);
        }
        else
        {
            LoadIntoTab(NewTab(), path);
        }
    }

    private void LoadIntoTab(EditorTab tab, string path)
    {
        try
        {
            tab.Document.LoadFromFile(path, tab.Editor);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open {path}:\n{ex.Message}", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        UpdateTabHeader(tab);
        UpdateTitle();
        UpdateCaretStatus(tab.Editor);
        RememberFolder(tab.Document.Directory);
        _ = RefreshGitStatusAsync();
    }

    private void Save_Click(object sender, RoutedEventArgs e) => SaveFile();

    private void SaveFile()
    {
        var tab = ActiveTab;
        if (tab == null)
            return;
        if (tab.Document.FilePath == null)
            SaveAsFile();
        else
            SaveTab(tab);
    }

    private void SaveAs_Click(object sender, RoutedEventArgs e) => SaveAsFile();

    private void SaveAsFile()
    {
        var tab = ActiveTab;
        if (tab == null)
            return;

        var dlg = new SaveFileDialog
        {
            DefaultExt = "md",
            Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*"
        };
        if (!string.IsNullOrEmpty(_settings.LastFolder) && Directory.Exists(_settings.LastFolder))
            dlg.InitialDirectory = _settings.LastFolder;

        if (dlg.ShowDialog(this) != true)
            return;

        try
        {
            tab.Document.SaveToFile(tab.Editor, dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save:\n{ex.Message}", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        UpdateTabHeader(tab);
        UpdateTitle();
        RememberFolder(tab.Document.Directory);
        _ = RefreshGitStatusAsync();
    }

    /// <summary>Returns false only when the save itself failed.</summary>
    private bool SaveTab(EditorTab tab)
    {
        try
        {
            tab.Document.SaveToFile(tab.Editor);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not save:\n{ex.Message}", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        UpdateTabHeader(tab);
        UpdateTitle();
        RememberFolder(tab.Document.Directory);
        _ = RefreshGitStatusAsync();
        return true;
    }

    /// <summary>Returns false when the user cancels instead of resolving a dirty tab.</summary>
    private bool SaveTabWithPrompt(EditorTab tab)
    {
        if (!tab.Document.IsDirty)
            return true;

        var answer = MessageBox.Show(this, $"Save changes to {tab.Document.DisplayName}?", "MarkPad",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        if (answer == MessageBoxResult.Cancel)
            return false;
        if (answer == MessageBoxResult.No)
            return true;

        if (tab.Document.FilePath == null)
        {
            var dlg = new SaveFileDialog
            {
                DefaultExt = "md",
                Filter = "Markdown files (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) != true)
                return false;
            try
            {
                tab.Document.SaveToFile(tab.Editor, dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not save:\n{ex.Message}", "MarkPad",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            UpdateTabHeader(tab);
            UpdateTitle();
            RememberFolder(tab.Document.Directory);
            return true;
        }

        return SaveTab(tab);
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e) => CloseTab(ActiveTab);

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        foreach (var tab in _tabs.ToList())
        {
            if (!SaveTabWithPrompt(tab))
            {
                e.Cancel = true;
                return;
            }
        }

        _settings.Save();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext is ".md" or ".markdown" or ".txt")
                LoadFile(file);
        }
    }

    private void RememberFolder(string? folder)
    {
        if (!string.IsNullOrEmpty(folder))
        {
            _settings.LastFolder = folder;
            _settings.Save();
        }
    }

    // ---------- Edit menu (AvalonEdit also handles these keys natively) ----------

    private void Undo_Click(object sender, RoutedEventArgs e) => ActiveTab?.Editor.Undo();
    private void Redo_Click(object sender, RoutedEventArgs e) => ActiveTab?.Editor.Redo();
    private void Cut_Click(object sender, RoutedEventArgs e) => ActiveTab?.Editor.Cut();
    private void Copy_Click(object sender, RoutedEventArgs e) => ActiveTab?.Editor.Copy();
    private void Paste_Click(object sender, RoutedEventArgs e) => ActiveTab?.Editor.Paste();
    private void SelectAll_Click(object sender, RoutedEventArgs e) => ActiveTab?.Editor.SelectAll();

    // ---------- View menu ----------

    private void DarkMode_Click(object sender, RoutedEventArgs e)
    {
        _theme = DarkModeMenuItem.IsChecked ? "Dark" : "Light";
        _settings.Theme = _theme;
        _settings.Save();
        ApplyTheme();
    }

    private void WordWrap_Click(object sender, RoutedEventArgs e)
    {
        _wordWrap = WordWrapMenuItem.IsChecked;
        foreach (var tab in _tabs)
            tab.Editor.WordWrap = _wordWrap;
    }

    private void ApplyTheme()
    {
        var themeFile = _theme == "Dark" ? "Dark.xaml" : "Light.xaml";
        var dict = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{themeFile}")
        };
        Application.Current.Resources.MergedDictionaries.Clear();
        Application.Current.Resources.MergedDictionaries.Add(dict);
        DarkModeMenuItem.IsChecked = _theme == "Dark";
        foreach (var tab in _tabs)
            ApplyEditorTheme(tab.Editor);
    }

    private void ApplyEditorTheme(TextEditor editor)
    {
        editor.SyntaxHighlighting = MarkdownHighlighting.GetDefinition(_theme);
        editor.Background = GetThemeBrush("EditorBackground", _theme == "Dark" ? "#1E1E1E" : "#FFFFFF");
        editor.Foreground = GetThemeBrush("EditorForeground", _theme == "Dark" ? "#D4D4D4" : "#1E1E1E");
        editor.LineNumbersForeground = GetThemeBrush("LineNumberForeground", "#858585");
    }

    private static Brush GetThemeBrush(string key, string fallback)
    {
        if (Application.Current.Resources[key] is Brush brush)
            return brush;
        return (Brush)new BrushConverter().ConvertFromString(fallback)!;
    }

    // ---------- Keyboard shortcuts the editor does not own ----------

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        switch (e.Key)
        {
            case Key.N:
                NewTab();
                e.Handled = true;
                break;
            case Key.O:
                OpenFile();
                e.Handled = true;
                break;
            case Key.S:
                if (shift)
                    SaveAsFile();
                else
                    SaveFile();
                e.Handled = true;
                break;
            case Key.W:
                CloseTab(ActiveTab);
                e.Handled = true;
                break;
        }
    }

    // ---------- Git menu (git CLI; your existing git auth, no new login) ----------

    private async Task<string?> GetRepoRootForActiveTabAsync()
    {
        var tab = ActiveTab;
        var dir = tab?.Document.Directory ?? _settings.LastFolder;
        if (string.IsNullOrEmpty(dir))
            return null;
        return await GitService.GetRepoRootAsync(dir);
    }

    private void SetGitEnabled(bool enabled)
    {
        CommitMenuItem.IsEnabled = enabled;
        PushMenuItem.IsEnabled = enabled;
        PullMenuItem.IsEnabled = enabled;
    }

    private async Task RefreshGitStatusAsync()
    {
        var tab = ActiveTab;
        var dir = tab?.Document.Directory ?? _settings.LastFolder;
        if (string.IsNullOrEmpty(dir))
        {
            BranchText.Text = "No file";
            GitStatusText.Text = "";
            SetGitEnabled(false);
            return;
        }

        var root = await GitService.GetRepoRootAsync(dir);
        if (root == null)
        {
            BranchText.Text = "Not a git repo";
            GitStatusText.Text = "";
            SetGitEnabled(false);
            return;
        }

        SetGitEnabled(true);
        var branch = await GitService.GetBranchAsync(root);
        var entries = await GitService.GetStatusAsync(root);
        BranchText.Text = string.IsNullOrEmpty(branch) ? "(detached)" : branch;
        GitStatusText.Text = entries.Count == 0 ? "clean" : $"{entries.Count} changed";
    }

    private void GitRefresh_Click(object sender, RoutedEventArgs e) => _ = RefreshGitStatusAsync();

    private async void GitCommit_Click(object sender, RoutedEventArgs e)
    {
        var root = await GetRepoRootForActiveTabAsync();
        if (root == null)
        {
            MessageBox.Show(this, "The current file is not inside a git repository.", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var entries = await GitService.GetStatusAsync(root);
        if (entries.Count == 0)
        {
            MessageBox.Show(this, "Nothing to commit.", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dlg = new CommitDialog(entries) { Owner = this };
        if (dlg.ShowDialog() != true)
            return;

        var stage = await GitService.StageAsync(root, dlg.SelectedFiles);
        if (!stage.Success)
        {
            MessageBox.Show(this, $"git add failed:\n{stage.Stderr}", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var commit = await GitService.CommitAsync(root, dlg.CommitMessage);
        if (!commit.Success)
        {
            MessageBox.Show(this, $"git commit failed:\n{commit.Stderr}", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await RefreshGitStatusAsync();
    }

    private async void GitPush_Click(object sender, RoutedEventArgs e)
    {
        var root = await GetRepoRootForActiveTabAsync();
        if (root == null)
            return;

        GitStatusText.Text = "Pushing…";
        var result = await GitService.PushAsync(root);
        if (!result.Success)
        {
            MessageBox.Show(this, $"git push failed:\n{result.Stderr}", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        await RefreshGitStatusAsync();
    }

    private async void GitPull_Click(object sender, RoutedEventArgs e)
    {
        var root = await GetRepoRootForActiveTabAsync();
        if (root == null)
            return;

        GitStatusText.Text = "Pulling…";
        var result = await GitService.PullAsync(root);
        if (!result.Success)
        {
            MessageBox.Show(this, $"git pull failed:\n{result.Stderr}", "MarkPad",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        await RefreshGitStatusAsync();
    }

    // ---------- Help ----------

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            "MarkPad — a minimal markdown editor.\n\nSingle-pane editing, dark mode, and git commit/push/pull through your own git install.\n\nFree and open source (MIT).",
            "About MarkPad", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
