using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameTTS.Data;
using System.Collections.Generic;
using System.Data;

namespace OpenGameTTS.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _windowWidth = 350;
    [ObservableProperty]
    private int _windowHeight = 50;
    private readonly int _collapsedHeight = 50;
    private readonly int _expandedHeight = 350;

    [ObservableProperty]
    private char _collapseButtonSymbol = PhosphorIconUnicode.FinnTheHuman;
    private readonly char _collapsedSymbol = PhosphorIconUnicode.FinnTheHuman;
    private readonly char _expandedSymbol = PhosphorIconUnicode.JakeTheDog;

    [ObservableProperty]
    private bool _isCollapsed = true;

    [ObservableProperty]
    private List<string> _availableVoices = [];

    [ObservableProperty]
    private int _volume = 100;

    private static Window? _window;

    public static void SetWindowInstance(Window window)
    {
        _window = window;
    }

    [RelayCommand]
    private void ToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
        WindowHeight = WindowHeight == _expandedHeight ? _collapsedHeight : _expandedHeight;
        CollapseButtonSymbol = CollapseButtonSymbol == _expandedSymbol ? _collapsedSymbol : _expandedSymbol;
    }

    [RelayCommand]
    private static void ExitApp()
    {
        _window?.Close();
    }
}
