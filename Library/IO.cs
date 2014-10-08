using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Library
{
  public class IO
  {
    public static class MainPage
    {
      private async static Task<HtmlDocument> Download(string mode)
      {
        if (Library.Util.IsInternetAvailable())
        {
          string link = "";
          switch (mode)
          {
            case "Routes":
              link = "http://t.bus55.ru/index.php/app/get_routes";
              break;
            case "Stops_Map":
              link = "http://t.bus55.ru/index.php/app/get_stations_json";
              break;
          }

          string htmlPage = "";

          try
          {
            htmlPage = await new HttpClient().GetStringAsync(link);
          }
          catch
          {
            return null;
          }

          HtmlDocument htmlDocument = new HtmlDocument();
          htmlDocument.LoadHtml(htmlPage);

          return htmlDocument;
        }
        else
          return null;
      }

      private async static Task<string[]> WriteAndGet(string mode, HtmlDocument html)
      {
        if (html == null)
          return null;

        string[] result = null;

        switch (mode)
        {
          case "Routes":
            List<string> s = new List<string>();
            string d = null;

            foreach (var a in html.DocumentNode.QuerySelectorAll("a"))
            {
              string number = a.ChildNodes.ToArray()[0].InnerText.Trim();
              string type = a.QuerySelector("span").InnerText.Trim();
              string desc = a.QuerySelector("div").InnerText.Trim();
              string toStop = a.Attributes["href"].Value + "|" + number + " " + type;

              string c = number + "|" + type + "|" + desc + "|" + a.Attributes["href"].Value;

              s.Add(c);

              d += c + "\n";
            }

            d = d.Substring(0, d.Length - 1); // символ перевода строки один, а не два (\n)
            await Library.Data.File.Write("Routes.db", d);

            result = s.ToArray();

            break;
          case "Stops_Map":
            string jsonText = html.DocumentNode.InnerText;

            List<Model.Stop.Map> json = JsonConvert.DeserializeObject<List<Model.Stop.Map>>(jsonText);

            string[] r = new string[json.Count - 1];
            string w = null;

            for (int i = 0; i < json.Count - 1; i++)
            {
              Model.Stop.Map a = json[i];

              string c = a.Id + "|" + a.Lat + "|" + a.Lon + "|" + a.Name;

              r[i] = c;

              w += c + "\n";
            }

            w = w.Substring(0, w.Length - 1);

            await Library.Data.File.Write("Stops_Map.db", w);

            result = r;

            break;

          case "Stops_List":
            string[] e = null;

            try
            {
              e = await Get("Stops_Map"); // проверка на отсутсвие интернета, так как основная не проходит (смотри проверку в начале функции)
              int _e = e.Length;            // чтобы try catch поймал исключение, надо произвести какое-либо действие над «e»
            }
            catch
            {
              break; // ловим исключение? валим отсюда.
            }

            List<Model.Stop.List> f = new List<Model.Stop.List>();
            List<string> j = new List<string>();
            string k = null;

            Model.Stop.List_Comparer mc = new Model.Stop.List_Comparer();

            foreach (string g in e)
            {
              try
              {
                string[] line = g.Split(new Char[] { '|' });

                string name = line[3];
                string link = "http://t.bus55.ru/index.php/app/get_dir/" + line[0];

                Model.Stop.List i = new Model.Stop.List() { Name = name, Link = link };

                if (!f.Contains(i, mc))
                {
                  f.Add(i);
                  j.Add(name + "|" + link);
                  k += name + "|" + link + "\n";
                }
              }
              catch { }
            }

            k = k.Substring(0, k.Length - 1);

            await Library.Data.File.Write("Stops_List.db", k);

            result = j.ToArray();

            break;
        }

        return result;
      }

      public async static Task<string[]> Get(string mode)
      {
        if (await Library.Data.File.IsExists(mode + ".db"))
        {
          string a = await Library.Data.File.Read(mode + ".db");
          return a.Split(new Char[] { '\n' });
        }
        else
        {
          if (mode != "Stops_List")
          {
            HtmlDocument a = await Download(mode);
            return await WriteAndGet(mode, a);
          }
          else
            return await WriteAndGet(mode, new HtmlDocument());
        }
      }
    }
  }
}
