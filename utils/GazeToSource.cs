using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NIER2014.Utils
{
  public class GazeSourceEntityRelationship
  {
    public SourceCodeEntitiesFile sc_file { get; set; }
    public SourceCodeEntity sc_entity { get; set; }
    public GazeData gaze_data { get; set; }

    public string class_ { get; set; }
    public string attribute { get; set; }
    public string method { get; set; }
    public string comment { get; set; }
  }

  public class GazeSourceRelationship : List<GazeSourceEntityRelationship>
  {
    public GazeSourceRelationship() : base()
    {
      //Must be defined, but do nothing.
    }

    public void writeSqlite(string file_name)
    {
      StreamWriter out_file = new StreamWriter(file_name);
      out_file.Write(
        "BEGIN TRANSACTION; " +
        "CREATE TABLE relationships (" +
          "class TEXT," +
          "attribute TEXT," +
          "method TEXT," +
          "comment TEXT" +
        "); ");
      foreach (GazeSourceEntityRelationship gser in this)
      {
        out_file.Write(
          "INSERT INTO relationships (class, attribute, " +
          "method, comment) VALUES ('" +
          prepareSqlString(gser.class_) + "', '" +
          prepareSqlString(gser.attribute) + "', '" +
          prepareSqlString(gser.method) + "', '" +
          prepareSqlString(gser.comment) + "'); ");
      }
      out_file.Write("COMMIT;");
      out_file.Close();
    }

    private string prepareSqlString(string instring)
    {
      if (instring == String.Empty || instring == null)
        return instring;
      else
        return instring.Replace("\\", "\\\\").Replace("'", "\\'");
    }
  }

  public abstract class GazeToSource
  {
    public static GazeSourceRelationship
      run(GazeResults gaze_results, SourceCodeEntitiesFileCollection scefc)
    {
      GazeSourceRelationship gsr = new GazeSourceRelationship();

      foreach (GazeData gd in gaze_results.gazes)
      {
        GazeSourceEntityRelationship gser = new GazeSourceEntityRelationship();

        SourceCodeEntitiesFile scef = getSourceFileByGaze(gd, scefc);
        if (scef != null)
        {
          foreach (SourceCodeEntity sce in scef)
          {
            if (isInEntity(gd, sce))
            {
              gser.sc_file = scef;
              gser.sc_entity = sce;
              gser.gaze_data = gd;

              switch (sce.Type)
              {
                case SourceCodeEntityType.CLASS:
                  gser.class_ = sce.Name;
                  break;
                case SourceCodeEntityType.ATTRIBUTE:
                  gser.attribute = sce.Name;
                  break;
                case SourceCodeEntityType.METHOD:
                  gser.method = sce.Name;
                  break;
                case SourceCodeEntityType.COMMENT:
                  gser.comment = sce.Name;
                  break;
              }
            }
          }
        }
        else
          gser.gaze_data = gd;

        gsr.Add(gser);
      }

      return gsr;
    }

    private static SourceCodeEntitiesFile getSourceFileByGaze(GazeData gd,
      SourceCodeEntitiesFileCollection scefc)
    {
      SourceCodeEntitiesFile scef = null;
      foreach (SourceCodeEntitiesFile cur_scef in scefc)
      {
        if (gd.filename == cur_scef.FileName)
        {
          scef = cur_scef;
          break;
        }
      }
      return scef;
    }

    private static bool isInEntity(GazeData gd, SourceCodeEntity sce)
    {
      if (gd.line > sce.LineStart && gd.line < sce.LineEnd)
        return true;
      else if (gd.line == sce.LineStart)
        return gd.col >= sce.ColumnStart;
      else if (gd.line == sce.LineEnd)
        return gd.col <= sce.ColumnEnd;
      else
        return false;
    }
  }
}
