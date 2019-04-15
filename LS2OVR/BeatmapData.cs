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
	public struct BeatmapData
	{
		public Byte Star {get; set;}
		public Byte StarRandom {get; set;}
		public String DifficultyName {get; set;}
		public BackgroundInfo? Background {get; set;}
		public BackgroundInfo? BackgroundRandom {get; set;}
		public List<CustomUnitInfo> CustomUnitList {get; set;}
		private Int32[] _scoreInfo;
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
		private Int32[] _comboInfo;
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
		public Int32 BaseScorePerTap {get; set;}
		public Int16 InitialStamina {get; set;}
		public Boolean SimultaneousFlagProperlyMarked {get; set;}
		public List<BeatmapTimingMap> MapData {get; set;}

		public BeatmapData(NbtCompound data)
		{
			CustomUnitList = new List<CustomUnitInfo>();
			MapData = new List<BeatmapTimingMap>();

			Star = data.Get<NbtByte>("star").ByteValue;
			StarRandom = data.Get<NbtByte>("star").ByteValue;
			Background = TryGetBackground(data, "background", "backgroundList");
			BackgroundRandom = TryGetBackground(data, "backgroundRandom", "randomBackgroundList");
			SimultaneousFlagProperlyMarked = data.Get<NbtByte>("simultaneousMarked").ByteValue > 0;

			Int32[] calculatedScore = new Int32[4];
			Int32[] calculatedCombo = new Int32[4];

			foreach (NbtCompound map in data.Get<NbtList>("map").ToArray<NbtCompound>())
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
					throw new InvalidOperationException("\"baseScorePerTap\" is 0 or negative");
			}
			else
				BaseScorePerTap = 0;

			if (data.TryGet("stamina", out NbtShort baseStamina))
			{
				InitialStamina = baseStamina.ShortValue;
				if (InitialStamina <= 0)
					throw new InvalidOperationException("\"stamina\" is 0 or negative");
			}
			else
				InitialStamina = 0;

			if (data.TryGet("difficultyName", out NbtString difficultyName))
				DifficultyName = difficultyName.StringValue;
			else
				DifficultyName = String.Format("{0}☆", Star); // dat star

			_scoreInfo = TryGetScoreOrComboInformation(data, "scoreInfo") ?? calculatedScore;
			_comboInfo = TryGetScoreOrComboInformation(data, "comboInfo") ?? calculatedCombo;
		}

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

		internal static BackgroundInfo? TryGetBackground(NbtCompound data, String key1, String key2)
		{
			NbtString temp;

			if (data.TryGet(key2, out NbtCompound background))
			{
				try
				{
					return new BackgroundInfo(background);
				}
				catch (InvalidCastException)
				{
					if (data.TryGet(key1, out temp))
					{
						try
						{
							return new BackgroundInfo(temp.StringValue);
						}
						catch (FormatException)
						{
							return null;
						}
					}
					else
						return null;
				}
			}
			else if (data.TryGet(key1, out temp))
			{
				try
				{
					return new BackgroundInfo(temp.StringValue);
				}
				catch (FormatException)
				{
					return null;
				}
			}
			else
				return null;
		}
	};
}
