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
using System.Collections.Generic;
using fNbt;

namespace LS2OVR
{

/// <summary>
/// LS2OVR beatmap data.
/// </summary>
public struct BeatmapData
{
	/// <summary>
	/// Beatmap star difficulty level.
	/// </summary>
	public Byte Star {get; set;}
	/// <summary>
	/// Beatmap star difficulty (randomized).
	/// </summary>
	public Byte StarRandom {get; set;}
	/// <summary>
	/// Beatmap difficulty name.
	/// </summary>
	public String DifficultyName {get; set;}
	/// <summary>
	/// Beatmap background information data.
	/// </summary>
	public BackgroundInfo? Background {get; set;}
	/// <summary>
	/// Beatmap background information data (randomized).
	/// </summary>
	public BackgroundInfo? BackgroundRandom {get; set;}
	/// <summary>
	/// Custom unit image list.
	/// </summary>
	public List<CustomUnitInfo> CustomUnitList {get; set;}
	/// <summary>
	/// 4 ints of score rank, in C, B, A, and S score order.
	/// </summary>
	public Int32[] ScoreInfo {
		get {
			return _scoreInfo;
		}
		set {
			if (value.Length < 4)
				throw new ArgumentException("array size must be at least 4", "value");
			_scoreInfo = value;
		}
	}
	/// <summary>
	/// 4 ints of combo rank, in C, B, A, and S combo order.
	/// </summary>
	public Int32[] ComboInfo
	{
		get
		{
			return _comboInfo;
		}
		set
		{
			if (value.Length < 4)
				throw new ArgumentException("array size must be at least 4", "value");
			_comboInfo = value;
		}
	}
	/// <summary>
	/// Base score/tap. May be 0 to use default.
	/// </summary>
	public Int32 BaseScorePerTap {get; set;}
	/// <summary>
	/// Initial and max stamina. May be 0 to use default.
	/// </summary>
	public Int16 InitialStamina {get; set;}
	/// <summary>
	/// Is simultaneous note marked correctly?
	/// </summary>
	public Boolean SimultaneousFlagProperlyMarked {get; set;}
	/// <summary>
	/// Beatmap hit points data.
	/// </summary>
	public List<BeatmapTimingMap> MapData {get; set;}

	/// <summary>
	/// Create new BeatmapData from NbtCompound.
	/// </summary>
	/// <param name="data">TAG_Compound containing the beatmap data.</param>
	/// <exception cref="LS2OVR.MissingRequiredFieldException">Thrown if some of the required fields are missing.</exception>
	/// <exception cref="LS2OVR.FieldInvalidValueException">Thrown if some of the required fields are invalid.</exception>
	public BeatmapData(NbtCompound data)
	{
		CustomUnitList = new List<CustomUnitInfo>();
		MapData = new List<BeatmapTimingMap>();

		Star = Util.GetRequiredByteField(data, "star");
		StarRandom = Util.GetRequiredByteField(data, "starRandom");
		SimultaneousFlagProperlyMarked = Util.GetRequiredByteField(data, "simultaneousMarked") > 0;

		Background = TryGetBackground(data, "background");
		BackgroundRandom = TryGetBackground(data, "backgroundRandom");

		if (Background != null && BackgroundRandom == null)
			throw new MissingRequiredFieldException("backgroundRandom");

		Int32[] calculatedScore = new Int32[4];
		Int32[] calculatedCombo = new Int32[4];

		NbtTag mapDataList = data["map"];
		if (mapDataList is NbtList)
		{
			NbtList mapDataListObject = mapDataList as NbtList;

			if (mapDataListObject.TagType == NbtTagType.Compound)
			{
				foreach (NbtCompound map in mapDataListObject.ToArray<NbtCompound>())
				{
					try
					{
						BeatmapTimingMap hitPoint = new BeatmapTimingMap(map);
						calculatedCombo[3]++;
						calculatedScore[3] += hitPoint.SwingNote ? 370 : 739;
						MapData.Add(hitPoint);
					}
					catch (InvalidCastException) {}
					catch (InvalidOperationException) {}
				}

				if (MapData.Count == 0)
					throw new FieldInvalidValueException("map");
			}
			else
				throw new FieldInvalidValueException("map");
		}
		else
			throw new MissingRequiredFieldException("map");

		calculatedScore[0] = (Int32) Math.Round(((Double) calculatedScore[3]) * 211.0 / 739.0);
		calculatedScore[1] = (Int32) Math.Round(((Double) calculatedScore[3]) * 528.0 / 739.0);
		calculatedScore[2] = (Int32) Math.Round(((Double) calculatedScore[3]) * 633.0 / 739.0);
		calculatedCombo[0] = (Int32) Math.Ceiling(((Double) calculatedCombo[3]) * 0.3);
		calculatedCombo[1] = (Int32) Math.Ceiling(((Double) calculatedCombo[3]) * 0.5);
		calculatedCombo[2] = (Int32) Math.Ceiling(((Double) calculatedCombo[3]) * 0.7);

		if (data.TryGet("customUnitList", out NbtList customUnitList))
		{
			NbtCompound[] list = null;

			try
			{
				list = customUnitList.ToArray<NbtCompound>();
			}
			catch (InvalidCastException) { }

			if (list != null)
			{
				foreach (NbtCompound unit in list)
				{
					try
					{
						CustomUnitList.Add(new CustomUnitInfo(unit));
					}
					catch (InvalidCastException) { }
					catch (InvalidOperationException) { }
				}
			}
		}

		if (data.TryGet("baseScorePerTap", out NbtInt baseScore))
		{
			BaseScorePerTap = baseScore.IntValue;
			if (BaseScorePerTap <= 0)
				BaseScorePerTap = 0;
		}
		else
			BaseScorePerTap = 0;

		if (data.TryGet("stamina", out NbtShort baseStamina))
		{
			InitialStamina = baseStamina.ShortValue;
			if (InitialStamina <= 0)
				InitialStamina = 0;
		}
		else
			InitialStamina = 0;

		if (data.TryGet("difficultyName", out NbtString difficultyName))
			DifficultyName = difficultyName.StringValue;
		else
			DifficultyName = null;

		_scoreInfo = TryGetScoreOrComboInformation(data, "scoreInfo") ?? calculatedScore;
		_comboInfo = TryGetScoreOrComboInformation(data, "comboInfo") ?? calculatedCombo;
	}

	public static explicit operator NbtCompound(BeatmapData self)
	{
		NbtCompound data = new NbtCompound("beatmap")
		{
			new NbtByte("star", self.Star),
			new NbtByte("starRandom", self.StarRandom),
			new NbtByte("simultaneousMarked", (Byte) (self.SimultaneousFlagProperlyMarked ? 1 : 0))
		};

		if (self.DifficultyName != null)
			data.Add(new NbtString("difficultyName", self.DifficultyName));
		if (self.Background.HasValue)
		{
			BackgroundInfo background = self.Background.Value;
			BackgroundInfo randomBG = self.BackgroundRandom ?? background;
			NbtTag backgroundNBT;
			NbtTag randomBackgroundNBT;
			
			if (background.IsComplex())
			{
				backgroundNBT = (NbtCompound) background;
				backgroundNBT.Name = "background";
			}
			else
				backgroundNBT = new NbtString("background", (String) background);
			
			if (randomBG.IsComplex())
			{
				randomBackgroundNBT = (NbtCompound) randomBG;
				randomBackgroundNBT.Name = "backgroundRandom";
			}
			else
				randomBackgroundNBT = new NbtString("backgroundRandom", (String) randomBG);

			data.Add(backgroundNBT);
			data.Add(randomBackgroundNBT);
		}

		if (self.CustomUnitList.Count > 0)
		{
			NbtList customUnitList = new NbtList("customUnitList");

			foreach (CustomUnitInfo customUnit in self.CustomUnitList)
				customUnitList.Add((NbtCompound) customUnit);
			
			data.Add(customUnitList);
		}
		
		if (self._scoreInfo != null && self._scoreInfo.Length >= 4)
		{
			Int32[] temp = new Int32[4];
			Array.Copy(self._scoreInfo, temp, 4);
			data.Add(new NbtIntArray("scoreInfo", temp));
		}

		if (self._comboInfo != null && self._comboInfo.Length >= 4)
		{
			Int32[] temp = new Int32[4];
			Array.Copy(self._comboInfo, temp, 4);
			data.Add(new NbtIntArray("comboInfo", temp));
		}

		if (self.BaseScorePerTap > 0)
			data.Add(new NbtInt("baseScorePerTap", self.BaseScorePerTap));
		
		if (self.InitialStamina > 0)
			data.Add(new NbtShort("stamina", self.InitialStamina));
		
		NbtList mapList = new NbtList("map");
		foreach (BeatmapTimingMap map in self.MapData)
			mapList.Add((NbtCompound) map);

		data.Add(mapList);

		return data;
	}

	private Int32[] _scoreInfo;
	private Int32[] _comboInfo;

	internal static Int32[] TryGetScoreOrComboInformation(NbtCompound data, String key)
	{
		if (data.TryGet(key, out NbtIntArray result))
		{
			Int32[] arrayData = result.IntArrayValue;
			if (arrayData.Length >= 4)
			{
				Boolean isOK = true;

				for (Int32 i = 0; i < 4 && isOK == false; i++)
				{
					if (arrayData[i] > 0)
					{
						if (i > 0)
						{
							if (arrayData[i] <= arrayData[i - 1])
								isOK = false;
						}
					}
					else
					{
						isOK = false;
						break;
					}
				}

				if (isOK)
					return arrayData;
			}
		}

		return null;
	}

	internal static BackgroundInfo? TryGetBackground(NbtCompound data, String key)
	{
		if (data.TryGet(key, out NbtTag background))
		{
			if (background is NbtCompound)
			{
				try
				{
					return new BackgroundInfo((NbtCompound) background);
				}
				catch (ProblematicRequiredFieldException)
				{
					return null;
				}
			}
			else if (background is NbtString)
			{
				try
				{
					return new BackgroundInfo(((NbtString) background).StringValue);
				}
				catch (FormatException)
				{
					return null;
				}
			}
		}

		return null;
	}
};

}
