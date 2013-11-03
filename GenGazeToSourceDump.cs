using System;
using System.Collections.Generic;
using NIER2014.Utils;

public class GenGazeToSourceDump
{
  public static void Main(string[] args)
  {
    if (args.Length < 2)
    {
      Console.WriteLine("You must provide the source directory and at least " +
                        "one gaze file to run this program.");
      return;
    }

    string source_directory = args[0];
    List<string> gaze_files = new List<string>();
    for (int i = 1; i < args.Length; ++i)
      gaze_files.Add(args[i]);

    Config config = new Config();
    SourceCodeEntitiesFileCollection source_info = SrcMLCodeReader.run(
      config.src2srcml_path, source_directory);
    List<GazeResults> gaze_results = GazeReader.run(gaze_files);

    for (int i = 0; i < gaze_results.Count; ++i)
    {
      GazeResults cur_gaze_results = gaze_results[i];
      string cur_filename = gaze_files[i];

      GazeSourceRelationship gsr = GazeToSource.run(cur_gaze_results,
                                                    source_info);
      gsr.writeSqlite(cur_filename + ".sql");
    }
  }
}
