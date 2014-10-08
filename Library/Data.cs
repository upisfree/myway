using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;

namespace Library
{
  public class Data
  {
    private static StorageFolder storage = ApplicationData.Current.LocalFolder;

    public class File
    {
      public static async Task Write(string path, string text, bool isTypography = true)
      {
        path = Regex.Replace(path, "/", "\\");

        StorageFile file = await storage.CreateFileAsync(path, CreationCollisionOption.OpenIfExists);

        if (isTypography)
        {
          text = Util.TypographString(text);
        }

        await FileIO.AppendTextAsync(file, text + "\n");
      }

      public static async Task<string> Read(string path)
      {
        path = Regex.Replace(path, "/", "\\");

        if (await File.IsExists(path) == true)
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

        if (await File.IsExists(path) == true)
        {
          StorageFile file = await storage.GetFileAsync(path);

          await file.DeleteAsync();
        }
      }

      public static async Task<bool> IsExists(string path)
      {
        path = Regex.Replace(path, "/", "\\");

        try
        {
          await storage.GetFileAsync(path);
		      return true;
	      }
        catch
        {
		      return false;
	      }
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
      var files = await storage.GetFilesAsync();
      var folders = await storage.GetFoldersAsync();

      foreach (var file in files)
        await file.DeleteAsync();

      foreach (var folder in folders)
        await folder.DeleteAsync();
    }
  }
}
