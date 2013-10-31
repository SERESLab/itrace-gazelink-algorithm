using System;
using System.Collections.Generic;
using System.ComponentModel;
using NIER2014.Utils;

public class UtilsTest
{
  public static void Main(string[] args)
  {
    GazeResults gaze_results = GazeReader.run(
                               new List<string> { "data/gazedata1.xml" })[0];
    foreach (GazeData gaze_data in gaze_results.gazes)
    {
      foreach (PropertyDescriptor descriptor in TypeDescriptor.
                                                GetProperties(gaze_data))
      {
        Console.Write("{0}={1}; ", descriptor.Name,
                      descriptor.GetValue(gaze_data));
      }
      Console.WriteLine("");
    }

    if (args.Length == 0)
    {
      Console.WriteLine("Please provide a src2srcml binary path to continue.");
      return;
    }

    SourceCodeEntitiesFileCollection collection = SrcMLCodeReader.run(
      args[0], "data/java/");
    foreach (SourceCodeEntitiesFile file in collection)
    {
      Console.WriteLine(file.FileName  + ":");
      foreach (SourceCodeEntity entity in file)
      {
        Console.Write(" - ");
        foreach (PropertyDescriptor descriptor in TypeDescriptor.
                                                  GetProperties(entity))
        {
          Console.Write("{0}={1}; ", descriptor.Name,
                        descriptor.GetValue(entity));
        }
        Console.WriteLine("");
      }
    }
  }
}
