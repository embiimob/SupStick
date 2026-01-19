using SupStick.ViewModels;

namespace SupStick.Views;

public partial class MediaPlayerPage : ContentPage
{
	private readonly MediaPlayerViewModel _viewModel;

	public MediaPlayerPage(MediaPlayerViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		// Could initialize platform-specific media player here
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		// Could cleanup media player resources here
	}
}
