using System.IO;
using UnityEngine;



/// <summary>
/// xiaonian£ºsave config
/// </summary>
public class HexEditorConfig
{
    public class HexConfig
    {
        public HexConfig()
        {
            ExportConfigFolder = "";
        }
        public string ExportConfigFolder;
    }

    HexConfig configData;

    static HexEditorConfig ins;
    public HexEditorConfig()
    {
        Init();
    }

    public static HexEditorConfig GetInstance()
    {
        if (ins == null)
        {
            ins = new HexEditorConfig();
        }
        return ins;
    }

    public void Init()
    {
        var config = PlayerPrefs.GetString("user_export_folder");
        if (string.IsNullOrEmpty(config))
            configData = new HexConfig();
        else
            configData = JsonUtility.FromJson<HexConfig>(config);
        

#if UNITY_EDITOR
        bool useDataFolder = true;
#else
		bool useDataFolder = false;
#endif

        if (useDataFolder)
        {
            configData.ExportConfigFolder = Path.Combine(Application.dataPath, "Data", "maps");
            if (!Directory.Exists(ExportConfigFolder))
            {
                Directory.CreateDirectory(ExportConfigFolder);
            }
            return;
        }
        configData.ExportConfigFolder = Application.persistentDataPath;
    }

    public void UpdateExportConfig(string url)
    {
        configData.ExportConfigFolder = url;
        var json = JsonUtility.ToJson(configData);
        PlayerPrefs.SetString("user_export_folder", json);
        PlayerPrefs.Save();
    }

    public string ExportConfigFolder { get { return configData.ExportConfigFolder; } }

}