using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenGameTTS.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private int _windowWidth = 350;

    [ObservableProperty]
    private int _windowHeight = 350;

    public static string Greeting => "Welcome to Avalonia!";
}
