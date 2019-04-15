using System;
using fNbt;

namespace LS2OVR
{
	/// <summary>
	/// Class that contains background information.
	/// </summary>
	public struct BackgroundInfo
	{
		/// <summary>
		/// Background number.
		/// </summary>
		public Int32 BackgroundNumber {get; set;}
		/// <summary>
		/// Main background filename.
		/// </summary>
		public String Main {get; set;}
		/// <summary>
		/// Left part background filename.
		/// </summary>
		public String Left {get; set;}
		/// <summary>
		/// Right part background filename.
		/// </summary>
		public String Right {get; set;}
		/// <summary>
		/// Top part background filename.
		/// </summary>
		public String Top {get; set;}
		/// <summary>
		/// Bottom part background filename.
		/// </summary>
		public String Bottom {get; set;}

		/// <summary>
		/// Create new BackgroundInfo with specified background number.
		/// </summary>
		/// <param name="number">Background number (must be greater than 0).</param>
		/// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="number"/> is 0 or negative.</exception>
		public BackgroundInfo(Int32 number)
		{
			if (number <= 0)
				throw new ArgumentOutOfRangeException("number");

			BackgroundNumber = number;
			Main = Left = Right = Top = Bottom = null;
		}

		/// <summary>
		/// Create new BackgroundInfo with specified main, left, right, top, and bottom background data.
		/// This function also accepts string in format ":&lt;number&gt;" to specify background number.
		/// </summary>
		/// <param name="main">Main background filename.</param>
		/// <param name="l">Left background filename.</param>
		/// <param name="r">Right background filename.</param>
		/// <param name="t">Top background filename.</param>
		/// <param name="b">Bottom background filename.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="main"/> is null.</exception>
		/// <exception cref="System.FormatException">Thrown if <paramref name="main"/> is not correct number format.</exception>
		public BackgroundInfo(String main, String l = null, String r = null, String t = null, String b = null)
		{
			if (main == null)
				throw new ArgumentNullException("main");
			
			if (main[0] == ':')
			{
				if (main.Length > 1)
				{
					BackgroundNumber = Int32.Parse(main.Substring(1));
					Main = Left = Right = Top = Bottom = null;
				}
				else
					throw new FormatException("\"main\" argument is not in correct format");
			}
			else
			{
				BackgroundNumber = 0;
				Main = main;

				if (l != null && r != null)
				{
					Left = l;
					Right = r;
				}
				else
					Left = Right = null;

				if (t != null && b != null)
				{
					Top = t;
					Bottom = b;
				}
				else
					Top = Bottom = null;
			}
		}

		/// <summary>
		/// Create new BackgroundInfo based on NBT data.
		/// </summary>
		/// <param name="data">TAG_Compound NBT data.</param>
		/// <exception cref="System.InvalidCastException">Thrown if "main" field in NBT is missing or invalid.</exception>
		public BackgroundInfo(NbtCompound data)
		{
			NbtString temp1, temp2;
			Main = data.Get<NbtString>("main").StringValue;
			BackgroundNumber = 0;

			if (data.TryGet<NbtString>("left", out temp1) && data.TryGet<NbtString>("right", out temp2))
			{
				Left = temp1.StringValue;
				Right = temp2.StringValue;
			}
			else
				Left = Right = null;


			if (data.TryGet<NbtString>("top", out temp1) && data.TryGet<NbtString>("bottom", out temp2))
			{
				Top = temp1.StringValue;
				Bottom = temp2.StringValue;
			}
			else
				Top = Bottom = null;
		}

		public Boolean IsValidLeftRightBackground()
		{
			return Left != null && Right != null;
		}

		public Boolean IsValidTopBottomBackground()
		{
			return Top != null && Bottom != null;
		}

		/// <summary>
		/// Check whetever this background info is complex or simple.
		/// </summary>
		/// - Background info is complex if it defines left & right part or top & bottom part of the background.
		/// - Background info is simple if it only defines main part of the background or background number.
		/// <returns>Whetever is the background info is complex or simple.</returns>
		public Boolean IsComplex()
		{
			return (IsValidLeftRightBackground() || IsValidTopBottomBackground()) && BackgroundNumber == 0;
		}
		
		public static explicit operator NbtCompound(BackgroundInfo self)
		{
			if (self.IsComplex() == false)
				throw new InvalidCastException("background info data is simple");

			NbtCompound data = new NbtCompound();
			data.Add(new NbtString("main", self.Main));

			if (self.IsValidLeftRightBackground())
			{
				data.Add(new NbtString("left", self.Left));
				data.Add(new NbtString("right", self.Right));
			}

			if (self.IsValidTopBottomBackground())
			{
				data.Add(new NbtString("top", self.Top));
				data.Add(new NbtString("bottom", self.Bottom));
			}

			return data;
		}

		public static explicit operator String(BackgroundInfo self)
		{
			if (self.IsComplex())
				throw new InvalidCastException("background info data is complex");

			if (self.Main != null)
				return self.Main;
			else
				return String.Format(":{0}", self.BackgroundNumber);
		}
	}
}
