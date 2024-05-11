using UnityEngine;
using UnityEngine.UI;
using System.IO;

/// <summary>
/// Container class for hex cells.
/// </summary>
[System.Serializable]
public class HexCell
{
	/// <summary>
	/// Hexagonal coordinates unique to the cell.
	/// </summary>
	public HexCoordinates Coordinates => Grid.CellData[Index].coordinates;

	/// <summary>
	/// Transform component for the cell's UI visiualization. 
	/// </summary>
	public RectTransform UIRect
	{ get; set; }

	/// <summary>
	/// Grid that contains the cell.
	/// </summary>
	public HexGrid Grid
	{ get; set; }

	/// <summary>
	/// Grid chunk that contains the cell.
	/// </summary>
	public HexGridChunk Chunk
	{ get; set; }

	/// <summary>
	/// Unique global index of the cell.
	/// </summary>
	public int Index
	{ get; set; }

	/// <summary>
	/// Map column index of the cell.
	/// </summary>
	public int ColumnIndex
	{ get; set; }

	/// <summary>
	/// Surface elevation level.
	/// </summary>
	public int Elevation
	{
		get => Values.Elevation;
		set
		{
			if (Values.Elevation == value)
			{
				return;
			}
			Values = Values.WithElevation(value);
			Grid.ShaderData.ViewElevationChanged(Index);
			RefreshPosition();
			ValidateRivers();

			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (Flags.HasRoad(d) && GetElevationDifference(d) > 1)
				{
					RemoveRoad(d);
				}
			}

			Refresh();
		}
	}

	/// <summary>
	/// Water elevation level.
	/// </summary>
	public int WaterLevel
	{
		get => Values.WaterLevel;
		set
		{
			if (Values.WaterLevel == value)
			{
				return;
			}
			Values = Values.WithWaterLevel(value);
			Grid.ShaderData.ViewElevationChanged(Index);
			ValidateRivers();
			Refresh();
		}
	}

	/// <summary>
	/// Elevation at which the cell is visible.
	/// Highest of surface and water level.
	/// </summary>
	public int ViewElevation =>
		Elevation >= WaterLevel ? Elevation : WaterLevel;

	/// <summary>
	/// Whether the cell counts as underwater,
	/// which is when water is higher than surface.
	/// </summary>
	public bool IsUnderwater => WaterLevel > Elevation;

	/// <summary>
	/// Whether there is an incoming river.
	/// </summary>
	public bool HasIncomingRiver => Flags.HasAny(HexFlags.RiverIn);

	/// <summary>
	/// Whether there is an outgoing river.
	/// </summary>
	public bool HasOutgoingRiver => Flags.HasAny(HexFlags.RiverOut);

	/// <summary>
	/// Whether there is a river, either incoming, outgoing, or both.
	/// </summary>
	public bool HasRiver => Flags.HasAny(HexFlags.River);

	/// <summary>
	/// Incoming river direction, if applicable.
	/// </summary>
	public HexDirection IncomingRiver => Flags.RiverInDirection();

	/// <summary>
	/// Outgoing river direction, if applicable.
	/// </summary>
	public HexDirection OutgoingRiver => Flags.RiverOutDirection();

	/// <summary>
	/// Local position of this cell.
	/// </summary>
	public Vector3 Position => Grid.CellPositions[Index];

	/// <summary>
	/// Urban feature level.
	/// </summary>
	public int UrbanLevel
	{
		get => Values.UrbanLevel;
		set
		{
			if (Values.UrbanLevel != value)
			{
				Values = Values.WithUrbanLevel(value);
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Farm feature level.
	/// </summary>
	public int FarmLevel
	{
		get => Values.FarmLevel;
		set
		{
			if (Values.FarmLevel != value)
			{
				Values = Values.WithFarmLevel(value);
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Plant feature level.
	/// </summary>
	public int PlantLevel
	{
		get => Values.PlantLevel;
		set
		{
			if (Values.PlantLevel != value)
			{
				Values = Values.WithPlantLevel(value);
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Special feature index.
	/// </summary>
	public int SpecialIndex
	{
		get => Values.SpecialIndex;
		set
		{
			if (Values.SpecialIndex != value && !HasRiver)
			{
				Values = Values.WithSpecialIndex(value);
				RemoveRoads();
				Chunk.Refresh();
			}
		}
	}

	/// <summary>
	/// Whether the cell contains a special feature.
	/// </summary>
	public bool IsSpecial => SpecialIndex > 0;

	/// <summary>
	/// Whether the cell is considered inside a walled region.
	/// </summary>
	public bool Walled
	{
		get => Flags.HasAny(HexFlags.Walled);
		set
		{
			HexFlags newFlags = value ?
				Flags.With(HexFlags.Walled) : Flags.Without(HexFlags.Walled);
			if (Flags != newFlags)
			{
				Flags = newFlags;
				Refresh();
			}
		}
	}

	/// <summary>
	/// Terrain type index.
	/// </summary>
	public int TerrainTypeIndex
	{
		get => Values.TerrainTypeIndex;
		set
		{
			if (Values.TerrainTypeIndex != value)
			{
				Values = Values.WithTerrainTypeIndex(value);
				Grid.ShaderData.RefreshTerrain(Index);
			}
		}
	}

	/// <summary>
	/// Whether the cell counts as explored.
	/// </summary>
	public bool IsExplored =>
		Flags.HasAll(HexFlags.Explored | HexFlags.Explorable);

	/// <summary>
	/// Whether the cell is explorable.
	/// If not it never counts as explored or visible.
	/// </summary>
	public bool Explorable
	{
		get => Flags.HasAny(HexFlags.Explorable);
		set => Flags = value ?
			Flags.With(HexFlags.Explorable) :
			Flags.Without(HexFlags.Explorable);
	}

	/// <summary>
	/// Unit currently occupying the cell, if any.
	/// </summary>
	public HexUnit Unit
	{ get; set; }

	HexFlags Flags
	{
		get => Grid.CellData[Index].flags;
		set => Grid.CellData[Index].flags = value;
	}

	HexValues Values
	{
		get => Grid.CellData[Index].values;
		set => Grid.CellData[Index].values = value;
	}

	/// <summary>
	/// Mark the cell as explored.
	/// </summary>
	public void MarkAsExplored() => Flags = Flags.With(HexFlags.Explored);

	/// <summary>
	/// Get one of the neighbor cells. Only valid if that neighbor exists.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <returns>Neighbor cell, if it exists.</returns>
	public HexCell GetNeighbor(HexDirection direction) =>
		Grid.GetCell(Coordinates.Step(direction));

	/// <summary>
	/// Try to get one of the neighbor cells.
	/// </summary>
	/// <param name="direction">Neighbor direction relative to the cell.</param>
	/// <param name="cell">The neighbor cell, if it exists.</param>
	/// <returns>Whether the neighbor exists.</returns>
	public bool TryGetNeighbor(HexDirection direction, out HexCell cell) =>
		Grid.TryGetCell(Coordinates.Step(direction), out cell);

	/// <summary>
	/// Get the <see cref="HexEdgeType"/> based on this and another cell.
	/// </summary>
	/// <param name="otherCell">Other cell to consider as neighbor.</param>
	/// <returns><see cref="HexEdgeType"/> between cells.</returns>
	public HexEdgeType GetEdgeType(HexCell otherCell) =>
		HexMetrics.GetEdgeType(Elevation, otherCell.Elevation);

	/// <summary>
	/// Whether a river goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to the cell.</param>
	/// <returns>Whether a river goes through the edge.</returns>
	public bool HasRiverThroughEdge(HexDirection direction) =>
		Flags.HasRiverIn(direction) || Flags.HasRiverOut(direction);

	void RemoveIncomingRiver()
	{
		if (!HasIncomingRiver)
		{
			return;
		}
		
		HexCell neighbor = GetNeighbor(IncomingRiver);
		Flags = Flags.Without(HexFlags.RiverIn);
		neighbor.Flags = neighbor.Flags.Without(HexFlags.RiverOut);
		neighbor.Chunk.Refresh();
		Chunk.Refresh();
	}

	/// <summary>
	/// Remove the outgoing river, if it exists.
	/// </summary>
	void RemoveOutgoingRiver()
	{
		if (!HasOutgoingRiver)
		{
			return;
		}
		
		HexCell neighbor = GetNeighbor(OutgoingRiver);
		Flags = Flags.Without(HexFlags.RiverOut);
		neighbor.Flags = neighbor.Flags.Without(HexFlags.RiverIn);
		neighbor.Chunk.Refresh();
		Chunk.Refresh();
	}

	/// <summary>
	/// Remove both incoming and outgoing rivers, if they exist.
	/// </summary>
	public void RemoveRiver()
	{
		RemoveOutgoingRiver();
		RemoveIncomingRiver();
	}

	/// <summary>
	/// Define an outgoing river.
	/// </summary>
	/// <param name="direction">Direction of the river.</param>
	public void SetOutgoingRiver(HexDirection direction)
	{
		if (Flags.HasRiverOut(direction))
		{
			return;
		}

		HexCell neighbor = GetNeighbor(direction);
		if (!IsValidRiverDestination(neighbor))
		{
			return;
		}

		RemoveOutgoingRiver();
		if (Flags.HasRiverIn(direction))
		{
			RemoveIncomingRiver();
		}

		Flags = Flags.WithRiverOut(direction);
		SpecialIndex = 0;
		neighbor.RemoveIncomingRiver();
		neighbor.Flags = neighbor.Flags.WithRiverIn(direction.Opposite());
		neighbor.SpecialIndex = 0;

		RemoveRoad(direction);
	}

	/// <summary>
	/// Whether a road goes through a specific cell edge.
	/// </summary>
	/// <param name="direction">Edge direction relative to cell.</param>
	/// <returns>Whether a road goes through the edge.</returns>
	public bool HasRoadThroughEdge(HexDirection direction) =>
		Flags.HasRoad(direction);

	/// <summary>
	/// Define a road that goes in a specific direction.
	/// </summary>
	/// <param name="direction">Direction relative to cell.</param>
	public void AddRoad(HexDirection direction)
	{
		if (
			!Flags.HasRoad(direction) && !HasRiverThroughEdge(direction) &&
			!IsSpecial && !GetNeighbor(direction).IsSpecial &&
			GetElevationDifference(direction) <= 1
		)
		{
			Flags = Flags.WithRoad(direction);
			HexCell neighbor = GetNeighbor(direction);
			neighbor.Flags = neighbor.Flags.WithRoad(direction.Opposite());
			neighbor.Chunk.Refresh();
			Chunk.Refresh();
		}
	}

	/// <summary>
	/// Remove all roads from the cell.
	/// </summary>
	public void RemoveRoads()
	{
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			if (Flags.HasRoad(d))
			{
				RemoveRoad(d);
			}
		}
	}

	int GetElevationDifference(HexDirection direction)
	{
		int difference = Elevation - GetNeighbor(direction).Elevation;
		return difference >= 0 ? difference : -difference;
	}

	bool IsValidRiverDestination(HexCell neighbor) => neighbor &&
		(Elevation >= neighbor.Elevation || WaterLevel == neighbor.Elevation);

	void ValidateRivers()
	{
		if (HasOutgoingRiver &&
			!IsValidRiverDestination(GetNeighbor(OutgoingRiver)))
		{
			RemoveOutgoingRiver();
		}
		if (HasIncomingRiver &&
			!GetNeighbor(IncomingRiver).IsValidRiverDestination(this))
		{
			RemoveIncomingRiver();
		}
	}

	void RemoveRoad(HexDirection direction)
	{
		Flags = Flags.WithoutRoad(direction);
		HexCell neighbor = GetNeighbor(direction);
		neighbor.Flags = neighbor.Flags.WithoutRoad(direction.Opposite());
		neighbor.Chunk.Refresh();
		Chunk.Refresh();
	}

	void RefreshPosition()
	{
		Vector3 position = Position;
		position.y = Elevation * HexMetrics.elevationStep;
		position.y +=
			(HexMetrics.SampleNoise(position).y * 2f - 1f) *
			HexMetrics.elevationPerturbStrength;
		Grid.CellPositions[Index] = position;

		Vector3 uiPosition = UIRect.localPosition;
		uiPosition.z = -position.y;
		UIRect.localPosition = uiPosition;
	}

	void Refresh()
	{
		if (Chunk)
		{
			Chunk.Refresh();
			for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
			{
				if (TryGetNeighbor(d, out HexCell neighbor) &&
					neighbor.Chunk != Chunk)
				{
					neighbor.Chunk.Refresh();
				}
			}
			if (Unit)
			{
				Unit.ValidateLocation();
			}
		}
	}

	/// <summary>
	/// Refresh everything, to be done after generating a new map.
	/// </summary>
	public void RefreshAll()
	{
		RefreshPosition();
		Grid.ShaderData.RefreshTerrain(Index);
		Grid.ShaderData.RefreshVisibility(Index);
	}

	/// <summary>
	/// Save the cell data.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public void Save(BinaryWriter writer)
	{
		Values.Save(writer);
		Flags.Save(writer);
	}

	/// <summary>
	/// Load the cell data.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
	public void Load(BinaryReader reader, int header)
	{
		Values = HexValues.Load(reader, header);
		Flags = Flags.Load(reader, header);
		RefreshPosition();
		Grid.ShaderData.RefreshTerrain(Index);
		Grid.ShaderData.RefreshVisibility(Index);
	}

	/// <summary>
	/// Set the cell's UI label.
	/// </summary>
	/// <param name="text">Label text.</param>
	public void SetLabel(string text) =>
		UIRect.GetComponent<Text>().text = text;

	/// <summary>
	/// Disable the cell's highlight.
	/// </summary>
	public void DisableHighlight() =>
		UIRect.GetChild(0).GetComponent<Image>().enabled = false;

	/// <summary>
	/// Enable the cell's highlight. 
	/// </summary>
	/// <param name="color">Highlight color.</param>
	public void EnableHighlight(Color color)
	{
		Image highlight = UIRect.GetChild(0).GetComponent<Image>();
		highlight.color = color;
		highlight.enabled = true;
	}

	/// <summary>
	/// A cell counts as true if it is not null, otherwise as false.
	/// </summary>
	/// <param name="cell">The cell to check.</param>
	public static implicit operator bool(HexCell cell) => cell != null;
}
