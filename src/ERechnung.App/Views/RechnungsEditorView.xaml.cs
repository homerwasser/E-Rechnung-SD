using System.Windows.Controls;
using ERechnung.App.ViewModels;

namespace ERechnung.App.Views;

public partial class RechnungsEditorView : UserControl
{
    public RechnungsEditorView()
    {
        InitializeComponent();
    }

    private void Validation_Error(object sender, ValidationErrorEventArgs e)
    {
        if (DataContext is RechnungsEditorViewModel viewModel)
        {
            viewModel.RegistriereEingabefehler(e.Action == ValidationErrorEventAction.Added);
        }
    }
}
