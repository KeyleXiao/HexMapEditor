using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Class containing all data used to generate a mesh
/// while triangulating a hex map.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
	[SerializeField]
	bool useCollider, useCellData, useUVCoordinates, useUV2Coordinates;

	[NonSerialized] List<Vector3> vertices, cellIndices;
	[NonSerialized] List<Color> cellWeights;
	[NonSerialized] List<Vector2> uvs, uv2s;
	[NonSerialized] List<int> triangles;

	Mesh hexMesh;
	MeshCollider meshCollider;

	void Awake()
	{
		GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
		if (useCollider)
		{
			meshCollider = gameObject.AddComponent<MeshCollider>();
		}
		hexMesh.name = "Hex Mesh";
	}

	/// <summary>
	/// Clear all data.
	/// </summary>
	public void Clear()
	{
		hexMesh.Clear();
		vertices = ListPool<Vector3>.Get();
		if (useCellData)
		{
			cellWeights = ListPool<Color>.Get();
			cellIndices = ListPool<Vector3>.Get();
		}
		if (useUVCoordinates)
		{
			uvs = ListPool<Vector2>.Get();
		}
		if (useUV2Coordinates)
		{
			uv2s = ListPool<Vector2>.Get();
		}
		triangles = ListPool<int>.Get();
	}

	/// <summary>
	/// Apply all triangulation data to the underlying mesh.
	/// </summary>
	public void Apply()
	{
		hexMesh.SetVertices(vertices);
		ListPool<Vector3>.Add(vertices);
		if (useCellData)
		{
			hexMesh.SetColors(cellWeights);
			ListPool<Color>.Add(cellWeights);
			hexMesh.SetUVs(2, cellIndices);
			ListPool<Vector3>.Add(cellIndices);
		}
		if (useUVCoordinates)
		{
			hexMesh.SetUVs(0, uvs);
			ListPool<Vector2>.Add(uvs);
		}
		if (useUV2Coordinates)
		{
			hexMesh.SetUVs(1, uv2s);
			ListPool<Vector2>.Add(uv2s);
		}
		hexMesh.SetTriangles(triangles, 0);
		ListPool<int>.Add(triangles);
		hexMesh.RecalculateNormals();
		if (useCollider)
		{
			meshCollider.sharedMesh = hexMesh;
		}
	}

	/// <summary>
	/// Add a triangle, applying perturbation to the positions.
	/// </summary>
	/// <param name="v1">First vertex position.</param>
	/// <param name="v2">Second vertex position.</param>
	/// <param name="v3">Third vertex position.</param>
	public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(HexMetrics.Perturb(v1));
		vertices.Add(HexMetrics.Perturb(v2));
		vertices.Add(HexMetrics.Perturb(v3));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	// <summary>
	/// Add a triangle verbatim, without perturbing the positions.
	/// </summary>
	/// <param name="v1">First vertex position.</param>
	/// <param name="v2">Second vertex position.</param>
	/// <param name="v3">Third vertex position.</param>
	public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
	}

	/// <summary>
	/// Add UV coordinates for a triangle.
	/// </summary>
	/// <param name="uv1">First UV coordinates.</param>
	/// <param name="uv2">Second UV coordinates.</param>
	/// <param name="uv3">Third UV coordinates.</param>
	public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
	{
		uvs.Add(uv1);
		uvs.Add(uv2);
		uvs.Add(uv3);
	}

	/// <summary>
	/// Add UV2 coordinates for a triangle.
	/// </summary>
	/// <param name="uv1">First UV2 coordinates.</param>
	/// <param name="uv2">Second UV2 coordinates.</param>
	/// <param name="uv3">Third UV2 coordinates.</param>
	public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
	{
		uv2s.Add(uv1);
		uv2s.Add(uv2);
		uv2s.Add(uv3);
	}

	/// <summary>
	/// Add cell data for a triangle.
	/// </summary>
	/// <param name="indices">Terrain type indices.</param>
	/// <param name="weights1">First terrain weights.</param>
	/// <param name="weights2">Second terrain weights.</param>
	/// <param name="weights3">Third terrain weights.</param>
	public void AddTriangleCellData(
		Vector3 indices, Color weights1, Color weights2, Color weights3)
	{
		cellIndices.Add(indices);
		cellIndices.Add(indices);
		cellIndices.Add(indices);
		cellWeights.Add(weights1);
		cellWeights.Add(weights2);
		cellWeights.Add(weights3);
	}

	/// <summary>
	/// Add cell data for a triangle.
	/// </summary>
	/// <param name="indices">Terrain type indices.</param>
	/// <param name="weights">Terrain weights, uniform per triangle.</param>
	public void AddTriangleCellData(Vector3 indices, Color weights) =>
		AddTriangleCellData(indices, weights, weights, weights);

	/// <summary>
	/// Add a quad, applying perturbation to the positions.
	/// </summary>
	/// <param name="v1">First vertex position.</param>
	/// <param name="v2">Second vertex position.</param>
	/// <param name="v3">Third vertex position.</param>
	/// <param name="v4">Fourth vertex position.</param>
	public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(HexMetrics.Perturb(v1));
		vertices.Add(HexMetrics.Perturb(v2));
		vertices.Add(HexMetrics.Perturb(v3));
		vertices.Add(HexMetrics.Perturb(v4));
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	/// <summary>
	/// Add a quad verbatim, without perturbing the positions.
	/// </summary>
	/// <param name="v1">First vertex position.</param>
	/// <param name="v2">Second vertex position.</param>
	/// <param name="v3">Third vertex position.</param>
	/// <param name="v4">Fourth vertex position.</param>
	public void AddQuadUnperturbed(
		Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
	{
		int vertexIndex = vertices.Count;
		vertices.Add(v1);
		vertices.Add(v2);
		vertices.Add(v3);
		vertices.Add(v4);
		triangles.Add(vertexIndex);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 1);
		triangles.Add(vertexIndex + 2);
		triangles.Add(vertexIndex + 3);
	}

	/// <summary>
	/// Add UV coordinates for a quad.
	/// </summary>
	/// <param name="uv1">First UV coordinates.</param>
	/// <param name="uv2">Second UV coordinates.</param>
	/// <param name="uv3">Third UV coordinates.</param>
	/// <param name="uv4">Fourth UV coordinates.</param>
	public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
	{
		uvs.Add(uv1);
		uvs.Add(uv2);
		uvs.Add(uv3);
		uvs.Add(uv4);
	}

	/// <summary>
	/// Add UV2 coordinates for a quad.
	/// </summary>
	/// <param name="uv1">First UV2 coordinates.</param>
	/// <param name="uv2">Second UV2 coordinates.</param>
	/// <param name="uv3">Third UV2 coordinates.</param>
	/// <param name="uv4">Fourth UV2 coordinates.</param>
	public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
	{
		uv2s.Add(uv1);
		uv2s.Add(uv2);
		uv2s.Add(uv3);
		uv2s.Add(uv4);
	}

	/// <summary>
	/// Add UV coordaintes for a quad.
	/// </summary>
	/// <param name="uMin">Minimum U coordinate.</param>
	/// <param name="uMax">Maximum U coordinate.</param>
	/// <param name="vMin">Minimum V coordinate.</param>
	/// <param name="vMax">Maximum V coorindate.</param>
	public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
	{
		uvs.Add(new Vector2(uMin, vMin));
		uvs.Add(new Vector2(uMax, vMin));
		uvs.Add(new Vector2(uMin, vMax));
		uvs.Add(new Vector2(uMax, vMax));
	}

	/// <summary>
	/// Add UV2 coordaintes for a quad.
	/// </summary>
	/// <param name="uMin">Minimum U2 coordinate.</param>
	/// <param name="uMax">Maximum U2 coordinate.</param>
	/// <param name="vMin">Minimum V2 coordinate.</param>
	/// <param name="vMax">Maximum V2 coorindate.</param>
	public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
	{
		uv2s.Add(new Vector2(uMin, vMin));
		uv2s.Add(new Vector2(uMax, vMin));
		uv2s.Add(new Vector2(uMin, vMax));
		uv2s.Add(new Vector2(uMax, vMax));
	}

	/// <summary>
	/// Add cell data for a quad.
	/// </summary>
	/// <param name="indices">Terrain type indices.</param>
	/// <param name="weights1">First terrain weights.</param>
	/// <param name="weights2">Second terrain weights.</param>
	/// <param name="weights3">Third terrain weights.</param>
	/// <param name="weights4">Fourth terrain weights.</param>
	public void AddQuadCellData(
		Vector3 indices,
		Color weights1, Color weights2, Color weights3, Color weights4)
	{
		cellIndices.Add(indices);
		cellIndices.Add(indices);
		cellIndices.Add(indices);
		cellIndices.Add(indices);
		cellWeights.Add(weights1);
		cellWeights.Add(weights2);
		cellWeights.Add(weights3);
		cellWeights.Add(weights4);
	}

	/// <summary>
	/// Add cell data for a quad.
	/// </summary>
	/// <param name="indices">Terrain type indices.</param>
	/// <param name="weights1">First and second terrain weights,
	/// both the same.</param>
	/// <param name="weights2">Third and fourth terrain weights,
	/// both the same.</param>
	public void AddQuadCellData(
		Vector3 indices, Color weights1, Color weights2) =>
		AddQuadCellData(indices, weights1, weights1, weights2, weights2);

	/// <summary>
	/// Add cell data for a quad.
	/// </summary>
	/// <param name="indices">Terrain type indices.</param>
	/// <param name="weights">Terrain weights, uniform for entire quad.</param>
	public void AddQuadCellData(Vector3 indices, Color weights) =>
		AddQuadCellData(indices, weights, weights, weights, weights);
}
