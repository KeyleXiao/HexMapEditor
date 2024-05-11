using System.IO;

/// <summary>
/// Flags that describe the contents of a cell.
/// </summary>
[System.Flags]
public enum HexFlags
{
	Empty = 0,

	RoadNE = 0b000001,
	RoadE  = 0b000010,
	RoadSE = 0b000100,
	RoadSW = 0b001000,
	RoadW  = 0b010000,
	RoadNW = 0b100000,

	Roads = 0b111111,

	RiverInNE = 0b000001_000000,
	RiverInE  = 0b000010_000000,
	RiverInSE = 0b000100_000000,
	RiverInSW = 0b001000_000000,
	RiverInW  = 0b010000_000000,
	RiverInNW = 0b100000_000000,

	RiverIn = 0b111111_000000,

	RiverOutNE = 0b000001_000000_000000,
	RiverOutE  = 0b000010_000000_000000,
	RiverOutSE = 0b000100_000000_000000,
	RiverOutSW = 0b001000_000000_000000,
	RiverOutW  = 0b010000_000000_000000,
	RiverOutNW = 0b100000_000000_000000,

	RiverOut = 0b111111_000000_000000,

	River = 0b111111_111111_000000,

	Walled = 0b1_000000_000000_000000,

	Explored   = 0b010_000000_000000_000000,
	Explorable = 0b100_000000_000000_000000
}

public static class HexFlagsExtensions
{
	/// <summary>
	/// Whether any flags of a mask are set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="mask">Mask.</param>
	/// <returns>Whether any of the flags are set.</returns>
	public static bool HasAny(this HexFlags flags, HexFlags mask) =>
		(flags & mask) != 0;

	/// <summary>
	/// Whether all flags of a mask are set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="mask">Mask.</param>
	/// <returns>Whether all of the flags are set.</returns>
	public static bool HasAll(this HexFlags flags, HexFlags mask) =>
		(flags & mask) == mask;

	/// <summary>
	/// Whether no flags of a mask are set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="mask">Mask.</param>
	/// <returns>Whether none of the flags are set.</returns>
	public static bool HasNone(this HexFlags flags, HexFlags mask) =>
		(flags & mask) == 0;

	/// <summary>
	/// Returns flags with bits of the given mask set.
	/// </summary>
	/// <param name="flags"><Flags./param>
	/// <param name="mask">Mask to set.</param>
	/// <returns>The set flags.</returns>
	public static HexFlags With(this HexFlags flags, HexFlags mask) =>
		flags | mask;

	/// <summary>
	/// Returns flags with bits of the given mask cleared.
	/// </summary>
	/// <param name="flags"><Flags./param>
	/// <param name="mask">Mask to clear.</param>
	/// <returns>The cleared flags.</returns>
	public static HexFlags Without(this HexFlags flags, HexFlags mask) =>
		flags & ~mask;

	/// <summary>
	/// Whether the flag for a road in a given direction is set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Road direction.</param>
	/// <returns>Whether the road is set.</returns>
	public static bool HasRoad(this HexFlags flags, HexDirection direction) =>
		flags.Has(HexFlags.RoadNE, direction);

	/// <summary>
	/// Returns the flags with the bit for a given road set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Road direction.</param>
	/// <returns>Flags with the road bit set.</returns>
	public static HexFlags WithRoad(
		this HexFlags flags, HexDirection direction) =>
		flags.With(HexFlags.RoadNE, direction);

	/// <summary>
	/// Returns the flags without the bit for a given road set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Road direction.</param>
	/// <returns>Flags without the road bit set.</returns>
	public static HexFlags WithoutRoad(
		this HexFlags flags, HexDirection direction) =>
		flags.Without(HexFlags.RoadNE, direction);

	/// <summary>
	/// Whether the flag for an incoming river in a given direction is set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Incoming river direction.</param>
	/// <returns>Whether the river is set.</returns>
	public static bool HasRiverIn(
		this HexFlags flags, HexDirection direction) =>
		flags.Has(HexFlags.RiverInNE, direction);

	/// <summary>
	/// Returns the flags with the bit for a given incoming river set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Incoming river direction.</param>
	/// <returns>Flags with the river bit set.</returns>
	public static HexFlags WithRiverIn(
		this HexFlags flags, HexDirection direction) =>
		flags.With(HexFlags.RiverInNE, direction);

	/// <summary>
	/// Returns the flags without the bit for a given incoming river set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Incoming river direction.</param>
	/// <returns>Flags without the river bit set.</returns>
	public static HexFlags WithoutRiverIn(
		this HexFlags flags, HexDirection direction) =>
		flags.Without(HexFlags.RiverInNE, direction);

	/// <summary>
	/// Whether the flag for an outgoing river in a given direction is set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Outgoing river direction.</param>
	/// <returns>Whether the river is set.</returns>
	public static bool HasRiverOut(
		this HexFlags flags, HexDirection direction) =>
		flags.Has(HexFlags.RiverOutNE, direction);

	/// <summary>
	/// Returns the flags with the bit for a given outgoing river set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Outgoing river direction.</param>
	/// <returns>Flags with the river bit set.</returns>
	public static HexFlags WithRiverOut(
		this HexFlags flags, HexDirection direction) =>
		flags.With(HexFlags.RiverOutNE, direction);

	/// <summary>
	/// Returns the flags without the bit for a given outgoing river set.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="direction">Outgoing river direction.</param>
	/// <returns>Flags without the river bit set.</returns>
	public static HexFlags WithoutRiverOut(
		this HexFlags flags, HexDirection direction) =>
		flags.Without(HexFlags.RiverOutNE, direction);

	/// <summary>
	/// Returns the incoming river direction. Only valid if the river exists.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <returns>River direction.</returns>
	public static HexDirection RiverInDirection(this HexFlags flags) =>
		flags.ToDirection(6);

	/// <summary>
	/// Returns the outgoing river direction. Only valid if the river exists.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <returns>River direction.</returns>
	public static HexDirection RiverOutDirection(this HexFlags flags) =>
		flags.ToDirection(12);

	static bool Has(
		this HexFlags flags, HexFlags start, HexDirection direction) =>
		((int)flags & ((int)start << (int)direction)) != 0;

	static HexFlags With(
		this HexFlags flags, HexFlags start, HexDirection direction) =>
		flags | (HexFlags)((int)start << (int)direction);

	static HexFlags Without(
		this HexFlags flags, HexFlags start, HexDirection direction) =>
		flags & ~(HexFlags)((int)start << (int)direction);

	static HexDirection ToDirection(this HexFlags flags, int shift) =>
		(((int)flags >> shift) & 0b111111) switch
		{
			0b000001 => HexDirection.NE,
			0b000010 => HexDirection.E,
			0b000100 => HexDirection.SE,
			0b001000 => HexDirection.SW,
			0b010000 => HexDirection.W,
			_ => HexDirection.NW
		};
	
	/// <summary>
	/// Save the flags.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="writer"><see cref="BinaryWriter"/> to use.</param>
	public static void Save(this HexFlags flags, BinaryWriter writer)
	{
		writer.Write(flags.HasAny(HexFlags.Walled));

		if (flags.HasAny(HexFlags.RiverIn))
		{
			writer.Write((byte)(flags.RiverInDirection() + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		if (flags.HasAny(HexFlags.RiverOut))
		{
			writer.Write((byte)(flags.RiverOutDirection() + 128));
		}
		else
		{
			writer.Write((byte)0);
		}

		writer.Write((byte)(flags & HexFlags.Roads));
		writer.Write(flags.HasAll(HexFlags.Explored | HexFlags.Explorable));
	}

		/// <summary>
	/// Load the cell data.
	/// </summary>
	/// <param name="flags">Flags.</param>
	/// <param name="reader"><see cref="BinaryReader"/> to use.</param>
	/// <param name="header">Header version.</param>
	public static HexFlags Load(
		this HexFlags basis, BinaryReader reader, int header)
	{
		HexFlags flags = basis & HexFlags.Explorable;

		if (reader.ReadBoolean())
		{
			flags = flags.With(HexFlags.Walled);
		}

		byte riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			flags = flags.WithRiverIn((HexDirection)(riverData - 128));
		}

		riverData = reader.ReadByte();
		if (riverData >= 128)
		{
			flags = flags.WithRiverOut((HexDirection)(riverData - 128));
		}

		flags |= (HexFlags)reader.ReadByte();

		if (header >= 3 && reader.ReadBoolean())
		{
			flags = flags.With(HexFlags.Explored);
		}
		return flags;
	}
}
