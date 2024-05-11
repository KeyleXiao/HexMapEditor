using System.Collections.Generic;

/// <summary>
/// Generic static pool for lists.
/// </summary>
/// <typeparam name="T"></typeparam>
public static class ListPool<T>
{
	static readonly Stack<List<T>> stack = new();

	/// <summary>
	/// Get a pooled list.
	/// </summary>
	/// <returns>The requested list.</returns>
	public static List<T> Get()
	{
		if (stack.Count > 0)
		{
			return stack.Pop();
		}
		return new List<T>();
	}

	/// <summary>
	/// Add a list back to the pool so it can be reused.
	/// </summary>
	/// <param name="list">List to add.</param>
	public static void Add(List<T> list)
	{
		list.Clear();
		stack.Push(list);
	}
}
