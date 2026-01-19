using SupStick.ViewModels;

namespace SupStick.Views;

public partial class SetupPage : ContentPage
{
	public SetupPage(SetupViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
