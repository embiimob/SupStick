using Microsoft.Extensions.Logging;
using SupStick.Services;
using SupStick.ViewModels;
using SupStick.Views;

namespace SupStick;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register Services
		builder.Services.AddSingleton<IBitcoinService, BitcoinService>();
		builder.Services.AddSingleton<IIpfsService, IpfsService>();
		builder.Services.AddSingleton<IP2FKService, P2FKService>();
		builder.Services.AddSingleton<IDataStorageService, DataStorageService>();
		builder.Services.AddSingleton<ITransactionMonitorService, TransactionMonitorService>();
		builder.Services.AddSingleton<IMediaPlayerService, MediaPlayerService>();

		// Register ViewModels
		builder.Services.AddTransient<StatusViewModel>();
		builder.Services.AddTransient<SearchViewModel>();
		builder.Services.AddTransient<SetupViewModel>();
		builder.Services.AddTransient<MediaPlayerViewModel>();

		// Register Views
		builder.Services.AddTransient<StatusPage>();
		builder.Services.AddTransient<SearchPage>();
		builder.Services.AddTransient<SetupPage>();
		builder.Services.AddTransient<MediaPlayerPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
