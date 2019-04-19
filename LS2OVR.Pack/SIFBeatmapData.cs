using System;

namespace LS2OVR.Pack
{

// {"effect":1,"effect_value":2,"notes_attribute":3,"notes_level":1,"position":6,"timing_sec":2.5}
internal struct SIFBeatmapData
{
#pragma warning disable 0649
	public Double timing_sec;
	public Byte position;
	public Int32 effect;
	public Double effect_value;
	public UInt32 notes_attribute;
	public Int32 notes_level;
#pragma warning restore 0649

	internal static NoteMapType ToNoteMapType(Int32 effect)
	{
		switch (effect % 10)
		{
			case 1:
			default:
				return NoteMapType.NormalNote;
			case 2:
				return NoteMapType.TokenNote;
			case 3:
				return NoteMapType.LongNote;
			case 4:
				return NoteMapType.StarNote;
		}
	}

	public static explicit operator BeatmapTimingMap(SIFBeatmapData self)
	{
		Boolean customColor = (self.notes_attribute & 0x0F) == 15;
		Boolean swingNote = self.effect > 10;
		NoteMapType noteType = ToNoteMapType(self.effect);

		return new BeatmapTimingMap()
		{
			Time = self.timing_sec,
			Position = self.position,
			NoteType = noteType,
			// Attribute bit colors: rrrrrrrr rggggggg ggbbbbbb bbb0aaaa
			Attribute = (Byte) (self.notes_attribute & 0x0F),
			RedColor = customColor ? ((Single) ((self.notes_attribute >> 23) & 0x1FF) / 511.0f) : 1.0f,
			GreenColor = customColor ? ((Single) ((self.notes_attribute >> 14) & 0x1FF) / 511.0f) : 1.0f,
			BlueColor = customColor ? ((Single) ((self.notes_attribute >> 5) & 0x1FF) / 511.0f) : 1.0f,
			SwingNote = swingNote,
			SimultaneousNote = false,
			NoteGroup = swingNote ? self.notes_level : 0,
			Length = noteType == NoteMapType.LongNote ? self.effect_value : 0.0
		};
	}
}

}
