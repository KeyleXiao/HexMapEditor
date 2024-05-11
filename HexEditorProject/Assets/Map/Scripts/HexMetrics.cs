using UnityEngine;

/// <summary>
/// Constant metrics and utility methods for the hex map.
/// </summary>
public static class HexMetrics
{
	/// <summary>
	/// Ratio of outer to inner radius of a hex cell.
	/// </summary>
	public const float outerToInner = 0.866025404f;

	/// <summary>
	/// Ratio of inner to outer radius of a hex cell.
	/// </summary>
	public const float innerToOuter = 1f / outerToInner;

	/// <summary>
	/// Outer radius of a hex cell.
	/// </summary>
	public const float outerRadius = 10f;

	/// <summary>
	/// Inner radius of a hex cell.
	/// </summary>
	public const float innerRadius = outerRadius * outerToInner;

	/// <summary>
	/// Inner diameter of a hex cell.
	/// </summary>
	public const float innerDiameter = innerRadius * 2f;

	/// <summary>
	/// Factor of the solid uniform region inside a hex cell.
	/// </summary>
	public const float solidFactor = 0.8f;

	/// <summary>
	/// Factor of the blending region inside a hex cell.
	/// </summary>
	public const float blendFactor = 1f - solidFactor;

	/// <summary>
	/// Factor of the solid uniform water region inside a hex cell.
	/// </summary>
	public const float waterFactor = 0.6f;

	/// <summary>
	/// Factor of the water blending region inside a hex cell.
	/// </summary>
	public const float waterBlendFactor = 1f - waterFactor;

	/// <summary>
	/// Height of a single elevation step.
	/// </summary>
	public const float elevationStep = 3f;

	/// <summary>
	/// Amount of terrace levels per slope.
	/// </summary>
	public const int terracesPerSlope = 2;

	/// <summary>
	/// Amount of terraces steps per slope
	/// needed for <see cref="terracesPerSlope"/>.
	/// </summary>
	public const int terraceSteps = terracesPerSlope * 2 + 1;

	/// <summary>
	/// Amount of horizontal terrace steps per slope.
	/// </summary>
	public const float horizontalTerraceStepSize = 1f / terraceSteps;

	/// <summary>
	/// Amount of vertical terrace steps per slope.
	/// </summary>
	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);

	/// <summary>
	/// Strength of cell position terturbation.
	/// </summary>
	public const float cellPerturbStrength = 4f;

	/// <summary>
	/// Strength of vertical elevation perturbation.
	/// </summary>
	public const float elevationPerturbStrength = 1.5f;

	/// <summary>
	/// Offset for stream bed elevation.
	/// </summary>
	public const float streamBedElevationOffset = -1.75f;

	/// <summary>
	/// Offset for water elevation.
	/// </summary>
	public const float waterElevationOffset = -0.5f;

	/// <summary>
	/// Height of walls.
	/// </summary>
	public const float wallHeight = 4f;

	/// <summary>
	/// Vertical wall offset,
	/// negative to prevent them from floating above the surface.
	/// </summary>
	public const float wallYOffset = -1f;

	/// <summary>
	/// Wall thickness.
	/// </summary>
	public const float wallThickness = 0.75f;

	/// <summary>
	/// Wall elevation offset, matching <see cref="verticalTerraceStepSize"/>.
	/// </summary>
	public const float wallElevationOffset = verticalTerraceStepSize;

	/// <summary>
	/// Probability threshold for wall towers.
	/// </summary>
	public const float wallTowerThreshold = 0.5f;

	/// <summary>
	/// Length at which the bridge model is designed.
	/// </summary>
	public const float bridgeDesignLength = 7f;

	/// <summary>
	/// World scale of the noise.
	/// </summary>
	public const float noiseScale = 0.003f;

	/// <summary>
	/// Hex grid chunk size in the X dimension.
	/// </summary>
	public const int chunkSizeX = 5;

	/// <summary>
	/// Hex grid chunk size in the Z dimension.
	/// </summary>
	public const int chunkSizeZ = 5;

	/// <summary>
	/// Size of the hash grid.
	/// </summary>
	public const int hashGridSize = 256;

	/// <summary>
	/// World scale of the hash grid.
	/// </summary>
	public const float hashGridScale = 0.25f;

	static HexHash[] hashGrid;

	static readonly Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};

	static readonly float[][] featureThresholds = {
		new float[] {0.0f, 0.0f, 0.4f},
		new float[] {0.0f, 0.4f, 0.6f},
		new float[] {0.4f, 0.6f, 0.8f}
	};

	/// <summary>
	/// Texture used for sampling noise.
	/// </summary>
	public static Texture2D noiseSource;

	/// <summary>
	/// Sample the noise texture.
	/// </summary>
	/// <param name="position">Sample position.</param>
	/// <returns>Four-component noise sample.</returns>
	public static Vector4 SampleNoise(Vector3 position)
	{
		Vector4 sample = noiseSource.GetPixelBilinear(
			position.x * noiseScale,
			position.z * noiseScale);

		if (Wrapping && position.x < innerDiameter * 1.5f)
		{
			Vector4 sample2 = noiseSource.GetPixelBilinear(
				(position.x + wrapSize * innerDiameter) * noiseScale,
				position.z * noiseScale);
			sample = Vector4.Lerp(
				sample2, sample, position.x * (1f / innerDiameter) - 0.5f);
		}

		return sample;
	}

	/// <summary>
	/// Wrap size of the map,
	/// matching its X size if east-west wrapping is enabled.
	/// </summary>
	public static int wrapSize;

	/// <summary>
	/// Whether east-west map wrapping is enabled.
	/// </summary>
	public static bool Wrapping => wrapSize > 0;

	/// <summary>
	/// Initialize the hash grid.
	/// </summary>
	/// <param name="seed">Seed to use for initialization.</param>
	public static void InitializeHashGrid(int seed)
	{
		hashGrid = new HexHash[hashGridSize * hashGridSize];
		Random.State currentState = Random.state;
		Random.InitState(seed);
		for (int i = 0; i < hashGrid.Length; i++)
		{
			hashGrid[i] = HexHash.Create();
		}
		Random.state = currentState;
	}

	/// <summary>
	/// Sample the hash grid.
	/// </summary>
	/// <param name="position">Sample position</param>
	/// <returns>Sampled <see cref="HexHash"/>.</returns>
	public static HexHash SampleHashGrid(Vector3 position)
	{
		int x = (int)(position.x * hashGridScale) % hashGridSize;
		if (x < 0)
		{
			x += hashGridSize;
		}
		int z = (int)(position.z * hashGridScale) % hashGridSize;
		if (z < 0)
		{
			z += hashGridSize;
		}
		return hashGrid[x + z * hashGridSize];
	}

	/// <summary>
	/// Get the feature threshold levels.
	/// </summary>
	/// <param name="level">Feature level.</param>
	/// <returns>Array containing the thresholds.</returns>
	public static float[] GetFeatureThresholds(int level) =>
		featureThresholds[level];

	/// <summary>
	/// Get the first outer cell corner for a direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The corner on the counter-clockwise side.</returns>
	public static Vector3 GetFirstCorner(HexDirection direction) =>
		corners[(int)direction];

	/// <summary>
	/// Get the second outer cell corner for a direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The corner on the clockwise side.</returns>
	public static Vector3 GetSecondCorner(HexDirection direction) =>
		corners[(int)direction + 1];

	/// <summary>
	/// Get the first inner solid cell corner for a direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The corner on the counter-clockwise side.</returns>
	public static Vector3 GetFirstSolidCorner(HexDirection direction) =>
		corners[(int)direction] * solidFactor;

	/// <summary>
	/// Get the second inner solid cell corner for a direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The corner on the clockwise side.</returns>
	public static Vector3 GetSecondSolidCorner(HexDirection direction) =>
		corners[(int)direction + 1] * solidFactor;

	/// <summary>
	/// Get the middle of the inner solid cell edge for a direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The position in between
	/// the two inner solid cell corners.</returns>
	public static Vector3 GetSolidEdgeMiddle(HexDirection direction) =>
		(corners[(int)direction] + corners[(int)direction + 1]) *
		(0.5f * solidFactor);

	/// <summary>
	/// Get the first inner water cell corner for a direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The corner on the counter-clockwise side.</returns>
	public static Vector3 GetFirstWaterCorner(HexDirection direction) =>
		corners[(int)direction] * waterFactor;

	/// <summary>
	/// Get the second inner water cell corner for a direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The corner on the clockwise side.</returns>
	public static Vector3 GetSecondWaterCorner(HexDirection direction) =>
		corners[(int)direction + 1] * waterFactor;

	/// <summary>
	/// Get the vector needed to bridge to the next cell for a given direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The bridge vector.</returns>
	public static Vector3 GetBridge(HexDirection direction) =>
		(corners[(int)direction] + corners[(int)direction + 1]) * blendFactor;

	/// <summary>
	/// Get the vector needed to bridge to the next water cell
	/// for a given direction.
	/// </summary>
	/// <param name="direction">The desired direction.</param>
	/// <returns>The bridge vector.</returns>
	public static Vector3 GetWaterBridge(HexDirection direction) =>
		(corners[(int)direction] + corners[(int)direction + 1]) *
		waterBlendFactor;

	/// <summary>
	/// Interpolate a position along a terraced edge.
	/// </summary>
	/// <param name="a">Start position.</param>
	/// <param name="b">End position.</param>
	/// <param name="step">Terrace interpolation step.</param>
	/// <returns>The position found by applying terrace interpolation.</returns>
	public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
	{
		float h = step * horizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		float v = ((step + 1) / 2) * verticalTerraceStepSize;
		a.y += (b.y - a.y) * v;
		return a;
	}

	/// <summary>
	/// Interpolate a color along a terraced edge.
	/// </summary>
	/// <param name="a">Start color.</param>
	/// <param name="b">End color.</param>
	/// <param name="step">Terrace interpolation step.</param>
	/// <returns>The color found by applying terrace interpolation.</returns>
	public static Color TerraceLerp(Color a, Color b, int step)
	{
		float h = step * horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

	/// <summary>
	/// Interpolate a position along a wall.
	/// </summary>
	/// <param name="near">Near position.</param>
	/// <param name="far">Far position.</param>
	/// <returns>The middle position with appropriate Y coordinate.</returns>
	public static Vector3 WallLerp(Vector3 near, Vector3 far)
	{
		near.x += (far.x - near.x) * 0.5f;
		near.z += (far.z - near.z) * 0.5f;
		float v = near.y < far.y ?
			wallElevationOffset : (1f - wallElevationOffset);
		near.y += (far.y - near.y) * v + wallYOffset;
		return near;
	}

	/// <summary>
	/// Apply wall thickness.
	/// </summary>
	/// <param name="near">Near position.</param>
	/// <param name="far">Far position.</param>
	/// <returns>Position taking wall thickness into account.</returns>
	public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
	{
		Vector3 offset;
		offset.x = far.x - near.x;
		offset.y = 0f;
		offset.z = far.z - near.z;
		return offset.normalized * (wallThickness * 0.5f);
	}

	/// <summary>
	/// Determine the <see cref="HexEdgeType"/> based on two elevations.
	/// </summary>
	/// <param name="elevation1">First elevation.</param>
	/// <param name="elevation2">Second elevation.</param>
	/// <returns>Matching <see cref="HexEdgeType"/>.</returns>
	public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
	{
		if (elevation1 == elevation2)
		{
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1)
		{
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}

	/// <summary>
	/// Perturn a position.
	/// </summary>
	/// <param name="position">A position.</param>
	/// <returns>The positions with noise applied
	/// to its XZ components.</returns>
	public static Vector3 Perturb(Vector3 position)
	{
		Vector4 sample = SampleNoise(position);
		position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
		position.z += (sample.z * 2f - 1f) * cellPerturbStrength;
		return position;
	}
}
