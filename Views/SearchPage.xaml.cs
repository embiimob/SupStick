using SupStick.ViewModels;

namespace SupStick.Views;

public partial class SearchPage : ContentPage
{
	public SearchPage(SearchViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
