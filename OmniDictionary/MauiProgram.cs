using Microsoft.Extensions.Logging;

namespace OmniDictionary
{
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
                    fonts.AddFont("Bookerly.ttf", "Bookerly");
                    fonts.AddFont("IBMPlexSans-Regular.ttf", "IBMPlexSans");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);


            return builder.Build();
        }
    }
}
