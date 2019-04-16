// Copyright(c) 2040 Dark Energy Processor
// 
// This software is provided 'as-is', without any express or implied
// warranty.In no event will the authors be held liable for any damages
// arising from the use of this software.
// 
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software.If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using fNbt;

namespace LS2OVR
{

/// <summary>
/// Note type.
/// </summary>
public enum NoteMapType
{
	NormalNote,
	TokenNote,
	StarNote,
	LongNote
};

/// <summary>
/// Note hit points.
/// </summary>
public struct BeatmapTimingMap
{
	/// <summary>
	/// Note timing in seconds.
	/// </summary>
	public Double Time {get; set;}
	/// <summary>
	/// Note attribute ID. If it's 15 then it's custom-colored note.
	/// </summary>
	public Byte Attribute {get; set;}
	/// <summary>
	/// Red color of the note. It's 1.0 if attribute is not 15.
	/// </summary>
	public Single RedColor {get; set; }
	/// <summary>
	/// Blue color of the note. It's 1.0 if attribute is not 15.
	/// </summary>
	public Single BlueColor {get; set; }
	/// <summary>
	/// Green color of the note. It's 1.0 if attribute is not 15.
	/// </summary>
	public Single GreenColor {get; set;}
	private Byte _position;
	/// <summary>
	/// Note lane position in range 1-9.
	/// </summary>
	public Byte Position {
		get {
			return _position;
		}
		set {
			if (value <= 0 || value > 9)
				throw new ArgumentOutOfRangeException("value", "must be in range 1-9");
			_position = value;
		}
	}
	/// <summary>
	/// Note type.
	/// </summary>
	public NoteMapType NoteType {get; set;}
	private Boolean _swing;
	/// <summary>
	/// Swing note flag.
	/// </summary>
	public Boolean SwingNote {
		get {
			return _swing;
		}
		set {
			if (NoteType != NoteMapType.StarNote)
				_swing = value;
		}
	}
	/// <summary>
	/// Simultaneous note mark.
	/// </summary>
	public Boolean SimultaneousNote {get; set;}
	/// <summary>
	/// Note group. Has meaningful value if swing note flag is set.
	/// </summary>
	public Int32 NoteGroup {get; set;}
	/// <summary>
	/// Long note length. It's 0.0 if note type is not long note.
	/// </summary>
	public Double Length {get; set;}

	/// <summary>
	/// Create new BeatmapTimingMap object from specified NbtCompound.
	/// </summary>
	/// <param name="data">TAG_Compound NBT data.</param>
	/// <exception cref="System.InvalidOperationException">Thrown when needed field(s) has invalid value.</exception>
	/// <exception cref="System.InvalidCastException">Thrown when needed field(s) is missing.</exception>
	public BeatmapTimingMap(NbtCompound data)
	{
		// "time" field
		Time = data.Get<NbtDouble>("time").DoubleValue;
		if (Time <= 0)
			throw new InvalidOperationException("\"time\" is 0 or negative");
		else if (Time != Time)
			throw new InvalidOperationException("\"time\" is nan");
		else if (Time == Double.PositiveInfinity)
			throw new InvalidOperationException("\"time\" is infinity");

		// "attribute" field
		UInt32 tempAttr = (UInt32) data.Get<NbtInt>("attribute").IntValue;

		if ((tempAttr & 0x0F) == 15)
		{
			Attribute = 15;
			BlueColor = ((Single) ((tempAttr >> 4) & 0x1FF)) / 511.0f * 2.0f;
			GreenColor = ((Single) ((tempAttr >> 13) & 0x1FF)) / 511.0f * 2.0f;
			RedColor = ((Single) ((tempAttr >> 22) & 0x1FF)) / 511.0f * 2.0f;
		}
		else
		{
			Attribute = (Byte) tempAttr;
			RedColor = GreenColor = BlueColor = 1.0f;
		}
		
		// "position" field
		_position = data.Get<NbtByte>("position").ByteValue;
		if (_position <= 0 || _position > 9)
			throw new InvalidOperationException("\"position\" is out of range");
		
		// "flags" field
		Byte flags = data.Get<NbtByte>("flags").ByteValue;
		Boolean isSwing = (flags & 4) > 0;

		switch (flags & 3)
		{
			case 0:
			{
				NoteType = NoteMapType.NormalNote;
				break;
			}
			case 1:
			{
				NoteType = NoteMapType.TokenNote;
				break;
			}
			case 2:
			{
				NoteType = NoteMapType.StarNote;
				isSwing = false; // regardless
				break;
			}
			case 3:
			{
				NoteType = NoteMapType.LongNote;
				break;
			}
			default:
				throw new InvalidOperationException("SHOULD NEVER THROW THIS EXCEPTION!");
		}

		SimultaneousNote = (flags & 8) > 0;
		_swing = isSwing;
		if (isSwing)
		{
			NoteGroup = data.Get<NbtInt>("noteGroup").IntValue;
			if (NoteGroup <= 0)
				throw new InvalidOperationException("\"noteGroup\" is 0 or negative");
		}
		else
			NoteGroup = 0;
		
		if (NoteType == NoteMapType.LongNote)
		{
			Length = data.Get<NbtDouble>("length").DoubleValue;
			if (Length <= 0)
				throw new InvalidOperationException("\"length\" is 0 or negative");
			else if (Length != Length)
				throw new InvalidOperationException("\"length\" is nan");
			else if (Length == Double.PositiveInfinity)
				throw new InvalidOperationException("\"length\" is infinity");
		}
		else
			Length = 0;
	}

	public static explicit operator NbtCompound(BeatmapTimingMap self)
	{
		Int32 encodedAttribute = self.Attribute;
		if (encodedAttribute == 15)
		{
			Int32 temp = ((Int32) (self.BlueColor * 511.0f)) & 0x1FF;
			encodedAttribute |= temp << 4;
			temp = ((Int32) (self.GreenColor * 511.0f)) & 0x1FF;
			encodedAttribute |= temp << 13;
			temp = ((Int32) (self.RedColor * 511.0f)) & 0x1FF;
			encodedAttribute |= temp << 22;
		}
		Int32 encodedFlags = (Int32) self.NoteType;
		encodedFlags |= (self.SwingNote ? 4 : 0) | (self.SimultaneousNote ? 8 : 0);

		NbtCompound data = new NbtCompound() {
			new NbtDouble("time", self.Time),
			new NbtInt("attribute", encodedAttribute),
			new NbtByte("position", self.Position),
			new NbtByte("flags", (Byte) encodedFlags)
		};

		if (self.SwingNote)
			data.Add(new NbtInt("noteGroup", self.NoteGroup));

		if (self.NoteType == NoteMapType.LongNote)
			data.Add(new NbtDouble("length", self.Length));
		
		return data;
	}
}

}
