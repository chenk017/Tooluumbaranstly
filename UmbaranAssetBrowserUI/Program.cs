using Avalonia;
using Avalonia.Android;

namespace UmbaranAssetBrowserUI;

public static class Program
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder
            .Configure<App>()
            .UseAndroid()
            .LogToTrace();
    }
}
