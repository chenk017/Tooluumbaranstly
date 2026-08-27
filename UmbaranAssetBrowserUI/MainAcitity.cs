using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace UmbaranAssetBrowserUI;

[Activity(
    Label = "Umbaran Asset Browser",
    Theme = "@style/MyTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.Orientation |
        ConfigChanges.ScreenSize |
        ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
}
