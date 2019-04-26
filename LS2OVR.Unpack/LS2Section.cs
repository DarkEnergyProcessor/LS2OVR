using System;
using System.IO;
using System.Collections.Generic;

namespace LS2OVR.Unpack.LS2
{

internal class LS2Section
{
	internal LS2Section() {}
}

internal class MTDTSection: LS2Section
{
	public static readonly Byte[] FourCC = {77, 84, 68, 84};

	public Byte Flags {get; set;} = 0;
	public Byte Star {get; set;} = 0;
	public Byte StarRandom {get; set;} = 0;
	public String SongName {get; set;} = String.Empty;
	public String PreferredAudioFilename {get; set;} = String.Empty;
	public Int32[] ScoreInfo {get; set;} = null;
	public Int32[] ComboInfo {get; set;} = null;
	
	public static MTDTSection Load(BinaryReader reader, Boolean v2)
	{
		MTDTSection ret = new MTDTSection() {Flags = reader.ReadByte()};
		Byte starInfo = reader.ReadByte();

		if ((ret.Flags & 0x04) > 0)
		{
			ret.Star = (Byte) (starInfo & 0x0F);
			if (ret.Star == 0)
				throw new InvalidBeatmapFileException("star difficulty is zero");
		}

		if ((ret.Flags & 0x08) > 0)
		{
			ret.StarRandom = (Byte) (starInfo >> 4);
			if (ret.StarRandom == 0)
				throw new InvalidBeatmapFileException("star random difficulty is zero");
		}

		ret.SongName = Util.ReadLS2String(reader);
		ret.PreferredAudioFilename = Util.ReadLS2String(reader);

		Int32[] scoreInfo = new Int32[4];
		for (Int32 i = 0; i < 4; i++)
			scoreInfo[i] = (Int32) Util.ReadUInt32LE(reader);
		
		if ((ret.Flags & 0x01) > 0)
		{
			// Verify score info
			for (Int32 i = 0; i < 4; i++)
			{
				if (scoreInfo[i] == 0)
					throw new InvalidBeatmapFileException("score info flag is set but it's 0");
				else if (i > 0)
				{
					if (scoreInfo[i] < scoreInfo[i - 1])
						throw new InvalidBeatmapFileException("invalid score info");
				}
			}

			ret.ScoreInfo = scoreInfo;
		}
		
		Int32[] comboInfo = new Int32[4];
		for (Int32 i = 0; i < 4; i++)
			comboInfo[i] = (Int32) Util.ReadUInt32LE(reader);

		if ((ret.Flags & 0x02) > 0)
		{
			// Verify combo info
			for (Int32 i = 0; i < 4; i++)
			{
				if (comboInfo[i] == 0)
					throw new InvalidBeatmapFileException("combo info flag is set but it's 0");
				else if (i > 0)
				{
					if (comboInfo[i] < comboInfo[i - 1])
						throw new InvalidBeatmapFileException("invalid combo info");
				}
			}

			ret.ComboInfo = comboInfo;
		}

		return ret;
	}

	internal MTDTSection() {}
};

internal class BMPMSection: LS2Section
{
	public static readonly Byte[] FourCCBMPM = {66, 77, 80, 77};
	public static readonly Byte[] FourCCBMPT = {66, 77, 80, 84};

	public List<BeatmapTimingMap> TimingMap {get; set;}

	internal static List<BeatmapTimingMap> ReadBeatmapData(BinaryReader reader, Boolean v2, UInt16 ppqn = 0, UInt32 bpm = 0)
	{
		if (ppqn > 0 || bpm > 0)
			throw new NotImplementedException("BMPT is currently not implemented");
		
		List<BeatmapTimingMap> map = new List<BeatmapTimingMap>();
		UInt32 count = Util.ReadUInt32LE(reader);

		for (UInt32 i = 0; i < count; i++)
		{
			UInt32 timingMsec = Util.ReadUInt32LE(reader);
			UInt32 attribute = Util.ReadUInt32LE(reader);
			UInt32 effect = Util.ReadUInt32LE(reader);

			if (attribute == 0xFFFFFFFF)
				throw new NotImplementedException("BMPT is currently not implemented");
			
			BeatmapTimingMap p = new BeatmapTimingMap()
			{
				Time = ((Double) timingMsec) * 0.001,
				Attribute = (Byte) (attribute & 0x0F),
				Position = (Byte) (effect & 15),
				NoteType = NoteMapType.NormalNote,
				Length = 0,
				NoteGroup = 0,
			};
			
			if (p.Attribute == 15)
			{
				p.RedColor = (Single) (((Double) ((attribute >> 23) & 0x1FF)) / 511.0);
				p.GreenColor = (Single) (((Double) ((attribute >> 14) & 0x1FF)) / 511.0);
				p.BlueColor = (Single) (((Double) ((attribute >> 5) & 0x1FF)) / 511.0);
			}
			
			if (v2)
			{
				p.SwingNote = (attribute & 0x10) > 0;

				switch ((effect >> 4) & 0x03)
				{
					case 0:
					{
						p.NoteType = NoteMapType.NormalNote;
						break;
					}
					case 1:
					{
						p.NoteType = NoteMapType.TokenNote;
						break;
					}
					case 2:
					{
						p.NoteType = NoteMapType.LongNote;
						p.Length = ((Double) ((effect >> 6) & 0x3FFFF)) * 0.001;
						break;
					}
					case 3:
					{
						p.NoteType = NoteMapType.StarNote;
						p.SwingNote = false;
						break;
					}
				}
			}
			else
			{
				if ((effect >> 31) > 0)
				{
					// Long note
					p.Length = ((Double) ((effect & 0x3FFFFFF0) >> 4)) * 0.001;
					p.NoteType = NoteMapType.LongNote;
				}
				else
				{
					Boolean isToken = (effect & 0x10) > 0;
					Boolean isStar = (effect & 0x20) > 0;

					if (isToken && isStar == false)
						p.NoteType = NoteMapType.TokenNote;
					else if (isToken == false && isStar)
						p.NoteType = NoteMapType.StarNote;
					else if (isToken && isStar)
					{
						p.SwingNote = true;
						p.NoteGroup = 2; // Uh, unknown
					}
				}
			}

			map.Add(p);
		}

		return map;
	}

	public static BMPMSection LoadBMPM(BinaryReader reader, Boolean v2)
	{
		return new BMPMSection() {
			TimingMap = ReadBeatmapData(reader, v2)
		};
	}

	public static BMPMSection LoadBMPT(BinaryReader reader, Boolean v2)
	{
		throw new NotImplementedException("BMPT is currently not implemented");
	}

	public static implicit operator List<BeatmapTimingMap>(BMPMSection self)
	{
		return self.TimingMap;
	}

	internal BMPMSection() {}
};

internal class SCRISection: LS2Section
{
	public static readonly Byte[] FourCC = {83, 67, 82, 73};

	public Int32[] ScoreInfo {get; set;}

	public static SCRISection Load(BinaryReader reader, Boolean v2)
	{
		SCRISection ret = new SCRISection() {ScoreInfo = new Int32[4]};

		for (Int32 i = 0; i < 4; i++)
		{
			Int32 v = (Int32) Util.ReadUInt32LE(reader);

			if (i > 0)
			{
				if (ret.ScoreInfo[i - 1] >= v)
					throw new InvalidBeatmapFileException("invalid score info");
			}

			ret.ScoreInfo[i] = v;
		}

		return ret;
	}

	public static implicit operator Int32[](SCRISection self)
	{
		return self.ScoreInfo;
	}

	internal SCRISection() {}
};

internal class SRYLSection: LS2Section
{
	public static readonly Byte[] FourCC = {83, 82, 89, 76};

	public Byte[] StoryboardData;

	public static SRYLSection Load(BinaryReader reader, Boolean v2)
	{
		SRYLSection ret = new SRYLSection();
		Byte[] data = Util.ReadLS2ByteArray(reader);

		if (
			// what kind of storyboard with len of 2?
			data.Length < 2 ||
			// GZip header
			(data[0] == 0x1f && data[1] == 0x8b) ||
			// Zlib header
			(data[0] == 0x78 && (data[1] == 0x01 || data[2] == 0x9C || data[2] == 0xDA))
		)
			throw new NotImplementedException("compressed storyboard is not supported");

		ret.StoryboardData = data;
		return ret;
	}

	public static implicit operator Byte[](SRYLSection self)
	{
		return self.StoryboardData;
	}

	internal SRYLSection() {}
};

internal class UIMGSection: LS2Section
{
	public static readonly Byte[] FourCC = {85, 73, 77, 71};

	public Byte Index {get; set;}
	public Byte[] ImageData {get; set;}

	public static UIMGSection Load(BinaryReader reader, Boolean v2)
	{
		return new UIMGSection {
			Index = reader.ReadByte(),
			ImageData = Util.ReadLS2ByteArray(reader)
		};
	}

	internal UIMGSection() {}
};

internal class UNITSection: LS2Section
{
	public static readonly Byte[] FourCC = {85, 78, 73, 84};

	public Dictionary<Byte, Byte> Definition {get; set;}

	public static UNITSection Load(BinaryReader reader, Boolean v2)
	{
		Byte count = reader.ReadByte();
		UNITSection ret = new UNITSection() {Definition = new Dictionary<Byte, Byte>()};

		for (Byte i = 0; i < count; i++)
		{
			// Can't use "ret.Definition.Add" directly
			// because the argument order can be undefined.
			Byte k = reader.ReadByte();
			Byte v = reader.ReadByte();
			ret.Definition.Add(k, v);
		}

		return ret;
	}

	internal UNITSection() {}
};

internal class DATASection: LS2Section
{
	public static readonly Byte[] FourCC = {68, 65, 84, 65};

	public String Filename {get; set;}
	public Byte[] Data {get; set;}

	public static DATASection Load(BinaryReader reader, Boolean v2)
	{
		return new DATASection() {
			Filename = Util.ReadLS2String(reader),
			Data = Util.ReadLS2ByteArray(reader)
		};
	}

	internal DATASection() {}
};

internal class ADIOSection: LS2Section
{
	public static readonly Byte[] FourCC = {65, 68, 73, 79};

	public Boolean NonStandardExtension {get; set;}
	public String Extension {get; set;}
	public Byte[] Data {get; set;}

	public static ADIOSection Load(BinaryReader reader, Boolean v2)
	{
		ADIOSection ret = new ADIOSection() {NonStandardExtension = false};
		Byte ext = reader.ReadByte();

		switch (ext)
		{
			default:
				throw new InvalidBeatmapFileException("Unknown audio type");
			case 0:
			{
				ret.Extension = "wav";
				break;
			}
			case 1:
			{
				ret.Extension = "ogg";
				break;
			}
			case 2:
			{
				ret.Extension = "mp3";
				break;
			}
			case 15:
			{
				Int32 extLen = ext >> 4;
				Int32 length = ((Int32) Util.ReadUInt32LE(reader)) - extLen;
				ret.NonStandardExtension = true;
				ret.Extension = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(extLen));
				ret.Data = reader.ReadBytes(length);
				return ret;
			}
		}

		ret.Data = Util.ReadLS2ByteArray(reader);
		return ret;
	}

	internal ADIOSection() {}
};

internal class COVRSection: LS2Section
{
	public static readonly Byte[] FourCC = {67, 79, 86, 82};

	public String Title {get; set;}
	public String Description {get; set;}
	public Byte[] CoverData {get; set;}

	public static COVRSection Load(BinaryReader reader, Boolean v2)
	{
		if (v2)
			return new COVRSection() {
				CoverData = Util.ReadLS2ByteArray(reader),
				Title = Util.ReadLS2String(reader),
				Description = Util.ReadLS2String(reader)
			};
		else
			return new COVRSection() {
				Title = Util.ReadLS2String(reader),
				Description = Util.ReadLS2String(reader),
				CoverData = Util.ReadLS2ByteArray(reader)
			};
	}

	internal COVRSection() {}
};

}
