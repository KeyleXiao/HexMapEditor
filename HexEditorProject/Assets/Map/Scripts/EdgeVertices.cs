using UnityEngine;

/// <summary>
/// Set of five vertex positions describing a cell edge.
/// </summary>
public struct EdgeVertices
{
	public Vector3 v1, v2, v3, v4, v5;

	/// <summary>
	/// Create a straight edge with equidistant vertices
	/// between two corner positions.
	/// </summary>
	/// <param name="corner1">Frist corner.</param>
	/// <param name="corner2">Second corner.</param>
	public EdgeVertices(Vector3 corner1, Vector3 corner2)
	{
		v1 = corner1;
		v2 = Vector3.Lerp(corner1, corner2, 0.25f);
		v3 = Vector3.Lerp(corner1, corner2, 0.5f);
		v4 = Vector3.Lerp(corner1, corner2, 0.75f);
		v5 = corner2;
	}

	/// <summary>
	/// Create a straight edge between two corner positions,
	/// with configurable outer step.
	/// </summary>
	/// <param name="corner1">First corner.</param>
	/// <param name="corner2">Second corner.</param>
	/// <param name="outerStep">First step away from corners,
	/// as fraction of edge.</param>
	public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
	{
		v1 = corner1;
		v2 = Vector3.Lerp(corner1, corner2, outerStep);
		v3 = Vector3.Lerp(corner1, corner2, 0.5f);
		v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
		v5 = corner2;
	}

	/// <summary>
	/// Create edge vertices for a specific terrace step.
	/// </summary>
	/// <param name="a">Edge on first side of the terrace.</param>
	/// <param name="b">Edge on second side of the terrace.</param>
	/// <param name="step">Terrace interpolation step,
	/// 0-<see cref="HexMetrics.terraceSteps"/> inclusive.</param>
	/// <returns>Edge vertices interpolated along terrace.</returns>
	public static EdgeVertices TerraceLerp(
		EdgeVertices a, EdgeVertices b, int step)
	{
		EdgeVertices result;
		result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
		result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
		result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
		result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
		result.v5 = HexMetrics.TerraceLerp(a.v5, b.v5, step);
		return result;
	}
}
