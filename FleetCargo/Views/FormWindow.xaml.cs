using System.Windows;
using System.Windows.Controls;

namespace FleetCargo.Views;

/// <summary>Універсальне модальне вікно для форм додавання та редагування.</summary>
public partial class FormWindow : Window
{
    public FormWindow(string title, UserControl form)
    {
        InitializeComponent();
        Title = title;
        Host.Content = form;
        MaxHeight = SystemParameters.WorkArea.Height * 0.95;
    }
}
