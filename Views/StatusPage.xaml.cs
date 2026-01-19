using SupStick.ViewModels;

namespace SupStick.Views;

public partial class StatusPage : ContentPage
{
	public StatusPage(StatusViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
