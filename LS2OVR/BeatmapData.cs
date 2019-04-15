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

namespace LS2OVR
{
	public struct BeatmapData
	{
		public Byte Star {get; set;}
		public Byte StarRandom {get; set;}
		public String DifficultyName {get; set;}
		public BackgroundInfo Background {get; set;}
		public BackgroundInfo BackgroundRandom {get; set;}
		public List<CustomUnitInfo> CustomUnitList {get; set;}
		public Int32[] ScoreInfo {get; set;}
		public Int32[] ComboInfo {get; set;}
		public Int32 BaseScorePerTap {get; set;}
		public Int16 InitialStamina {get; set;}
		public Boolean SimultaneousFlagProperlyMarked {get; set;}
		public List<BeatmapTimingMap> Map {get; set;}
	};
}
