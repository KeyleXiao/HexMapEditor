using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component that manages cell data used by shaders.
/// </summary>
public class HexCellShaderData : MonoBehaviour
{
	const float transitionSpeed = 255f;

	Texture2D cellTexture;

	Color32[] cellTextureData;

	bool[] visibilityTransitions;

	List<int> transitioningCellIndices = new();

	bool needsVisibilityReset;

	public HexGrid Grid { get; set; }

	public bool ImmediateMode { get; set; }

	/// <summary>
	/// Initialze the map data.
	/// </summary>
	/// <param name="x">Map X size.</param>
	/// <param name="z">Map Z size.</param>
	public void Initialize(int x, int z)
	{
		if (cellTexture)
		{
			cellTexture.Reinitialize(x, z);
		}
		else
		{
			cellTexture = new Texture2D(x, z, TextureFormat.RGBA32, false, true)
			{
				filterMode = FilterMode.Point,
				wrapModeU = TextureWrapMode.Repeat,
				wrapModeV = TextureWrapMode.Clamp
			};
			Shader.SetGlobalTexture("_HexCellData", cellTexture);
		}
		Shader.SetGlobalVector(
			"_HexCellData_TexelSize",
			new Vector4(1f / x, 1f / z, x, z));

		if (cellTextureData == null || cellTextureData.Length != x * z)
		{
			cellTextureData = new Color32[x * z];
			visibilityTransitions = new bool[x * z];
		}
		else
		{
			for (int i = 0; i < cellTextureData.Length; i++)
			{
				cellTextureData[i] = new Color32(0, 0, 0, 0);
				visibilityTransitions[i] = false;
			}
		}

		transitioningCellIndices.Clear();
		enabled = true;
	}

	/// <summary>
	/// Refresh the terrain data of a cell.
	/// Supports water surfaces up to 30 units high.
	/// </summary>
	/// <param name="cell">Cell with changed terrain type.</param>
	public void RefreshTerrain(int cellIndex)
	{
		HexCellData cell = Grid.CellData[cellIndex];
		Color32 data = cellTextureData[cellIndex];
		data.b = cell.IsUnderwater ?
			(byte)(cell.WaterSurfaceY * (255f / 30f)) : (byte)0;
		data.a = (byte)cell.TerrainTypeIndex;
		cellTextureData[cellIndex] = data;
		enabled = true;
	}

	/// <summary>
	/// Refresh visibility of a cell.
	/// </summary>
	/// <param name="cell">Cell with changed visibility.</param>
	public void RefreshVisibility(int cellIndex)
	{
		if (ImmediateMode)
		{
			cellTextureData[cellIndex].r = Grid.IsCellVisible(cellIndex) ?
				(byte)255 : (byte)0;
			cellTextureData[cellIndex].g = Grid.CellData[cellIndex].IsExplored ?
				(byte)255 : (byte)0;
		}
		else if (!visibilityTransitions[cellIndex])
		{
			visibilityTransitions[cellIndex] = true;
			transitioningCellIndices.Add(cellIndex);
		}
		enabled = true;
	}

	/// <summary>
	/// Indicate that view elevation data has changed,
	/// requiring a visibility reset.
	/// Supports water surfaces up to 30 units high.
	/// </summary>
	/// <param name="cell">Changed cell.</param>
	public void ViewElevationChanged(int cellIndex)
	{
		HexCellData cell = Grid.CellData[cellIndex];
		cellTextureData[cellIndex].b = cell.IsUnderwater ?
			(byte)(cell.WaterSurfaceY * (255f / 30f)) : (byte)0;
		needsVisibilityReset = true;
		enabled = true;
	}

	void LateUpdate()
	{
		if (needsVisibilityReset)
		{
			needsVisibilityReset = false;
			Grid.ResetVisibility();
		}

		int delta = (int)(Time.deltaTime * transitionSpeed);
		if (delta == 0)
		{
			delta = 1;
		}
		for (int i = 0; i < transitioningCellIndices.Count; i++)
		{
			if (!UpdateCellData(transitioningCellIndices[i], delta))
			{
				int lastIndex = transitioningCellIndices.Count - 1;
				transitioningCellIndices[i--] =
					transitioningCellIndices[lastIndex];
				transitioningCellIndices.RemoveAt(lastIndex);
			}
		}

		cellTexture.SetPixels32(cellTextureData);
		cellTexture.Apply();
		enabled = transitioningCellIndices.Count > 0;
	}

	bool UpdateCellData(int index, int delta)
	{
		Color32 data = cellTextureData[index];
		bool stillUpdating = false;

		if (Grid.CellData[index].IsExplored && data.g < 255)
		{
			stillUpdating = true;
			int t = data.g + delta;
			data.g = t >= 255 ? (byte)255 : (byte)t;
		}

		if (Grid.IsCellVisible(index))
		{
			if (data.r < 255)
			{
				stillUpdating = true;
				int t = data.r + delta;
				data.r = t >= 255 ? (byte)255 : (byte)t;
			}
		}
		else if (data.r > 0)
		{
			stillUpdating = true;
			int t = data.r - delta;
			data.r = t < 0 ? (byte)0 : (byte)t;
		}

		if (!stillUpdating)
		{
			visibilityTransitions[index] = false;
		}
		cellTextureData[index] = data;
		return stillUpdating;
	}
}
