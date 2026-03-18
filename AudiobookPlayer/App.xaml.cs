using System;
using System.Linq;
using System.Windows;
using AudiobookPlayer.Models;

namespace AudiobookPlayer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var config = AppConfig.Load();
        ApplyTheme(config.Theme);
    }

    public static void ApplyTheme(string themeName)
    {
        var dict = new ResourceDictionary();
        switch (themeName)
        {
            case "DarkRed": dict.Source = new Uri("Themes/DarkRed.xaml", UriKind.Relative); break;
            case "LightBlack": dict.Source = new Uri("Themes/LightBlack.xaml", UriKind.Relative); break;
            default: dict.Source = new Uri("Themes/DarkGreen.xaml", UriKind.Relative); break;
        }

        var currentTheme = Current.Resources.MergedDictionaries.FirstOrDefault(m => m.Source != null && m.Source.OriginalString.StartsWith("Themes/"));
        if (currentTheme != null)
        {
            Current.Resources.MergedDictionaries.Remove(currentTheme);
        }
        
        Current.Resources.MergedDictionaries.Add(dict);
    }
}
