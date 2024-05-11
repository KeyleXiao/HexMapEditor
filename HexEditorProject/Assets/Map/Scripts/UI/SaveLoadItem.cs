using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component that represents a single save or load menu item.
/// </summary>
public class SaveLoadItem : MonoBehaviour
{
	/// <summary>
	/// Parent save-load menu.
	/// </summary>
	public SaveLoadMenu Menu
	{ get; set; }

	/// <summary>
	/// Map name of the item.
	/// </summary>
	public string MapName
	{
		get => mapName;
		set
		{
			mapName = value;
			transform.GetChild(0).GetComponent<Text>().text = value;
		}
	}

	string mapName;

	/// <summary>
	/// Selection method, hooked up to the in-game UI.
	/// </summary>
	public void Select() => Menu.SelectItem(mapName);
}
