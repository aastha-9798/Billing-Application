using PlateBilling.Models;
using PlateBilling.ViewModels;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace PlateBilling.Views;

public partial class ChallanView : UserControl
{
    private TextBox? _clientTextBox;
    private TextBox? _plateTypeTextBox;

    private bool _updatingSearch;

    public ChallanView()
    {
        InitializeComponent();

        DataContext = new ChallanViewModel();

        ClientInput.Loaded += ClientInput_Loaded;
        PlateTypeInput.Loaded += PlateTypeInput_Loaded;

        ClientInput.PreviewMouseLeftButtonDown +=
            ClientInput_PreviewMouseLeftButtonDown;

        PlateTypeInput.PreviewMouseLeftButtonDown +=
            PlateTypeInput_PreviewMouseLeftButtonDown;

        ClientInput.GotKeyboardFocus += ClientInput_GotKeyboardFocus;
        PlateTypeInput.GotKeyboardFocus += PlateTypeInput_GotKeyboardFocus;
    }

    // =========================================================
    // CONNECT TO INTERNAL EDITABLE TEXTBOX
    // =========================================================

    private void ClientInput_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _clientTextBox =
            ClientInput.Template.FindName(
                "PART_EditableTextBox",
                ClientInput) as TextBox;

        if (_clientTextBox != null)
        {
            _clientTextBox.TextChanged +=
                ClientTextBox_TextChanged;
        }
    }

    private void PlateTypeInput_Loaded(
        object sender,
        RoutedEventArgs e)
    {
        _plateTypeTextBox =
            PlateTypeInput.Template.FindName(
                "PART_EditableTextBox",
                PlateTypeInput) as TextBox;

        if (_plateTypeTextBox != null)
        {
            _plateTypeTextBox.TextChanged +=
                PlateTypeTextBox_TextChanged;
        }
    }

    // =========================================================
    // SELECT ALL TEXT ON FOCUS (MOUSE OR KEYBOARD)
    // =========================================================

    private void ClientInput_GotKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        if (_clientTextBox != null)
        {
            _clientTextBox.Dispatcher.InvokeAsync(
                () => _clientTextBox.SelectAll(),
                System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private void PlateTypeInput_GotKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        if (_plateTypeTextBox != null)
        {
            _plateTypeTextBox.Dispatcher.InvokeAsync(
                () => _plateTypeTextBox.SelectAll(),
                System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    // =========================================================
    // SELECT EXISTING CLIENT TEXT WHEN USER CLICKS FIELD
    // =========================================================

    private void ClientInput_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_clientTextBox == null)
        {
            return;
        }

        if (!_clientTextBox.IsKeyboardFocusWithin)
        {
            // Let focus happen naturally; GotKeyboardFocus will SelectAll.
            ClientInput.Focus();
            e.Handled = true;
        }
    }

    // =========================================================
    // SELECT EXISTING PLATE TYPE TEXT WHEN USER CLICKS FIELD
    // =========================================================

    private void PlateTypeInput_PreviewMouseLeftButtonDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_plateTypeTextBox == null)
        {
            return;
        }

        if (!_plateTypeTextBox.IsKeyboardFocusWithin)
        {
            PlateTypeInput.Focus();
            e.Handled = true;
        }
    }

    // =========================================================
    // CLIENT PREFIX SEARCH
    // =========================================================

    private void ClientTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_updatingSearch)
        {
            return;
        }

        if (_clientTextBox == null)
        {
            return;
        }

        // If user starts typing while an item is selected, clear the
        // selection so the field acts as a fresh search box.
        if (ClientInput.SelectedItem != null)
        {
            _updatingSearch = true;
            ClientInput.SelectedItem = null;
            _updatingSearch = false;
        }

        string text = _clientTextBox.Text;

        FilterClients(text);
    }

    private void FilterClients(string text)
    {
        if (DataContext is not ChallanViewModel viewModel)
        {
            return;
        }

        ICollectionView view =
            CollectionViewSource.GetDefaultView(
                viewModel.Clients);

        string searchText = text.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            view.Filter = null;
        }
        else
        {
            view.Filter = item =>
            {
                if (item is not Client client)
                {
                    return false;
                }

                return client.Name.StartsWith(
                    searchText,
                    StringComparison.OrdinalIgnoreCase);
            };
        }

        _updatingSearch = true;
        view.Refresh();
        _updatingSearch = false;

        // Refresh() causes WPF's ComboBox internals to call SelectAll()
        // asynchronously. Defer the caret restore so it runs after that.
        if (_clientTextBox != null)
        {
            var tb = _clientTextBox;
            int caretPos = tb.Text.Length;
            tb.Dispatcher.InvokeAsync(() =>
            {
                tb.SelectionStart = caretPos;
                tb.SelectionLength = 0;
            }, System.Windows.Threading.DispatcherPriority.Input);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            ClientInput.IsDropDownOpen = true;
        }
    }

    // =========================================================
    // PLATE TYPE PREFIX SEARCH
    // =========================================================

    private void PlateTypeTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_updatingSearch)
        {
            return;
        }

        if (_plateTypeTextBox == null)
        {
            return;
        }

        // If user starts typing while an item is selected, clear the
        // selection so the field acts as a fresh search box.
        if (PlateTypeInput.SelectedItem != null)
        {
            _updatingSearch = true;
            PlateTypeInput.SelectedItem = null;
            _updatingSearch = false;
        }

        string text = _plateTypeTextBox.Text;

        FilterPlateTypes(text);
    }

    private void FilterPlateTypes(string text)
    {
        if (DataContext is not ChallanViewModel viewModel)
        {
            return;
        }

        ICollectionView view =
            CollectionViewSource.GetDefaultView(
                viewModel.PlateTypes);

        string searchText = text.Trim();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            view.Filter = null;
        }
        else
        {
            view.Filter = item =>
            {
                if (item is not PlateType plateType)
                {
                    return false;
                }

                return plateType.Code.StartsWith(
                    searchText,
                    StringComparison.OrdinalIgnoreCase);
            };
        }

        _updatingSearch = true;
        view.Refresh();
        _updatingSearch = false;

        // Refresh() causes WPF's ComboBox internals to call SelectAll()
        // asynchronously. Defer the caret restore so it runs after that.
        if (_plateTypeTextBox != null)
        {
            var tb = _plateTypeTextBox;
            int caretPos = tb.Text.Length;
            tb.Dispatcher.InvokeAsync(() =>
            {
                tb.SelectionStart = caretPos;
                tb.SelectionLength = 0;
            }, System.Windows.Threading.DispatcherPriority.Input);
        }

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            PlateTypeInput.IsDropDownOpen = true;
        }
    }

    // =========================================================
    // COMMIT CLIENT INPUT
    // =========================================================

    private void CommitClientInput()
    {
        if (DataContext is not ChallanViewModel viewModel)
        {
            return;
        }

        string text =
            _clientTextBox?.Text.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        Client? client =
            viewModel.Clients.FirstOrDefault(
                c => string.Equals(
                    c.Name.Trim(),
                    text,
                    StringComparison.OrdinalIgnoreCase));

        if (client == null)
        {
            return;
        }

        _updatingSearch = true;

        ClientInput.SelectedItem = client;

        if (_clientTextBox != null)
        {
            _clientTextBox.Text = client.Name;

            _clientTextBox.SelectionStart =
                _clientTextBox.Text.Length;

            _clientTextBox.SelectionLength = 0;
        }

        _updatingSearch = false;

        ClearClientFilter();
    }

    // =========================================================
    // COMMIT PLATE TYPE INPUT
    // =========================================================

    private void CommitPlateTypeInput()
    {
        if (DataContext is not ChallanViewModel viewModel)
        {
            return;
        }

        string text =
            _plateTypeTextBox?.Text.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        PlateType? plateType =
            viewModel.PlateTypes.FirstOrDefault(
                p => string.Equals(
                    p.Code.Trim(),
                    text,
                    StringComparison.OrdinalIgnoreCase));

        if (plateType == null)
        {
            return;
        }

        _updatingSearch = true;

        PlateTypeInput.SelectedItem = plateType;

        if (_plateTypeTextBox != null)
        {
            _plateTypeTextBox.Text = plateType.Code;

            _plateTypeTextBox.SelectionStart =
                _plateTypeTextBox.Text.Length;

            _plateTypeTextBox.SelectionLength = 0;
        }

        _updatingSearch = false;

        ClearPlateTypeFilter();
    }

    // =========================================================
    // CLEAR CLIENT FILTER
    // =========================================================

    private void ClearClientFilter()
    {
        if (DataContext is not ChallanViewModel viewModel)
        {
            return;
        }

        ICollectionView view =
            CollectionViewSource.GetDefaultView(
                viewModel.Clients);

        view.Filter = null;
        view.Refresh();

        ClientInput.IsDropDownOpen = false;
    }

    // =========================================================
    // CLEAR PLATE TYPE FILTER
    // =========================================================

    private void ClearPlateTypeFilter()
    {
        if (DataContext is not ChallanViewModel viewModel)
        {
            return;
        }

        ICollectionView view =
            CollectionViewSource.GetDefaultView(
                viewModel.PlateTypes);

        view.Filter = null;
        view.Refresh();

        PlateTypeInput.IsDropDownOpen = false;
    }

    // =========================================================
    // KEYBOARD NAVIGATION
    // =========================================================

    private void ChallanView_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        // -----------------------------------------------------
        // CLIENT
        // -----------------------------------------------------

        if (ClientInput.IsKeyboardFocusWithin)
        {
            if (e.Key == Key.Enter)
            {
                CommitClientInput();

                MoveFocusTo(ChallanInput);

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab)
            {
                CommitClientInput();
                return;
            }
        }

        // -----------------------------------------------------
        // PLATE TYPE
        // -----------------------------------------------------

        if (PlateTypeInput.IsKeyboardFocusWithin)
        {
            if (e.Key == Key.Enter)
            {
                CommitPlateTypeInput();

                MoveFocusTo(QuantityInput);

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Tab)
            {
                CommitPlateTypeInput();
                return;
            }
        }

        // -----------------------------------------------------
        // OTHER ENTER NAVIGATION
        // -----------------------------------------------------

        if (e.Key != Key.Enter)
        {
            return;
        }

        // -----------------------------------------------------
        // DATE
        // -----------------------------------------------------

        if (DateInput.IsKeyboardFocusWithin)
        {
            MoveFocusTo(ClientInput);

            e.Handled = true;
            return;
        }

        // -----------------------------------------------------
        // TEXTBOXES
        // -----------------------------------------------------

        if (e.OriginalSource is TextBox textBox)
        {
            if (textBox == ChallanInput)
            {
                MoveFocusTo(DescriptionInput);
            }
            else if (textBox == DescriptionInput)
            {
                MoveFocusTo(PlateTypeInput);
            }
            else if (textBox == QuantityInput)
            {
                MoveFocusTo(AreaBillingInput);
            }

            e.Handled = true;
            return;
        }

        // -----------------------------------------------------
        // LAST FIELD
        // -----------------------------------------------------

        if (e.OriginalSource is CheckBox checkBox &&
            checkBox == AreaBillingInput)
        {
            SaveChallan();

            e.Handled = true;
        }
    }

    // =========================================================
    // MOVE FOCUS
    // =========================================================

    private void MoveFocusTo(Control control)
    {
        control.Focus();

        if (control is TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    // =========================================================
    // SAVE / UPDATE
    // =========================================================

    private void SaveChallan()
    {
        if (DataContext is ChallanViewModel viewModel)
        {
            if (viewModel.SaveChallanCommand.CanExecute(null))
            {
                viewModel.SaveChallanCommand.Execute(null);
            }
        }
    }
    // =========================================================
    // Challan input preview
    // =========================================================
    private void ChallanInput_PreviewTextInput(
    object sender,
    TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }
    // =========================================================
    // Challan input pasting prevent
    // =========================================================
    private void ChallanInput_Pasting(
    object sender,
    DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }

        string text =
            (string)e.DataObject.GetData(typeof(string))!;

        if (!text.All(char.IsDigit))
        {
            e.CancelCommand();
        }
    }

    // =========================================================
    // Challan input increment / decrement with up/down keys
    // =========================================================
    private void ChallanInput_PreviewKeyDown(
    object sender,
    KeyEventArgs e)
    {
        if (DataContext is not ChallanViewModel viewModel)
            return;

        if (e.Key == Key.Up)
        {
            if (viewModel.ChallanNo == null)
            {
                viewModel.ChallanNo = 1;
            }
            else
            {
                viewModel.ChallanNo++;
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (viewModel.ChallanNo != null &&
                viewModel.ChallanNo > 1)
            {
                viewModel.ChallanNo--;
            }

            // If empty or already 1, do nothing.
            e.Handled = true;
        }
    }
}