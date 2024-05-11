using UnityEngine;

/// <summary>
/// Component that manages the map feature visualizations for a hex grid chunk.
/// </summary>
public class HexFeatureManager : MonoBehaviour
{
	[System.Serializable]
	public struct HexFeatureCollection
	{
		public Transform[] prefabs;

		public readonly Transform Pick(float choice) =>
			prefabs[(int)(choice * prefabs.Length)];
	}

	[SerializeField]
	HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;

	[SerializeField]
	HexMesh walls;

	[SerializeField]
	Transform wallTower, bridge;

	[SerializeField]
	Transform[] special;

	Transform container;

	/// <summary>
	/// Clear all features.
	/// </summary>
	public void Clear()
	{
		if (container)
		{
			Destroy(container.gameObject);
		}
		container = new GameObject("Features Container").transform;
		container.SetParent(transform, false);
		walls.Clear();
	}

	/// <summary>
	/// Apply triangulation.
	/// </summary>
	public void Apply() => walls.Apply();

	Transform PickPrefab(
		HexFeatureCollection[] collection, int level, float hash, float choice)
	{
		if (level > 0)
		{
			float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
			for (int i = 0; i < thresholds.Length; i++)
			{
				if (hash < thresholds[i])
				{
					return collection[i].Pick(choice);
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Add a bridge between two road centers.
	/// </summary>
	/// <param name="roadCenter1">Center position of first road.</param>
	/// <param name="roadCenter2">Center position of second road.</param>
	public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
	{
		roadCenter1 = HexMetrics.Perturb(roadCenter1);
		roadCenter2 = HexMetrics.Perturb(roadCenter2);
		Transform instance = Instantiate(bridge);
		instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
		instance.forward = roadCenter2 - roadCenter1;
		float length = Vector3.Distance(roadCenter1, roadCenter2);
		instance.localScale = new Vector3(
			1f,	1f, length * (1f / HexMetrics.bridgeDesignLength));
		instance.SetParent(container, false);
	}

	/// <summary>
	/// Add a feature for a cell.
	/// </summary>
	/// <param name="cell">Cell with one or more features.</param>
	/// <param name="position">Feature position.</param>
	public void AddFeature(HexCellData cell, Vector3 position)
	{
		if (cell.IsSpecial)
		{
			return;
		}

		HexHash hash = HexMetrics.SampleHashGrid(position);
		Transform prefab = PickPrefab(
			urbanCollections, cell.UrbanLevel, hash.a, hash.d);
		Transform otherPrefab = PickPrefab(
			farmCollections, cell.FarmLevel, hash.b, hash.d);
		float usedHash = hash.a;
		if (prefab)
		{
			if (otherPrefab && hash.b < hash.a)
			{
				prefab = otherPrefab;
				usedHash = hash.b;
			}
		}
		else if (otherPrefab)
		{
			prefab = otherPrefab;
			usedHash = hash.b;
		}
		otherPrefab = PickPrefab(
			plantCollections, cell.PlantLevel, hash.c, hash.d);
		if (prefab)
		{
			if (otherPrefab && hash.c < usedHash)
			{
				prefab = otherPrefab;
			}
		}
		else if (otherPrefab)
		{
			prefab = otherPrefab;
		}
		else
		{
			return;
		}

		Transform instance = Instantiate(prefab);
		position.y += instance.localScale.y * 0.5f;
		instance.SetLocalPositionAndRotation(
			HexMetrics.Perturb(position),
			Quaternion.Euler(0f, 360f * hash.e, 0f));
		instance.SetParent(container, false);
	}

	/// <summary>
	/// Add a special feature for a cell.
	/// </summary>
	/// <param name="cell">Cell with special feature.</param>
	/// <param name="position">Feature position.</param>
	public void AddSpecialFeature(HexCellData cell, Vector3 position)
	{
		HexHash hash = HexMetrics.SampleHashGrid(position);
		Transform instance = Instantiate(special[cell.SpecialIndex - 1]);
		instance.SetLocalPositionAndRotation(
			HexMetrics.Perturb(position),
			Quaternion.Euler(0f, 360f * hash.e, 0f));
		instance.SetParent(container, false);
	}

	/// <summary>
	/// Add a wall along the edge between two cells.
	/// </summary>
	/// <param name="near">Near edge.</param>
	/// <param name="nearCell">Near cell.</param>
	/// <param name="far">Far edge.</param>
	/// <param name="farCell">Far cell.</param>
	/// <param name="hasRiver">Whether a river crosses the edge.</param>
	/// <param name="hasRoad">Whether a road crosses the edge.</param>
	public void AddWall(
		EdgeVertices near, HexCellData nearCell,
		EdgeVertices far, HexCellData farCell,
		bool hasRiver, bool hasRoad)
	{
		if (nearCell.Walled != farCell.Walled &&
			!nearCell.IsUnderwater && !farCell.IsUnderwater &&
			nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
		{
			AddWallSegment(near.v1, far.v1, near.v2, far.v2);
			if (hasRiver || hasRoad)
			{
				AddWallCap(near.v2, far.v2);
				AddWallCap(far.v4, near.v4);
			}
			else
			{
				AddWallSegment(near.v2, far.v2, near.v3, far.v3);
				AddWallSegment(near.v3, far.v3, near.v4, far.v4);
			}
			AddWallSegment(near.v4, far.v4, near.v5, far.v5);
		}
	}

	/// <summary>
	/// Add a call though the corner where three cells meet.
	/// </summary>
	/// <param name="c1">First corner position.</param>
	/// <param name="cell1">First corner cell.</param>
	/// <param name="c2">Second corner position.</param>
	/// <param name="cell2">Second corner cell.</param>
	/// <param name="c3">Third corner position.</param>
	/// <param name="cell3">Third corner cell.</param>
	public void AddWall(
		Vector3 c1, HexCellData cell1,
		Vector3 c2, HexCellData cell2,
		Vector3 c3, HexCellData cell3)
	{
		if (cell1.Walled)
		{
			if (cell2.Walled)
			{
				if (!cell3.Walled)
				{
					AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
				}
			}
			else if (cell3.Walled)
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
			else
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
		}
		else if (cell2.Walled)
		{
			if (cell3.Walled)
			{
				AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
			}
			else
			{
				AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
			}
		}
		else if (cell3.Walled)
		{
			AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
		}
	}

	void AddWallSegment(
		Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight,
		bool addTower = false)
	{
		nearLeft = HexMetrics.Perturb(nearLeft);
		farLeft = HexMetrics.Perturb(farLeft);
		nearRight = HexMetrics.Perturb(nearRight);
		farRight = HexMetrics.Perturb(farRight);

		Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
		Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

		Vector3 leftThicknessOffset =
			HexMetrics.WallThicknessOffset(nearLeft, farLeft);
		Vector3 rightThicknessOffset =
			HexMetrics.WallThicknessOffset(nearRight, farRight);

		float leftTop = left.y + HexMetrics.wallHeight;
		float rightTop = right.y + HexMetrics.wallHeight;

		Vector3 v1, v2, v3, v4;
		v1 = v3 = left - leftThicknessOffset;
		v2 = v4 = right - rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);

		Vector3 t1 = v3, t2 = v4;

		v1 = v3 = left + leftThicknessOffset;
		v2 = v4 = right + rightThicknessOffset;
		v3.y = leftTop;
		v4.y = rightTop;
		walls.AddQuadUnperturbed(v2, v1, v4, v3);

		walls.AddQuadUnperturbed(t1, t2, v3, v4);

		if (addTower)
		{
			Transform towerInstance = Instantiate(wallTower);
			towerInstance.transform.localPosition = (left + right) * 0.5f;
			Vector3 rightDirection = right - left;
			rightDirection.y = 0f;
			towerInstance.transform.right = rightDirection;
			towerInstance.SetParent(container, false);
		}
	}

	void AddWallSegment(
		Vector3 pivot, HexCellData pivotCell,
		Vector3 left, HexCellData leftCell,
		Vector3 right, HexCellData rightCell)
	{
		if (pivotCell.IsUnderwater)
		{
			return;
		}

		bool hasLeftWall = !leftCell.IsUnderwater &&
			pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
		bool hasRighWall = !rightCell.IsUnderwater &&
			pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

		if (hasLeftWall)
		{
			if (hasRighWall)
			{
				bool hasTower = false;
				if (leftCell.Elevation == rightCell.Elevation)
				{
					HexHash hash = HexMetrics.SampleHashGrid(
						(pivot + left + right) * (1f / 3f));
					hasTower = hash.e < HexMetrics.wallTowerThreshold;
				}
				AddWallSegment(pivot, left, pivot, right, hasTower);
			}
			else if (leftCell.Elevation < rightCell.Elevation)
			{
				AddWallWedge(pivot, left, right);
			}
			else
			{
				AddWallCap(pivot, left);
			}
		}
		else if (hasRighWall)
		{
			if (rightCell.Elevation < leftCell.Elevation)
			{
				AddWallWedge(right, pivot, left);
			}
			else
			{
				AddWallCap(right, pivot);
			}
		}
	}

	void AddWallCap(Vector3 near, Vector3 far)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = center.y + HexMetrics.wallHeight;
		walls.AddQuadUnperturbed(v1, v2, v3, v4);
	}

	void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
	{
		near = HexMetrics.Perturb(near);
		far = HexMetrics.Perturb(far);
		point = HexMetrics.Perturb(point);

		Vector3 center = HexMetrics.WallLerp(near, far);
		Vector3 thickness = HexMetrics.WallThicknessOffset(near, far);

		Vector3 v1, v2, v3, v4;
		Vector3 pointTop = point;
		point.y = center.y;

		v1 = v3 = center - thickness;
		v2 = v4 = center + thickness;
		v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;

		walls.AddQuadUnperturbed(v1, point, v3, pointTop);
		walls.AddQuadUnperturbed(point, v2, pointTop, v4);
		walls.AddTriangleUnperturbed(pointTop, v3, v4);
	}
}
