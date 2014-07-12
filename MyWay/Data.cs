﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Windows.Storage;
using Windows.Storage.Streams;

namespace MyWay
{
  class Data
  {
    private static StorageFolder storage = ApplicationData.Current.LocalFolder;

    public class File
    {
      public static async Task Write(string path, string text)
      {
        path = Regex.Replace(path, "/", "\\");

        StorageFile file = await storage.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

        await FileIO.AppendTextAsync(file, text + "\n");
      }

      public static async Task<string> Read(string path)
      {
        path = Regex.Replace(path, "/", "\\");

        if (File.IsExists(path) == true)
        {
          StorageFile file = await storage.GetFileAsync(path);

          return await FileIO.ReadTextAsync(file);
        }
        else
          return "";
      }

      public static async Task Delete(string path)
      {
        path = Regex.Replace(path, "/", "\\");

        if (File.IsExists(path) == true)
        {
          StorageFile file = await storage.GetFileAsync(path);

          await file.DeleteAsync();
        }
      }

      public static bool IsExists(string path)
      {
        path = Regex.Replace(path, "/", "\\");
        path = Path.Combine(storage.Path, path);

        return System.IO.File.Exists(path);
      }
    }

    public class Folder
    {
      public static async Task Create(string name)
      {
        name = Regex.Replace(name, "/", "\\");

        await storage.CreateFolderAsync(name, CreationCollisionOption.OpenIfExists);
      }

      public static async Task<bool> IsExists(string name)
      {
        name = Regex.Replace(name, "/", "\\");

        try
        {
          await storage.GetFolderAsync(name);

          return true;
        }
        catch
        {
          return false;
        }
      }
    }

    public static async Task Clear()
    {
      var files   = await storage.GetFilesAsync();
      var folders = await storage.GetFoldersAsync();

      foreach (var file in files)
        await file.DeleteAsync();

      foreach (var folder in folders)
        await folder.DeleteAsync();
    }
  }
}