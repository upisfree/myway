using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Animation;

namespace MyWay
{
  public static class Util
  {
    public static bool IsInternetAvailable()
    {
      var ni = NetworkInterface.NetworkInterfaceType;

      bool isConnected = false;

      if ((ni == NetworkInterfaceType.Wireless80211) || (ni == NetworkInterfaceType.MobileBroadbandCdma) || (ni == NetworkInterfaceType.MobileBroadbandGsm))
        isConnected = true;
      else if (ni == NetworkInterfaceType.None)
        isConnected = false;

      return isConnected;
    }

    public static bool IsStringContains(this string source, string toCheck) // case insensitive
    {
      return source.IndexOf(toCheck, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static string TypographString(string s)
    {
      s = Regex.Replace(s, "\"([а-яА-Я0-9\\s\\-]+)\"", "«$1»"); // "слово" -> «слово»
      s = Regex.Replace(s, "([^0-9]|-[0-9])-((?!([а-я0-9]|Омск|Восточное))|[а-я]\\.)", "$1 — $2"); // текст-текст -> текст — текст
      s = Regex.Replace(s, " - ", " — "); // текст - текст -> текст — текст

      return s;
    }

    public static void Show(UIElement e)
    {
      e.Visibility = Visibility.Visible;
    }

    public static void Hide(UIElement e)
    {
      e.Visibility = Visibility.Collapsed;
    }

    public static void Animation(DependencyObject target, PropertyPath property, DoubleAnimation animation)
    {
      Storyboard sb = new Storyboard();

      Storyboard.SetTarget(animation, target);
      Storyboard.SetTargetProperty(animation, property);

      sb.Children.Add(animation);

      sb.Begin();
    }
  }
}
