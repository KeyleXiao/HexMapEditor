using System.Collections.Generic;

/// <summary>
/// Priority queue to store hex cell indices for the pathfinding algorithm.
/// </summary>
public class HexCellPriorityQueue
{
	readonly List<int> list = new();

	readonly HexGrid grid;

	public HexCellPriorityQueue(HexGrid grid) => this.grid = grid;

	int minimum = int.MaxValue;

	/// <summary>
	/// Add a cell index to the queue.
	/// </summary>
	/// <param name="cellIndex">Cell index to add.</param>
	public void Enqueue(int cellIndex)
	{
		int priority = grid.SearchData[cellIndex].SearchPriority;
		if (priority < minimum)
		{
			minimum = priority;
		}
		while (priority >= list.Count)
		{
			list.Add(-1);
		}
		grid.SearchData[cellIndex].nextWithSamePriority = list[priority];
		list[priority] = cellIndex;
	}

	/// <summary>
	/// Try to remove a cell index from the queue. Fails if the queue is empty.
	/// </summary>
	/// <param name="cellIndex">The dequeued cell index.</param>
	/// <returns>Whether the dequeue succeeded.</returns>
	public bool TryDequeue(out int cellIndex)
	{
		for (; minimum < list.Count; minimum++)
		{
			cellIndex = list[minimum];
			if (cellIndex >= 0)
			{
				list[minimum] = grid.SearchData[cellIndex].nextWithSamePriority;
				return true;
			}
		}
		cellIndex = -1;
		return false;
	}

	/// <summary>
	/// Apply the current priority of a cell index that was previously enqueued.
	/// </summary>
	/// <param name="cellIndex">Cell index to update</param>
	/// <param name="oldPriority">Cell priority before it was changed.</param>
	public void Change(int cellIndex, int oldPriority)
	{
		int current = list[oldPriority];
		int next = grid.SearchData[current].nextWithSamePriority;
		if (current == cellIndex)
		{
			list[oldPriority] = next;
		}
		else
		{
			while (next != cellIndex)
			{
				current = next;
				next = grid.SearchData[current].nextWithSamePriority;
			}
			grid.SearchData[current].nextWithSamePriority =
				grid.SearchData[cellIndex].nextWithSamePriority;
		}
		Enqueue(cellIndex);
	}

	/// <summary>
	/// Clear the queue.
	/// </summary>
	public void Clear()
	{
		list.Clear();
		minimum = int.MaxValue;
	}
}
