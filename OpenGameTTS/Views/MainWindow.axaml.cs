using Avalonia.Controls;
using OpenGameTTS.ViewModels;

namespace OpenGameTTS.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainViewModel.SetWindowInstance(this);
        Closing += (_, _) => (DataContext as MainViewModel)?.Dispose();
    }
}
