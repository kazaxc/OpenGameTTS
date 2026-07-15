using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenGameTTS.Data;
using OpenGameTTS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OpenGameTTS.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly TtsAudioService _ttsService = new();
    private readonly GlobalHotKeyService _hotKeyService = new();

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
    private string? _selectedVoice;

    [ObservableProperty]
    private string? _inputText;

    [ObservableProperty]
    private int _volume = 100;

    private static Window? _window;

    public event EventHandler? FocusInputRequested;

    public MainViewModel()
    {
        AvailableVoices = _ttsService.GetInstalledVoiceNames();
        SelectedVoice = AvailableVoices.FirstOrDefault();

        _hotKeyService.HotKeyPressed += OnGlobalHotKeyPressed;
        _hotKeyService.Start();
    }

    public static void SetWindowInstance(Window window)
    {
        _window = window;
    }

    partial void OnVolumeChanged(int value)
    {
        _ttsService.Volume = value;
    }

    partial void OnSelectedVoiceChanged(string? value)
    {
        if (value is not null)
        {
            _ttsService.SelectVoice(value);
        }
    }

    private void OnGlobalHotKeyPressed(object? sender, EventArgs e)
    {
        _window?.Activate();
        FocusInputRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task Speak()
    {
        if (string.IsNullOrWhiteSpace(InputText)) return;

        var text = InputText;
        InputText = string.Empty;
        await Task.Run(() => _ttsService.Speak(text));
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

    public void Dispose()
    {
        _hotKeyService.HotKeyPressed -= OnGlobalHotKeyPressed;
        _hotKeyService.Dispose();
        _ttsService.Dispose();
    }
}
