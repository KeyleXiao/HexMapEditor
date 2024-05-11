using UnityEngine;

/// <summary>
/// Component that manages a single chunk of <see cref="HexGrid"/>.
/// </summary>
public class HexGridChunk : MonoBehaviour
{
	readonly static Color weights1 = new(1f, 0f, 0f);
	readonly static Color weights2 = new(0f, 1f, 0f);
	readonly static Color weights3 = new(0f, 0f, 1f);

	public HexGrid Grid
	{ get; set; }

	[SerializeField]
	HexMesh terrain, rivers, roads, water, waterShore, estuaries;

	[SerializeField]
	HexFeatureManager features;

	int[] cellIndices;

	Canvas gridCanvas;

	void Awake()
	{
		gridCanvas = GetComponentInChildren<Canvas>();
		cellIndices = new int[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
	}

	/// <summary>
	/// Add a cell to the chunk.
	/// </summary>
	/// <param name="index">Index of the cell for the chunk.</param>
	/// <param name="cellIndex">Index of the cell to add.</param>
	/// <param name="cellUI">UI root transform of the cell.</param>
	public void AddCell(int index, int cellIndex, RectTransform cellUI)
	{
		cellIndices[index] = cellIndex;
		cellUI.SetParent(gridCanvas.transform, false);
	}

	/// <summary>
	/// Refresh the chunk.
	/// </summary>
	public void Refresh() => enabled = true;

	/// <summary>
	/// Control whether the map UI is visibile or hidden for the chunk.
	/// </summary>
	/// <param name="visible">Whether the UI should be visible.</param>
	public void ShowUI(bool visible) =>
		gridCanvas.gameObject.SetActive(visible);

	void LateUpdate()
	{
		Triangulate();
		enabled = false;
	}

	/// <summary>
	/// Triangulate everything in the chunk.
	/// </summary>
	public void Triangulate()
	{
		terrain.Clear();
		rivers.Clear();
		roads.Clear();
		water.Clear();
		waterShore.Clear();
		estuaries.Clear();
		features.Clear();
		for (int i = 0; i < cellIndices.Length; i++)
		{
			Triangulate(cellIndices[i]);
		}
		terrain.Apply();
		rivers.Apply();
		roads.Apply();
		water.Apply();
		waterShore.Apply();
		estuaries.Apply();
		features.Apply();
	}

	void Triangulate(int cellIndex)
	{
		HexCellData cell = Grid.CellData[cellIndex];
		Vector3 cellPosition = Grid.CellPositions[cellIndex];
		for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
		{
			Triangulate(d, cell, cellIndex, cellPosition);
		}
		if (!cell.IsUnderwater)
		{
			if (!cell.HasRiver && !cell.HasRoads)
			{
				features.AddFeature(cell, cellPosition);
			}
			if (cell.IsSpecial)
			{
				features.AddSpecialFeature(cell, cellPosition);
			}
		}
	}

	void Triangulate(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center)
	{
		var e = new EdgeVertices(
			center + HexMetrics.GetFirstSolidCorner(direction),
			center + HexMetrics.GetSecondSolidCorner(direction));

		if (cell.HasRiver)
		{
			if (cell.HasRiverThroughEdge(direction))
			{
				e.v3.y = cell.StreamBedY;
				if (cell.HasRiverBeginOrEnd)
				{
					TriangulateWithRiverBeginOrEnd(cell, cellIndex, center, e);
				}
				else
				{
					TriangulateWithRiver(direction, cell, cellIndex, center, e);
				}
			}
			else
			{
				TriangulateAdjacentToRiver(
					direction, cell, cellIndex, center, e);
			}
		}
		else
		{
			TriangulateWithoutRiver(direction, cell, cellIndex, center, e);
			if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
			{
				features.AddFeature(
					cell, (center + e.v1 + e.v5) * (1f / 3f));
			}
		}

		if (direction <= HexDirection.SE)
		{
			TriangulateConnection(direction, cell, cellIndex, center.y, e);
		}

		if (cell.IsUnderwater)
		{
			TriangulateWater(direction, cell, cellIndex, center);
		}
	}

	void TriangulateWater(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center)
	{
		center.y = cell.WaterSurfaceY;
		HexCoordinates neighborCoordinates = cell.coordinates.Step(direction);
		if (Grid.TryGetCellIndex(neighborCoordinates, out int neighborIndex) &&
			!Grid.CellData[neighborIndex].IsUnderwater)
		{
			TriangulateWaterShore(
				direction, cell, cellIndex, neighborIndex,
				neighborCoordinates.ColumnIndex, center);
		}
		else
		{
			TriangulateOpenWater(
				cell.coordinates, direction, cellIndex, neighborIndex, center);
		}
	}

	void TriangulateOpenWater(
		HexCoordinates coordinates,
		HexDirection direction,
		int cellIndex,
		int neighborIndex,
		Vector3 center)
	{
		Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
		Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

		water.AddTriangle(center, c1, c2);
		Vector3 indices;
		indices.x = indices.y = indices.z = cellIndex;
		water.AddTriangleCellData(indices, weights1);

		if (direction <= HexDirection.SE && neighborIndex != -1)
		{
			Vector3 bridge = HexMetrics.GetWaterBridge(direction);
			Vector3 e1 = c1 + bridge;
			Vector3 e2 = c2 + bridge;

			water.AddQuad(c1, c2, e1, e2);
			indices.y = neighborIndex;
			water.AddQuadCellData(indices, weights1, weights2);

			if (direction <= HexDirection.E)
			{
				if (!Grid.TryGetCellIndex(
					coordinates.Step(direction.Next()),
					out int nextNeighborIndex) ||
					!Grid.CellData[nextNeighborIndex].IsUnderwater)
				{
					return;
				}
				water.AddTriangle(
					c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next()));
				indices.z = nextNeighborIndex;
				water.AddTriangleCellData(
					indices, weights1, weights2, weights3);
			}
		}
	}

	void TriangulateWaterShore(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		int neighborIndex,
		int neighborColumnIndex,
		Vector3 center)
	{
		var e1 = new EdgeVertices(
			center + HexMetrics.GetFirstWaterCorner(direction),
			center + HexMetrics.GetSecondWaterCorner(direction));
		water.AddTriangle(center, e1.v1, e1.v2);
		water.AddTriangle(center, e1.v2, e1.v3);
		water.AddTriangle(center, e1.v3, e1.v4);
		water.AddTriangle(center, e1.v4, e1.v5);
		Vector3 indices;
		indices.x = indices.z = cellIndex;
		indices.y = neighborIndex;
		water.AddTriangleCellData(indices, weights1);
		water.AddTriangleCellData(indices, weights1);
		water.AddTriangleCellData(indices, weights1);
		water.AddTriangleCellData(indices, weights1);

		Vector3 center2 = Grid.CellPositions[neighborIndex];
		int cellColumnIndex = cell.coordinates.ColumnIndex;
		if (neighborColumnIndex < cellColumnIndex - 1)
		{
			center2.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
		}
		else if (neighborColumnIndex > cellColumnIndex + 1)
		{
			center2.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
		}
		center2.y = center.y;
		var e2 = new EdgeVertices(
			center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
			center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite()));

		if (cell.HasRiverThroughEdge(direction))
		{
			TriangulateEstuary(
				e1, e2, cell.HasIncomingRiverThroughEdge(direction), indices);
		}
		else
		{
			waterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
			waterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
			waterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
			waterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
			waterShore.AddQuadUV(0f, 0f, 0f, 1f);
			waterShore.AddQuadUV(0f, 0f, 0f, 1f);
			waterShore.AddQuadUV(0f, 0f, 0f, 1f);
			waterShore.AddQuadUV(0f, 0f, 0f, 1f);
			waterShore.AddQuadCellData(indices, weights1, weights2);
			waterShore.AddQuadCellData(indices, weights1, weights2);
			waterShore.AddQuadCellData(indices, weights1, weights2);
			waterShore.AddQuadCellData(indices, weights1, weights2);
		}

		HexCoordinates nextNeighborCoordinates = cell.coordinates.Step(
			direction.Next());
		if (Grid.TryGetCellIndex(
			nextNeighborCoordinates, out int nextNeighborIndex))
		{
			Vector3 center3 = Grid.CellPositions[nextNeighborIndex];
			bool nextNeighborIsUnderwater =
				Grid.CellData[nextNeighborIndex].IsUnderwater;
			int nextNeighborColumnIndex = nextNeighborCoordinates.ColumnIndex;
			if (nextNeighborColumnIndex < cellColumnIndex - 1)
			{
				center3.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
			}
			else if (nextNeighborColumnIndex > cellColumnIndex + 1)
			{
				center3.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
			}
			Vector3 v3 = center3 + (nextNeighborIsUnderwater ?
				HexMetrics.GetFirstWaterCorner(direction.Previous()) :
				HexMetrics.GetFirstSolidCorner(direction.Previous()));
			v3.y = center.y;
			waterShore.AddTriangle(e1.v5, e2.v5, v3);
			waterShore.AddTriangleUV(
				new Vector2(0f, 0f),
				new Vector2(0f, 1f),
				new Vector2(0f, nextNeighborIsUnderwater ? 0f : 1f));
			indices.z = nextNeighborIndex;
			waterShore.AddTriangleCellData(
				indices, weights1, weights2, weights3);
		}
	}

	void TriangulateEstuary(
		EdgeVertices e1, EdgeVertices e2, bool incomingRiver, Vector3 indices)
	{
		waterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
		waterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
		waterShore.AddTriangleUV(
			new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
		waterShore.AddTriangleUV(
			new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f));
		waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);
		waterShore.AddTriangleCellData(indices, weights2, weights1, weights1);

		estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
		estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
		estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

		estuaries.AddQuadUV(
			new Vector2(0f, 1f), new Vector2(0f, 0f),
			new Vector2(1f, 1f), new Vector2(0f, 0f));
		estuaries.AddTriangleUV(
			new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(1f, 1f));
		estuaries.AddQuadUV(
			new Vector2(0f, 0f), new Vector2(0f, 0f),
			new Vector2(1f, 1f), new Vector2(0f, 1f));
		estuaries.AddQuadCellData(
			indices, weights2, weights1, weights2, weights1);
		estuaries.AddTriangleCellData(indices, weights1, weights2, weights2);
		estuaries.AddQuadCellData(indices, weights1, weights2);

		if (incomingRiver)
		{
			estuaries.AddQuadUV2(
				new Vector2(1.5f, 1f), new Vector2(0.7f, 1.15f),
				new Vector2(1f, 0.8f), new Vector2(0.5f, 1.1f));
			estuaries.AddTriangleUV2(
				new Vector2(0.5f, 1.1f),
				new Vector2(1f, 0.8f),
				new Vector2(0f, 0.8f));
			estuaries.AddQuadUV2(
				new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
				new Vector2(0f, 0.8f), new Vector2(-0.5f, 1f));
		}
		else
		{
			estuaries.AddQuadUV2(
				new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
				new Vector2(0f, 0f), new Vector2(0.5f, -0.3f));
			estuaries.AddTriangleUV2(
				new Vector2(0.5f, -0.3f),
				new Vector2(0f, 0f),
				new Vector2(1f, 0f));
			estuaries.AddQuadUV2(
				new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
				new Vector2(1f, 0f), new Vector2(1.5f, -0.2f));
		}
	}

	void TriangulateWithoutRiver(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center,
		EdgeVertices e)
	{
		TriangulateEdgeFan(center, e, cellIndex);

		if (cell.HasRoads)
		{
			Vector2 interpolators = GetRoadInterpolators(direction, cell);
			TriangulateRoad(
				center,
				Vector3.Lerp(center, e.v1, interpolators.x),
				Vector3.Lerp(center, e.v5, interpolators.y),
				e, cell.HasRoadThroughEdge(direction), cellIndex);
		}
	}

	Vector2 GetRoadInterpolators(HexDirection direction, HexCellData cell)
	{
		Vector2 interpolators;
		if (cell.HasRoadThroughEdge(direction))
		{
			interpolators.x = interpolators.y = 0.5f;
		}
		else
		{
			interpolators.x =
				cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
			interpolators.y =
				cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
		}
		return interpolators;
	}

	void TriangulateAdjacentToRiver(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center,
		EdgeVertices e)
	{
		if (cell.HasRoads)
		{
			TriangulateRoadAdjacentToRiver(
				direction, cell, cellIndex, center, e);
		}

		if (cell.HasRiverThroughEdge(direction.Next()))
		{
			if (cell.HasRiverThroughEdge(direction.Previous()))
			{
				center += HexMetrics.GetSolidEdgeMiddle(direction) *
					(HexMetrics.innerToOuter * 0.5f);
			}
			else if (cell.HasRiverThroughEdge(direction.Previous2()))
			{
				center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
			}
		}
		else if (cell.HasRiverThroughEdge(direction.Previous()) &&
			cell.HasRiverThroughEdge(direction.Next2()))
		{
			center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
		}

		var m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f));

		TriangulateEdgeStrip(
			m, weights1, cellIndex,
			e, weights1, cellIndex);
		TriangulateEdgeFan(center, m, cellIndex);

		if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction))
		{
			features.AddFeature(
				cell, (center + e.v1 + e.v5) * (1f / 3f));
		}
	}

	void TriangulateRoadAdjacentToRiver(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center,
		EdgeVertices e)
	{
		bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
		bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
		bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
		Vector2 interpolators = GetRoadInterpolators(direction, cell);
		Vector3 roadCenter = center;

		HexDirection riverIn = cell.IncomingRiver;
		HexDirection riverOut = cell.OutgoingRiver;

		if (cell.HasRiverBeginOrEnd)
		{
			roadCenter += HexMetrics.GetSolidEdgeMiddle(
				(cell.HasIncomingRiver ? riverIn : riverOut).Opposite()
			) * (1f / 3f);
		}
		else if (riverIn == riverOut.Opposite())
		{
			Vector3 corner;
			if (previousHasRiver)
			{
				if (!hasRoadThroughEdge &&
					!cell.HasRoadThroughEdge(direction.Next()))
				{
					return;
				}
				corner = HexMetrics.GetSecondSolidCorner(direction);
			}
			else
			{
				if (!hasRoadThroughEdge &&
					!cell.HasRoadThroughEdge(direction.Previous()))
				{
					return;
				}
				corner = HexMetrics.GetFirstSolidCorner(direction);
			}
			roadCenter += corner * 0.5f;
			if (riverIn == direction.Next() && (
				cell.HasRoadThroughEdge(direction.Next2()) ||
				cell.HasRoadThroughEdge(direction.Opposite())))
			{
				features.AddBridge(roadCenter, center - corner * 0.5f);
			}
			center += corner * 0.25f;
		}
		else if (riverIn == riverOut.Previous())
		{
			roadCenter -= HexMetrics.GetSecondCorner(riverIn) * 0.2f;
		}
		else if (riverIn == riverOut.Next())
		{
			roadCenter -= HexMetrics.GetFirstCorner(riverIn) * 0.2f;
		}
		else if (previousHasRiver && nextHasRiver)
		{
			if (!hasRoadThroughEdge)
			{
				return;
			}
			Vector3 offset =
				HexMetrics.GetSolidEdgeMiddle(direction) *
				HexMetrics.innerToOuter;
			roadCenter += offset * 0.7f;
			center += offset * 0.5f;
		}
		else
		{
			HexDirection middle;
			if (previousHasRiver)
			{
				middle = direction.Next();
			}
			else if (nextHasRiver)
			{
				middle = direction.Previous();
			}
			else
			{
				middle = direction;
			}
			if (!cell.HasRoadThroughEdge(middle) &&
				!cell.HasRoadThroughEdge(middle.Previous()) &&
				!cell.HasRoadThroughEdge(middle.Next()))
			{
				return;
			}
			Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
			roadCenter += offset * 0.25f;
			if (direction == middle &&
				cell.HasRoadThroughEdge(direction.Opposite()))
			{
				features.AddBridge(
					roadCenter,
					center - offset * (HexMetrics.innerToOuter * 0.7f));
			}
		}

		Vector3 mL = Vector3.Lerp(roadCenter, e.v1, interpolators.x);
		Vector3 mR = Vector3.Lerp(roadCenter, e.v5, interpolators.y);
		TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge, cellIndex);
		if (previousHasRiver)
		{
			TriangulateRoadEdge(roadCenter, center, mL, cellIndex);
		}
		if (nextHasRiver)
		{
			TriangulateRoadEdge(roadCenter, mR, center, cellIndex);
		}
	}

	void TriangulateWithRiverBeginOrEnd(
		HexCellData cell, int cellIndex, Vector3 center, EdgeVertices e)
	{
		var m = new EdgeVertices(
			Vector3.Lerp(center, e.v1, 0.5f),
			Vector3.Lerp(center, e.v5, 0.5f));
		m.v3.y = e.v3.y;

		TriangulateEdgeStrip(
			m, weights1, cellIndex,
			e, weights1, cellIndex);
		TriangulateEdgeFan(center, m, cellIndex);

		if (!cell.IsUnderwater)
		{
			bool reversed = cell.HasIncomingRiver;
			Vector3 indices;
			indices.x = indices.y = indices.z = cellIndex;
			TriangulateRiverQuad(
				m.v2, m.v4, e.v2, e.v4,
				cell.RiverSurfaceY, 0.6f, reversed, indices);
			center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
			rivers.AddTriangle(center, m.v2, m.v4);
			if (reversed)
			{
				rivers.AddTriangleUV(
					new Vector2(0.5f, 0.4f),
					new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
			}
			else
			{
				rivers.AddTriangleUV(
					new Vector2(0.5f, 0.4f),
					new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
			}
			rivers.AddTriangleCellData(indices, weights1);
		}
	}

	void TriangulateWithRiver(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		Vector3 center,
		EdgeVertices e)
	{
		Vector3 centerL, centerR;
		if (cell.HasRiverThroughEdge(direction.Opposite()))
		{
			centerL = center +
				HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
			centerR = center +
				HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
		}
		else if (cell.HasRiverThroughEdge(direction.Next()))
		{
			centerL = center;
			centerR = Vector3.Lerp(center, e.v5, 2f / 3f);
		}
		else if (cell.HasRiverThroughEdge(direction.Previous()))
		{
			centerL = Vector3.Lerp(center, e.v1, 2f / 3f);
			centerR = center;
		}
		else if (cell.HasRiverThroughEdge(direction.Next2()))
		{
			centerL = center;
			centerR = center +
				HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
				(0.5f * HexMetrics.innerToOuter);
		}
		else
		{
			centerL = center +
				HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
				(0.5f * HexMetrics.innerToOuter);
			centerR = center;
		}
		center = Vector3.Lerp(centerL, centerR, 0.5f);

		var m = new EdgeVertices(
			Vector3.Lerp(centerL, e.v1, 0.5f),
			Vector3.Lerp(centerR, e.v5, 0.5f),
			1f / 6f);
		m.v3.y = center.y = e.v3.y;

		TriangulateEdgeStrip(
			m, weights1, cellIndex,
			e, weights1, cellIndex);

		terrain.AddTriangle(centerL, m.v1, m.v2);
		terrain.AddQuad(centerL, center, m.v2, m.v3);
		terrain.AddQuad(center, centerR, m.v3, m.v4);
		terrain.AddTriangle(centerR, m.v4, m.v5);

		Vector3 indices;
		indices.x = indices.y = indices.z = cellIndex;
		terrain.AddTriangleCellData(indices, weights1);
		terrain.AddQuadCellData(indices, weights1);
		terrain.AddQuadCellData(indices, weights1);
		terrain.AddTriangleCellData(indices, weights1);

		if (!cell.IsUnderwater)
		{
			bool reversed = cell.HasIncomingRiverThroughEdge(direction);
			TriangulateRiverQuad(
				centerL, centerR, m.v2, m.v4,
				cell.RiverSurfaceY, 0.4f, reversed, indices);
			TriangulateRiverQuad(
				m.v2, m.v4, e.v2, e.v4,
				cell.RiverSurfaceY, 0.6f, reversed, indices);
		}
	}

	void TriangulateConnection(
		HexDirection direction,
		HexCellData cell,
		int cellIndex,
		float centerY,
		EdgeVertices e1)
	{
		if (!Grid.TryGetCellIndex(
			cell.coordinates.Step(direction), out int neighborIndex))
		{
			return;
		}
		HexCellData neighbor = Grid.CellData[neighborIndex];
		Vector3 bridge = HexMetrics.GetBridge(direction);
		bridge.y = Grid.CellPositions[neighborIndex].y - centerY;
		var e2 = new EdgeVertices(e1.v1 + bridge, e1.v5 + bridge);

		bool hasRiver = cell.HasRiverThroughEdge(direction);
		bool hasRoad = cell.HasRoadThroughEdge(direction);

		if (hasRiver)
		{
			e2.v3.y = neighbor.StreamBedY;
			Vector3 indices;
			indices.x = indices.z = cellIndex;
			indices.y = neighborIndex;

			if (!cell.IsUnderwater)
			{
				if (!neighbor.IsUnderwater)
				{
					TriangulateRiverQuad(
						e1.v2, e1.v4, e2.v2, e2.v4,
						cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
						cell.HasIncomingRiverThroughEdge(direction),
						indices);
				}
				else if (cell.Elevation > neighbor.WaterLevel)
				{
					TriangulateWaterfallInWater(
						e1.v2, e1.v4, e2.v2, e2.v4,
						cell.RiverSurfaceY, neighbor.RiverSurfaceY,
						neighbor.WaterSurfaceY, indices);
				}
			}
			else if (!neighbor.IsUnderwater &&
				neighbor.Elevation > cell.WaterLevel)
			{
				TriangulateWaterfallInWater(
					e2.v4, e2.v2, e1.v4, e1.v2,
					neighbor.RiverSurfaceY, cell.RiverSurfaceY,
					cell.WaterSurfaceY, indices);
			}
		}

		if (cell.GetEdgeType(neighbor) == HexEdgeType.Slope)
		{
			TriangulateEdgeTerraces(e1, cellIndex, e2, neighborIndex, hasRoad);
		}
		else
		{
			TriangulateEdgeStrip(
				e1, weights1, cellIndex,
				e2, weights2, neighborIndex, hasRoad);
		}

		features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

		if (direction <= HexDirection.E &&
			Grid.TryGetCellIndex(
				cell.coordinates.Step(direction.Next()),
				out int nextNeighborIndex))
		{
			HexCellData nextNeighbor = Grid.CellData[nextNeighborIndex];
			Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
			v5.y = Grid.CellPositions[nextNeighborIndex].y;

			if (cell.Elevation <= neighbor.Elevation)
			{
				if (cell.Elevation <= nextNeighbor.Elevation)
				{
					TriangulateCorner(
						e1.v5, cellIndex, cell,
						e2.v5, neighborIndex, neighbor,
						v5, nextNeighborIndex, nextNeighbor);
				}
				else
				{
					TriangulateCorner(
						v5, nextNeighborIndex, nextNeighbor,
						e1.v5, cellIndex, cell,
						e2.v5, neighborIndex, neighbor);
				}
			}
			else if (neighbor.Elevation <= nextNeighbor.Elevation)
			{
				TriangulateCorner(
					e2.v5, neighborIndex, neighbor,
					v5, nextNeighborIndex, nextNeighbor,
					e1.v5, cellIndex, cell);
			}
			else {
				TriangulateCorner(
					v5, nextNeighborIndex, nextNeighbor,
					e1.v5, cellIndex, cell,
					e2.v5, neighborIndex, neighbor);
			}
		}
	}

	void TriangulateWaterfallInWater(
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
		float y1, float y2, float waterY, Vector3 indices)
	{
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;
		v1 = HexMetrics.Perturb(v1);
		v2 = HexMetrics.Perturb(v2);
		v3 = HexMetrics.Perturb(v3);
		v4 = HexMetrics.Perturb(v4);
		float t = (waterY - y2) / (y1 - y2);
		v3 = Vector3.Lerp(v3, v1, t);
		v4 = Vector3.Lerp(v4, v2, t);
		rivers.AddQuadUnperturbed(v1, v2, v3, v4);
		rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
		rivers.AddQuadCellData(indices, weights1, weights2);
	}

	void TriangulateCorner(
		Vector3 bottom, int bottomCellIndex, HexCellData bottomCell,
		Vector3 left, int leftCellIndex, HexCellData leftCell,
		Vector3 right, int rightCellIndex, HexCellData rightCell)
	{
		HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
		HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

		if (leftEdgeType == HexEdgeType.Slope)
		{
			if (rightEdgeType == HexEdgeType.Slope)
			{
				TriangulateCornerTerraces(
					bottom, bottomCellIndex,
					left, leftCellIndex,
					right, rightCellIndex);
			}
			else if (rightEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(
					left, leftCellIndex,
					right, rightCellIndex,
					bottom, bottomCellIndex);
			}
			else
			{
				TriangulateCornerTerracesCliff(
					bottom, bottomCellIndex, bottomCell,
					left, leftCellIndex, leftCell,
					right, rightCellIndex, rightCell);
			}
		}
		else if (rightEdgeType == HexEdgeType.Slope)
		{
			if (leftEdgeType == HexEdgeType.Flat)
			{
				TriangulateCornerTerraces(
					right, rightCellIndex,
					bottom, bottomCellIndex,
					left, leftCellIndex);
			}
			else
			{
				TriangulateCornerCliffTerraces(
					bottom, bottomCellIndex, bottomCell,
					left, leftCellIndex, leftCell,
					right, rightCellIndex, rightCell);
			}
		}
		else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			if (leftCell.Elevation < rightCell.Elevation)
			{
				TriangulateCornerCliffTerraces(
					right, rightCellIndex, rightCell,
					bottom, bottomCellIndex, bottomCell,
					left, leftCellIndex, leftCell);
			}
			else
			{
				TriangulateCornerTerracesCliff(
					left, leftCellIndex, leftCell,
					right, rightCellIndex, rightCell,
					bottom, bottomCellIndex, bottomCell);
			}
		}
		else
		{
			terrain.AddTriangle(bottom, left, right);
			Vector3 indices;
			indices.x = bottomCellIndex;
			indices.y = leftCellIndex;
			indices.z = rightCellIndex;
			terrain.AddTriangleCellData(indices, weights1, weights2, weights3);
		}

		features.AddWall(
			bottom, bottomCell, left, leftCell, right, rightCell);
	}

	void TriangulateEdgeTerraces(
		EdgeVertices begin, int beginCellIndex,
		EdgeVertices end, int endCellIndex,
		bool hasRoad)
	{
		EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
		Color w2 = HexMetrics.TerraceLerp(weights1, weights2, 1);
		float i1 = beginCellIndex;
		float i2 = endCellIndex;

		TriangulateEdgeStrip(begin, weights1, i1, e2, w2, i2, hasRoad);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			EdgeVertices e1 = e2;
			Color w1 = w2;
			e2 = EdgeVertices.TerraceLerp(begin, end, i);
			w2 = HexMetrics.TerraceLerp(weights1, weights2, i);
			TriangulateEdgeStrip(e1, w1, i1, e2, w2, i2, hasRoad);
		}

		TriangulateEdgeStrip(e2, w2, i1, end, weights2, i2, hasRoad);
	}

	void TriangulateCornerTerraces(
		Vector3 begin, int beginCellIndex,
		Vector3 left, int leftCellIndex,
		Vector3 right, int rightCellIndex)
	{
		Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
		Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
		Color w3 = HexMetrics.TerraceLerp(weights1, weights2, 1);
		Color w4 = HexMetrics.TerraceLerp(weights1, weights3, 1);
		Vector3 indices;
		indices.x = beginCellIndex;
		indices.y = leftCellIndex;
		indices.z = rightCellIndex;

		terrain.AddTriangle(begin, v3, v4);
		terrain.AddTriangleCellData(indices, weights1, w3, w4);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			Vector3 v1 = v3;
			Vector3 v2 = v4;
			Color w1 = w3;
			Color w2 = w4;
			v3 = HexMetrics.TerraceLerp(begin, left, i);
			v4 = HexMetrics.TerraceLerp(begin, right, i);
			w3 = HexMetrics.TerraceLerp(weights1, weights2, i);
			w4 = HexMetrics.TerraceLerp(weights1, weights3, i);
			terrain.AddQuad(v1, v2, v3, v4);
			terrain.AddQuadCellData(indices, w1, w2, w3, w4);
		}

		terrain.AddQuad(v3, v4, left, right);
		terrain.AddQuadCellData(indices, w3, w4, weights2, weights3);
	}

	void TriangulateCornerTerracesCliff(
		Vector3 begin, int beginCellIndex, HexCellData beginCell,
		Vector3 left, int leftCellIndex, HexCellData leftCell,
		Vector3 right, int rightCellIndex, HexCellData rightCell)
	{
		float b = 1f / (rightCell.Elevation - beginCell.Elevation);
		if (b < 0)
		{
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(
			HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
		Color boundaryWeights = Color.Lerp(weights1, weights3, b);
		Vector3 indices;
		indices.x = beginCellIndex;
		indices.y = leftCellIndex;
		indices.z = rightCellIndex;

		TriangulateBoundaryTriangle(
			begin, weights1, left, weights2,
			boundary, boundaryWeights, indices);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(
				left, weights2, right, weights3,
				boundary, boundaryWeights, indices);
		}
		else
		{
			terrain.AddTriangleUnperturbed(
				HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleCellData(
				indices, weights2, weights3, boundaryWeights);
		}
	}

	void TriangulateCornerCliffTerraces(
		Vector3 begin, int beginCellIndex, HexCellData beginCell,
		Vector3 left, int leftCellIndex, HexCellData leftCell,
		Vector3 right, int rightCellIndex, HexCellData rightCell)
	{
		float b = 1f / (leftCell.Elevation - beginCell.Elevation);
		if (b < 0)
		{
			b = -b;
		}
		Vector3 boundary = Vector3.Lerp(
			HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
		Color boundaryWeights = Color.Lerp(weights1, weights2, b);
		Vector3 indices;
		indices.x = beginCellIndex;
		indices.y = leftCellIndex;
		indices.z = rightCellIndex;

		TriangulateBoundaryTriangle(
			right, weights3, begin, weights1,
			boundary, boundaryWeights, indices);

		if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
		{
			TriangulateBoundaryTriangle(
				left, weights2, right, weights3,
				boundary, boundaryWeights, indices);
		}
		else
		{
			terrain.AddTriangleUnperturbed(
				HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
			terrain.AddTriangleCellData(
				indices, weights2, weights3, boundaryWeights);
		}
	}

	void TriangulateBoundaryTriangle(
		Vector3 begin, Color beginWeights,
		Vector3 left, Color leftWeights,
		Vector3 boundary, Color boundaryWeights, Vector3 indices)
	{
		Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
		Color w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, 1);

		terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
		terrain.AddTriangleCellData(indices, beginWeights, w2, boundaryWeights);

		for (int i = 2; i < HexMetrics.terraceSteps; i++)
		{
			Vector3 v1 = v2;
			Color w1 = w2;
			v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
			w2 = HexMetrics.TerraceLerp(beginWeights, leftWeights, i);
			terrain.AddTriangleUnperturbed(v1, v2, boundary);
			terrain.AddTriangleCellData(indices, w1, w2, boundaryWeights);
		}

		terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
		terrain.AddTriangleCellData(indices, w2, leftWeights, boundaryWeights);
	}

	void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
	{
		terrain.AddTriangle(center, edge.v1, edge.v2);
		terrain.AddTriangle(center, edge.v2, edge.v3);
		terrain.AddTriangle(center, edge.v3, edge.v4);
		terrain.AddTriangle(center, edge.v4, edge.v5);

		Vector3 indices;
		indices.x = indices.y = indices.z = index;
		terrain.AddTriangleCellData(indices, weights1);
		terrain.AddTriangleCellData(indices, weights1);
		terrain.AddTriangleCellData(indices, weights1);
		terrain.AddTriangleCellData(indices, weights1);
	}

	void TriangulateEdgeStrip(
		EdgeVertices e1, Color w1, float index1,
		EdgeVertices e2, Color w2, float index2,
		bool hasRoad = false)
	{
		terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
		terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
		terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
		terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

		Vector3 indices;
		indices.x = indices.z = index1;
		indices.y = index2;
		terrain.AddQuadCellData(indices, w1, w2);
		terrain.AddQuadCellData(indices, w1, w2);
		terrain.AddQuadCellData(indices, w1, w2);
		terrain.AddQuadCellData(indices, w1, w2);

		if (hasRoad)
		{
			TriangulateRoadSegment(
				e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4, w1, w2, indices);
		}
	}

	void TriangulateRiverQuad(
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
		float y, float v, bool reversed, Vector3 indices) =>
		TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed, indices);

	void TriangulateRiverQuad(
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
		float y1, float y2, float v, bool reversed, Vector3 indices)
	{
		v1.y = v2.y = y1;
		v3.y = v4.y = y2;
		rivers.AddQuad(v1, v2, v3, v4);
		if (reversed)
		{
			rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
		}
		else
		{
			rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
		}
		rivers.AddQuadCellData(indices, weights1, weights2);
	}

	void TriangulateRoad(
		Vector3 center, Vector3 mL, Vector3 mR,
		EdgeVertices e, bool hasRoadThroughCellEdge, float index)
	{
		if (hasRoadThroughCellEdge)
		{
			Vector3 indices;
			indices.x = indices.y = indices.z = index;
			Vector3 mC = Vector3.Lerp(mL, mR, 0.5f);
			TriangulateRoadSegment(
				mL, mC, mR, e.v2, e.v3, e.v4,
				weights1, weights1, indices);
			roads.AddTriangle(center, mL, mC);
			roads.AddTriangle(center, mC, mR);
			roads.AddTriangleUV(
				new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f));
			roads.AddTriangleUV(
				new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f));
			roads.AddTriangleCellData(indices, weights1);
			roads.AddTriangleCellData(indices, weights1);
		}
		else
		{
			TriangulateRoadEdge(center, mL, mR, index);
		}
	}

	void TriangulateRoadEdge(
		Vector3 center, Vector3 mL, Vector3 mR, float index)
	{
		roads.AddTriangle(center, mL, mR);
		roads.AddTriangleUV(
			new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
		Vector3 indices;
		indices.x = indices.y = indices.z = index;
		roads.AddTriangleCellData(indices, weights1);
	}

	void TriangulateRoadSegment(
		Vector3 v1, Vector3 v2, Vector3 v3,
		Vector3 v4, Vector3 v5, Vector3 v6,
		Color w1, Color w2, Vector3 indices)
	{
		roads.AddQuad(v1, v2, v4, v5);
		roads.AddQuad(v2, v3, v5, v6);
		roads.AddQuadUV(0f, 1f, 0f, 0f);
		roads.AddQuadUV(1f, 0f, 0f, 0f);
		roads.AddQuadCellData(indices, w1, w2);
		roads.AddQuadCellData(indices, w1, w2);
	}
}
