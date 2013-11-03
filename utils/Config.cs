using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace NIER2014.Utils
{
  public class Config
  {
    public string src2srcml_path { get; set; }

    public Config()
    {
      load("config.xml");
    }

    public Config(string config_file)
    {
      load(config_file);
    }

    private void load(string config_file)
    {
      XmlDocument document = new XmlDocument();
      try
      {
        document.Load(config_file);
        XPathNavigator navigator = document.CreateNavigator();
        XPathNodeIterator iterator = (XPathNodeIterator)
          navigator.Evaluate("config/*");
        while (iterator.MoveNext())
        {
          XPathNavigator element = iterator.Current;
          switch (element.Name)
          {
            case "src2srcml-path":
              src2srcml_path = element.Value;
              break;
          }
        }
      }
      catch (Exception e)
      {
        throw e;
      }
    }
  }
}
