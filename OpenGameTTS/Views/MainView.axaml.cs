using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using OpenGameTTS.ViewModels;

namespace OpenGameTTS.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.FocusInputRequested += (_, _) => SpeechInputTextBox.Focus();
        }
    }

    private void Border_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (IsInteractiveElement(e.Source as Visual)) return;

        if (TopLevel.GetTopLevel(this) is Window window)
        {
            window.BeginMoveDrag(e);
        }
    }

    private static bool IsInteractiveElement(Visual? source)
    {
        for (var visual = source; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is TextBox or Button or ListBox or ListBoxItem or Slider or ScrollBar)
            {
                return true;
            }
            if (visual is Border)
            {
                break;
            }
        }
        return false;
    }

    private void SpeechInputTextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                if (vm.SpeakCommand.CanExecute(null))
                {
                    vm.SpeakCommand.Execute(null);
                }
                break;
            case Key.Escape:
                e.Handled = true;
                vm.ExitAppCommand.Execute(null);
                break;
        }
    }
}
