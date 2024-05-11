using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Component representing a unit that occupies a cell of the hex map.
/// </summary>
public class HexUnit : MonoBehaviour
{
	const float rotationSpeed = 180f;
	const float travelSpeed = 4f;

	public static HexUnit unitPrefab;

	public HexGrid Grid { get; set; }

	/// <summary>
	/// Cell that the unit occupies.
	/// </summary>
	public HexCell Location
	{
		get => Grid.GetCell(locationCellIndex);
		set
		{
			if (locationCellIndex >= 0)
			{
				HexCell location = Grid.GetCell(locationCellIndex);
				Grid.DecreaseVisibility(location, VisionRange);
				location.Unit = null;
			}
			locationCellIndex = value.Index;
			value.Unit = this;
			Grid.IncreaseVisibility(value, VisionRange);
			transform.localPosition = value.Position;
			Grid.MakeChildOfColumn(transform, value.ColumnIndex);
		}
	}

	int locationCellIndex = -1, currentTravelLocationCellIndex = -1;

	/// <summary>
	/// Orientation that the unit is facing.
	/// </summary>
	public float Orientation
	{
		get => orientation;
		set
		{
			orientation = value;
			transform.localRotation = Quaternion.Euler(0f, value, 0f);
		}
	}

	/// <summary>
	/// Speed of the unit, in cells per turn.
	/// </summary>
	public int Speed => 24;

	/// <summary>
	/// Vision range of the unit, in cells.
	/// </summary>
	public int VisionRange => 3;

	float orientation;

	List<int> pathToTravel;

	/// <summary>
	/// Validate the position of the unit.
	/// </summary>
	public void ValidateLocation() =>
		transform.localPosition = Grid.GetCell(locationCellIndex).Position;

	/// <summary>
	/// Checl whether a cell is a valid destination for the unit.
	/// </summary>
	/// <param name="cell">Cell to check.</param>
	/// <returns>Whether the unit could occupy the cell.</returns>
	public bool IsValidDestination(HexCell cell) =>
		cell.IsExplored && !cell.IsUnderwater && !cell.Unit;

	/// <summary>
	/// Travel along a path.
	/// </summary>
	/// <param name="path">List of cells that describe a valid path.</param>
	public void Travel(List<int> path)
	{
		HexCell location = Grid.GetCell(locationCellIndex);
		location.Unit = null;
		location = Grid.GetCell(path[^1]);
		locationCellIndex = location.Index;
		location.Unit = this;
		pathToTravel = path;
		StopAllCoroutines();
		StartCoroutine(TravelPath());
	}

	IEnumerator TravelPath()
	{
		Vector3 a, b, c = Grid.GetCell(pathToTravel[0]).Position;
		yield return LookAt(Grid.GetCell(pathToTravel[1]).Position);

		if (currentTravelLocationCellIndex < 0)
		{
			currentTravelLocationCellIndex = pathToTravel[0];
		}
		HexCell currentTravelLocation = Grid.GetCell(
			currentTravelLocationCellIndex);
		Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
		int currentColumn = currentTravelLocation.ColumnIndex;

		float t = Time.deltaTime * travelSpeed;
		for (int i = 1; i < pathToTravel.Count; i++)
		{
			currentTravelLocation = Grid.GetCell(pathToTravel[i]);
			currentTravelLocationCellIndex = currentTravelLocation.Index;
			a = c;
			b = Grid.GetCell(pathToTravel[i - 1]).Position;

			int nextColumn = currentTravelLocation.ColumnIndex;
			if (currentColumn != nextColumn)
			{
				if (nextColumn < currentColumn - 1)
				{
					a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
					b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
				}
				else if (nextColumn > currentColumn + 1)
				{
					a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
					b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
				}
				Grid.MakeChildOfColumn(transform, nextColumn);
				currentColumn = nextColumn;
			}

			c = (b + currentTravelLocation.Position) * 0.5f;
			Grid.IncreaseVisibility(Grid.GetCell(pathToTravel[i]), VisionRange);

			for (; t < 1f; t += Time.deltaTime * travelSpeed)
			{
				transform.localPosition = Bezier.GetPoint(a, b, c, t);
				Vector3 d = Bezier.GetDerivative(a, b, c, t);
				d.y = 0f;
				transform.localRotation = Quaternion.LookRotation(d);
				yield return null;
			}
			Grid.DecreaseVisibility(Grid.GetCell(pathToTravel[i]), VisionRange);
			t -= 1f;
		}
		currentTravelLocationCellIndex = -1;

		HexCell location = Grid.GetCell(locationCellIndex);
		a = c;
		b = location.Position;
		c = b;
		Grid.IncreaseVisibility(location, VisionRange);
		for (; t < 1f; t += Time.deltaTime * travelSpeed)
		{
			transform.localPosition = Bezier.GetPoint(a, b, c, t);
			Vector3 d = Bezier.GetDerivative(a, b, c, t);
			d.y = 0f;
			transform.localRotation = Quaternion.LookRotation(d);
			yield return null;
		}

		transform.localPosition = location.Position;
		orientation = transform.localRotation.eulerAngles.y;
		ListPool<int>.Add(pathToTravel);
		pathToTravel = null;
	}

	IEnumerator LookAt(Vector3 point)
	{
		if (HexMetrics.Wrapping)
		{
			float xDistance = point.x - transform.localPosition.x;
			if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize)
			{
				point.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
			}
			else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize)
			{
				point.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
			}
		}

		point.y = transform.localPosition.y;
		Quaternion fromRotation = transform.localRotation;
		Quaternion toRotation =
			Quaternion.LookRotation(point - transform.localPosition);
		float angle = Quaternion.Angle(fromRotation, toRotation);

		if (angle > 0f)
		{
			float speed = rotationSpeed / angle;
			for (float t = Time.deltaTime * speed;
				t < 1f; t += Time.deltaTime * speed)
			{
				transform.localRotation = Quaternion.Slerp(
					fromRotation, toRotation, t);
				yield return null;
			}
		}

		transform.LookAt(point);
		orientation = transform.localRotation.eulerAngles.y;
	}

	/// <summary>
	/// Get the movement cost of moving from one cell to another.
	/// </summary>
	/// <param name="fromCell">Cell to move from.</param>
	/// <param name="toCell">Cell to move to.</param>
	/// <param name="direction">Movement direction.</param>
	/// <returns></returns>
	public int GetMoveCost(
		HexCell fromCell, HexCell toCell, HexDirection direction)
	{
		if (!IsValidDestination(toCell))
		{
			return -1;
		}
		HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
		if (edgeType == HexEdgeType.Cliff)
		{
			return -1;
		}
		int moveCost;
		if (fromCell.HasRoadThroughEdge(direction))
		{
			moveCost = 1;
		}
		else if (fromCell.Walled != toCell.Walled)
		{
			return -1;
		}
		else
		{
			moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
			moveCost +=
				toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
		}
		return moveCost;
	}

	/// <summary>
	/// Terminate the unit.
	/// </summary>
	public void Die()
	{
		HexCell location = Grid.GetCell(locationCellIndex);
		Grid.DecreaseVisibility(location, VisionRange);
		location.Unit = null;
		Destroy(gameObject);
	}

	/// <summary>
	/// Save the unit data.
	/// </summary>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public void Save(BinaryWriter writer)
	{
		//location.Coordinates.Save(writer);
		Grid.GetCell(locationCellIndex).Coordinates.Save(writer);
		writer.Write(orientation);
	}

	/// <summary>
	/// Load the unit data.
	/// </summary>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="grid"><see cref="HexGrid"/> to add the unit to.</param>
	public static void Load(BinaryReader reader, HexGrid grid)
	{
		HexCoordinates coordinates = HexCoordinates.Load(reader);
		float orientation = reader.ReadSingle();
		grid.AddUnit(
			Instantiate(unitPrefab), grid.GetCell(coordinates), orientation);
	}

	void OnEnable()
	{
		if (locationCellIndex >= 0)
		{
			HexCell location = Grid.GetCell(locationCellIndex);
			transform.localPosition = location.Position;
			if (currentTravelLocationCellIndex >= 0)
			{
				HexCell currentTravelLocation =
					Grid.GetCell(currentTravelLocationCellIndex);
				Grid.IncreaseVisibility(location, VisionRange);
				Grid.DecreaseVisibility(currentTravelLocation, VisionRange);
				currentTravelLocationCellIndex = -1;
			}
		}
	}
}
