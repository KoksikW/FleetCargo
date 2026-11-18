using System.Windows;
using FleetCargo.ViewModels;

namespace FleetCargo.Views;

/// <summary>Модальне вікно з відомостями про автора проєкту.</summary>
public partial class AuthorWindow : Window
{
    public AuthorWindow(AuthorViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
