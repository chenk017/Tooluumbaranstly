using Avalonia;
using System;

namespace UmbaranAssetBrowserUI;

class Program
{
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UseAndroid()
            .LogToTrace();
    }

    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithAndroidLifetime(args);
    }
}
