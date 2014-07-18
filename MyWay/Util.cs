using Microsoft.Phone.Net.NetworkInformation;
using System;
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
