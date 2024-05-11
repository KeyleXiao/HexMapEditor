using UnityEngine;

/// <summary>
/// Five-component hash value.
/// </summary>
public struct HexHash
{
	public float a, b, c, d, e;

	/// <summary>
	/// Create a hex hash.
	/// </summary>
	/// <returns>Hash value based on <see cref="UnityEngine.Random"/>.</returns>
	public static HexHash Create()
	{
		HexHash hash;
		hash.a = Random.value * 0.999f;
		hash.b = Random.value * 0.999f;
		hash.c = Random.value * 0.999f;
		hash.d = Random.value * 0.999f;
		hash.e = Random.value * 0.999f;
		return hash;
	}
}
