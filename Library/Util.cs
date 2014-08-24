﻿using System;
using System.Text.RegularExpressions;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace Library
{
  public static class Util
  {
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

    public static Color ConvertStringToColor(String hex)
    {
      // remove the # at the front
      hex = hex.Replace("#", "");

      byte a = 255;
      byte r = 255;
      byte g = 255;
      byte b = 255;

      int start = 0;

      // handle ARGB strings (8 characters long)
      if (hex.Length == 8)
      {
        a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        start = 2;
      }

      // convert RGB characters to bytes
      r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
      g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
      b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

      return Color.FromArgb(a, r, g, b);
    }

    public static void Show(UIElement e)
    {
      e.Visibility = Visibility.Visible;
    }

    public static void Hide(UIElement e)
    {
      e.Visibility = Visibility.Collapsed;
    }

    /*****************************************
     Анимации 
    *****************************************/

    public static void DoubleAnimation(DependencyObject target, string property, DoubleAnimation animation) // TODO: объединить
    {
      Storyboard sb = new Storyboard();

      Storyboard.SetTarget(animation, target);
      Storyboard.SetTargetProperty(animation, property);

      sb.Children.Add(animation);

      sb.Begin();
    }

    public static void ColorAnimation(DependencyObject target, string property, ColorAnimation animation)
    {
      Storyboard sb = new Storyboard();

      Storyboard.SetTarget(animation, target);
      Storyboard.SetTargetProperty(animation, property);

      sb.Children.Add(animation);

      sb.Begin();
    }
  }
}