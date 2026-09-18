using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Graphics.Color;
using Android.Views;
using Android.Widget;
using UABEA.Android.V1.Services;

namespace UABEA.Android.V1;

[Activity(
    Label = "UABEA Android V1",
    MainLauncher = true,
    Exported = true)]
public class MainActivity : Activity
{
    private const int PickFileRequest = 1001;

    private TextView status = null!;
    private LinearLayout assetList = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        BuildUi();
    }

    private void BuildUi()
    {
        LinearLayout root =
            new LinearLayout(this);

        root.Orientation =
            Orientation.Vertical;

        root.SetPadding(
            30,
            40,
            30,
            30);

        root.SetBackgroundColor(
            Color.Black);

        TextView title =
            new TextView(this);

        title.Text =
            "UABEA Android V1";

        title.TextSize =
            24;

        title.SetTextColor(
            Color.White);

        Button openButton =
            new Button(this);

        openButton.Text =
            "OPEN UNITY3D";

        openButton.Click +=
            delegate
            {
                OpenUnityFile();
            };

        status =
            new TextView(this);

        status.Text =
            "\nNo file selected.";

        status.TextSize =
            16;

        status.SetTextColor(
            Color.White);

        ScrollView scroll =
            new ScrollView(this);

        assetList =
            new LinearLayout(this);

        assetList.Orientation =
            Orientation.Vertical;

        scroll.AddView(assetList);

        root.AddView(title);
        root.AddView(openButton);
        root.AddView(status);
        root.AddView(scroll);

        SetContentView(root);
    }

    private void OpenUnityFile()
    {
        Intent intent =
            new Intent(Intent.ActionOpenDocument);

        intent.AddCategory(
            Intent.CategoryOpenable);

        intent.SetType(
            "*/*");

        StartActivityForResult(
            intent,
            PickFileRequest);
    }

    protected override void OnActivityResult(
        int requestCode,
        Result resultCode,
        Intent? data)
    {
        base.OnActivityResult(
            requestCode,
            resultCode,
            data);

        if (requestCode != PickFileRequest)
            return;

        if (resultCode != Result.Ok ||
            data == null)
        {
            status.Text =
                "\nFile selection cancelled.";

            return;
        }

        Android.Net.Uri? uri =
            data.Data;

        if (uri == null)
        {
            status.Text =
                "\nNo file URI.";

            return;
        }

        try
        {
            string path =
                GetFilePath(uri);

            if (string.IsNullOrEmpty(path))
            {
                status.Text =
                    "\nCould not resolve file path.";

                return;
            }

            LoadUnityBundle(path);
        }
        catch (Exception ex)
        {
            status.Text =
                "\nERROR:\n" + ex.Message;
        }
    }

    private string GetFilePath(
        Android.Net.Uri uri)
    {
        string? path =
            uri.Path;

        if (!string.IsNullOrEmpty(path) &&
            System.IO.File.Exists(path))
        {
            return path;
        }

        throw new InvalidOperationException(
            "Selected file does not expose a direct filesystem path.");
    }

    private void LoadUnityBundle(
        string path)
    {
        status.Text =
            "\nLoading:\n" + path;

        assetList.RemoveAllViews();

        UnityBundleLoadResult result =
            UnityBundleLoader.Load(path);

        status.Text =
            "\nLoaded: " +
            result.CabName +
            "\nAssets: " +
            result.Assets.Count;

        foreach (UnityAssetInfo asset
            in result.Assets)
        {
            TextView item =
                new TextView(this);

            item.Text =
                asset.ToString();

            item.TextSize =
                14;

            item.SetTextColor(
                Color.White);

            item.SetPadding(
                10,
                10,
                10,
                10);

            assetList.AddView(item);
        }
    }
}
