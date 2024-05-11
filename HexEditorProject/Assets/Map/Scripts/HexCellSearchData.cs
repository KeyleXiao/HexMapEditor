/// <summary>
/// Cell data for searching.
/// </summary>
[System.Serializable]
public struct HexCellSearchData
{
	/// <summary>
	/// Shortest distance found from start to the cell.
	/// </summary>
	public int distance;
	
	/// <summary>
	/// Linked list reference used for pathfinding.
	/// </summary>
	public int nextWithSamePriority;

	/// <summary>
	/// Index of cell from which the found path entered the cell.
	/// </summary>
	public int pathFrom;

	/// <summary>
	/// Heuristic data used by pathfinding algorithm.
	/// </summary>
	public int heuristic;

	/// <summary>
	/// Search phases data used by pathfinding algorithm.
	/// </summary>
	public int searchPhase;

	/// <summary>
	/// Search priority used by pathfinding algorithm.
	/// </summary>
	public readonly int SearchPriority => distance + heuristic;
}
