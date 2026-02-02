using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace POS.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(IServiceProvider serviceProvider) : this()
    {
        blazorWebView.Services = serviceProvider;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (this.WindowState != WindowState.Minimized)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}
