using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingMenu : MonoBehaviour
{
    public Button Save;
    public TMP_InputField DefaultFolder;
    public TMP_InputField UsersFolder;

    // Start is called before the first frame update
    void Start()
    {
        DefaultFolder.text = Application.persistentDataPath;

        UsersFolder.text = HexEditorConfig.GetInstance().ExportConfigFolder;

        Save.onClick.AddListener(SaveSetting);
    }

    private void SaveSetting()
    {
        HexEditorConfig.GetInstance().UpdateExportConfig(UsersFolder.text);
        gameObject.SetActive(false);
    }

    public void Open() 
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

}
