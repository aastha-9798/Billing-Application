using PlateBilling.Controls;
using PlateBilling.Models;
using PlateBilling.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlateBilling.Views;

public partial class ChallanView : UserControl
{
    private readonly ChallanViewModel _viewModel;

    // Order in which Tab / Shift+Tab / Enter move through the entry form.
    private readonly Control[] _entryFields;

    public ChallanView()
    {
        InitializeComponent();

        _viewModel = new ChallanViewModel();
        DataContext = _viewModel;

        _viewModel.EntrySaved += ViewModel_EntrySaved;

        _entryFields =
        [
            DateInput,
            ClientInput,
            ChallanInput,
            DescriptionInput,
            PlateTypeInput,
            QuantityInput,
            AreaBillingInput
        ];

        _lastField = ClientInput;

        // Also runs when the user comes back to this screen.
        Loaded += (_, _) => FocusFieldDeferred(_lastField);
    }

    // The entry field last used, focused again when the screen is shown.
    private Control _lastField;


    // =========================================================
    // AFTER SAVE / UPDATE
    // =========================================================

    private void ViewModel_EntrySaved(object? sender, Challan challan)
    {
        ChallansGrid.ScrollIntoView(challan);

        // Ready for the next entry without touching the mouse.
        FocusFieldDeferred(ClientInput);
    }


    // =========================================================
    // KEYBOARD NAVIGATION
    //
    //   Tab          next field
    //   Shift+Tab    previous field
    //   Enter        next field; saves on the last field
    // =========================================================

    private void EntryForm_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        int index = Array.FindIndex(
            _entryFields,
            field => field.IsKeyboardFocusWithin);

        // Not in a form field, or the date picker calendar is open.
        if (index < 0 || DateInput.IsDropDownOpen)
        {
            return;
        }

        int step;

        if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
        {
            step = 1;
        }
        else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Shift)
        {
            step = -1;
        }
        else if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
        {
            step = 1;
        }
        else
        {
            return;
        }

        if (_entryFields[index] is SuggestionBox suggestionBox)
        {
            suggestionBox.CommitSuggestion();
        }

        bool isLastField = index == _entryFields.Length - 1;

        if (e.Key == Key.Enter && isLastField)
        {
            SaveEntry();

            e.Handled = true;
            return;
        }

        int target = index + step;

        // Beyond either end of the form, Tab keeps its normal
        // behaviour (e.g. Tab from the last field reaches Save).
        if (target < 0 || target >= _entryFields.Length)
        {
            return;
        }

        FocusField(_entryFields[target]);

        e.Handled = true;
    }

    // Select the whole value when a text field is entered,
    // so typing replaces it.
    private void EntryForm_GotKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        if (e.NewFocus is TextBox textBox)
        {
            Dispatcher.InvokeAsync(
                textBox.SelectAll,
                DispatcherPriority.Input);
        }

        if (e.NewFocus is DependencyObject focused &&
            _entryFields.FirstOrDefault(f => f == focused || f.IsAncestorOf(focused))
                is Control field)
        {
            _lastField = field;
        }
    }

    private static void FocusField(Control field)
    {
        switch (field)
        {
            case SuggestionBox suggestionBox:
                suggestionBox.FocusInput();
                break;

            case DatePicker datePicker
                when datePicker.Template.FindName(
                    "PART_TextBox",
                    datePicker) is TextBox dateTextBox:
                dateTextBox.Focus();
                break;

            default:
                field.Focus();
                break;
        }
    }

    private void FocusFieldDeferred(Control field)
    {
        Dispatcher.InvokeAsync(
            () => FocusField(field),
            DispatcherPriority.Input);
    }

    private void SaveEntry()
    {
        if (_viewModel.SaveChallanCommand.CanExecute(null))
        {
            _viewModel.SaveChallanCommand.Execute(null);
        }
    }


    // =========================================================
    // DATE: NEVER LEFT EMPTY
    // =========================================================

    private void DateInput_LostKeyboardFocus(
        object sender,
        KeyboardFocusChangedEventArgs e)
    {
        if (DateInput.IsKeyboardFocusWithin)
        {
            return;
        }

        // If the date text was cleared, restore the last valid date.
        Dispatcher.InvokeAsync(() =>
        {
            if (DateInput.SelectedDate == null)
            {
                DateInput.SetCurrentValue(
                    DatePicker.SelectedDateProperty,
                    _viewModel.Date);
            }
        }, DispatcherPriority.Input);
    }


    // =========================================================
    // DIGITS-ONLY INPUT (Challan No., Quantity)
    // =========================================================

    private void DigitsOnly_PreviewTextInput(
        object sender,
        TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private void DigitsOnly_Pasting(
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
    // CHALLAN NO.: UP / DOWN TO INCREMENT / DECREMENT
    // =========================================================

    private void ChallanInput_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Up)
        {
            if (_viewModel.ChallanNo == null)
            {
                _viewModel.ChallanNo = 1;
            }
            else
            {
                _viewModel.ChallanNo++;
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (_viewModel.ChallanNo != null &&
                _viewModel.ChallanNo > 1)
            {
                _viewModel.ChallanNo--;
            }

            // If empty or already 1, do nothing.
            e.Handled = true;
        }
    }
}
