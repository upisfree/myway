using Microsoft.Phone.Net.NetworkInformation;
using System.Windows.Controls;

namespace MyWay
{
  class Util
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
  }
}
