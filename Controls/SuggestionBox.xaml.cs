using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlateBilling.Controls;

/// <summary>
/// A text box that suggests matching items while the user types,
/// like a search box. Items starting with the typed text are listed
/// first, followed by items that contain it.
///
/// Keys:
///   Down / Up   move through the suggestions (Down opens the full list)
///   Enter / Tab pick the highlighted suggestion
///   Escape      close the suggestions
/// </summary>
public partial class SuggestionBox : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(SuggestionBox));

    public static readonly DependencyProperty DisplayMemberPathProperty =
        DependencyProperty.Register(
            nameof(DisplayMemberPath),
            typeof(string),
            typeof(SuggestionBox),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(SuggestionBox),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedItemChanged));

    // Set while the text is changed from code, so it is not
    // treated as the user typing.
    private bool _settingText;

    // Set while the selection is cleared because the user typed,
    // so the typed text is not replaced.
    private bool _clearingSelection;

    public SuggestionBox()
    {
        InitializeComponent();

        Unloaded += (_, _) => ClosePopup();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }


    // =========================================================
    // PUBLIC API
    // =========================================================

    public void FocusInput()
    {
        InputBox.Focus();
    }

    /// <summary>
    /// Selects the highlighted suggestion, or an item whose text
    /// matches exactly, and closes the suggestions.
    /// </summary>
    public void CommitSuggestion()
    {
        object? item = SuggestionPopup.IsOpen
            ? SuggestionList.SelectedItem
            : null;

        item ??= FindExactMatch(InputBox.Text);

        if (item != null)
        {
            Select(item);
        }

        ClosePopup();
    }


    // =========================================================
    // SELECTION
    // =========================================================

    private static void OnSelectedItemChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var box = (SuggestionBox)d;

        if (box._clearingSelection)
        {
            return;
        }

        // Selection set from outside (view model): show its text.
        box.SetText(
            e.NewValue == null
                ? string.Empty
                : box.GetDisplayText(e.NewValue));

        box.ClosePopup();
    }

    private void Select(object item)
    {
        string text = GetDisplayText(item);

        // Re-picking the current entry must not raise a change,
        // otherwise the view model would recalculate needlessly.
        bool alreadySelected =
            SelectedItem != null &&
            GetDisplayText(SelectedItem) == text;

        if (!alreadySelected)
        {
            SetCurrentValue(SelectedItemProperty, item);
        }

        SetText(text);
    }


    // =========================================================
    // TYPING
    // =========================================================

    private void InputBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_settingText)
        {
            return;
        }

        // Editing the text of a selected item starts a new search.
        if (SelectedItem != null &&
            GetDisplayText(SelectedItem) != InputBox.Text)
        {
            _clearingSelection = true;
            SetCurrentValue(SelectedItemProperty, null);
            _clearingSelection = false;
        }

        string query = InputBox.Text.Trim();

        if (query.Length == 0)
        {
            ClosePopup();
            return;
        }

        ShowSuggestions(query);
    }

    private void InputBox_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                if (SuggestionPopup.IsOpen)
                {
                    MoveHighlight(1);
                }
                else
                {
                    // Browse: show everything when the field holds a
                    // picked item, otherwise filter by the typed text.
                    ShowSuggestions(
                        SelectedItem != null
                            ? string.Empty
                            : InputBox.Text.Trim());
                }

                e.Handled = true;
                break;

            case Key.Up:
                if (SuggestionPopup.IsOpen)
                {
                    MoveHighlight(-1);
                    e.Handled = true;
                }
                break;

            case Key.Escape:
                if (SuggestionPopup.IsOpen)
                {
                    ClosePopup();
                    e.Handled = true;
                }
                break;

            case Key.Enter:
            case Key.Tab:
                // Moving focus is left to the host view.
                CommitSuggestion();
                break;
        }
    }

    private void InputBox_LostKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        // Leaving the field (e.g. by mouse) keeps only an exact match.
        // Partial text stays as typed so validation can flag it.
        object? match = FindExactMatch(InputBox.Text);

        if (match != null)
        {
            Select(match);
        }

        ClosePopup();
    }

    private void SuggestionList_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (ItemsControl.ContainerFromElement(
                SuggestionList,
                (DependencyObject)e.OriginalSource) is not ListBoxItem container)
        {
            return;
        }

        Select(container.DataContext);
        ClosePopup();

        InputBox.Focus();

        e.Handled = true;
    }


    // =========================================================
    // SUGGESTIONS
    // =========================================================

    private void ShowSuggestions(string query)
    {
        var matches = GetItems()
            .Select(item => (Item: item, Text: GetDisplayText(item)))
            .Where(x => x.Text.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Text.StartsWith(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ToList();

        if (matches.Count == 0)
        {
            ClosePopup();
            return;
        }

        // Highlight an exact match, else the current selection,
        // else the best (first) suggestion.
        string selectedText = SelectedItem == null
            ? string.Empty
            : GetDisplayText(SelectedItem);

        int highlight = matches.FindIndex(x =>
            x.Text.Equals(query, StringComparison.OrdinalIgnoreCase));

        if (highlight < 0)
        {
            highlight = matches.FindIndex(x => x.Text == selectedText);
        }

        SuggestionList.ItemsSource = matches.Select(x => x.Item).ToList();
        SuggestionList.SelectedIndex = Math.Max(highlight, 0);
        SuggestionList.ScrollIntoView(SuggestionList.SelectedItem);

        SuggestionPopup.IsOpen = true;
    }

    private void MoveHighlight(int step)
    {
        int index = Math.Clamp(
            SuggestionList.SelectedIndex + step,
            0,
            SuggestionList.Items.Count - 1);

        SuggestionList.SelectedIndex = index;
        SuggestionList.ScrollIntoView(SuggestionList.SelectedItem);
    }

    private void ClosePopup()
    {
        SuggestionPopup.IsOpen = false;
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private IEnumerable<object> GetItems()
    {
        return ItemsSource?.Cast<object>() ?? [];
    }

    private object? FindExactMatch(string text)
    {
        string query = text.Trim();

        if (query.Length == 0)
        {
            return null;
        }

        return GetItems().FirstOrDefault(item =>
            GetDisplayText(item).Trim().Equals(
                query,
                StringComparison.OrdinalIgnoreCase));
    }

    private string GetDisplayText(object item)
    {
        if (string.IsNullOrEmpty(DisplayMemberPath))
        {
            return item.ToString() ?? string.Empty;
        }

        return item.GetType()
            .GetProperty(DisplayMemberPath)?
            .GetValue(item)?
            .ToString() ?? string.Empty;
    }

    private void SetText(string text)
    {
        _settingText = true;

        InputBox.Text = text;
        InputBox.CaretIndex = text.Length;

        _settingText = false;
    }
}
