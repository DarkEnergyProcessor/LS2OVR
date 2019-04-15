using System;

namespace LS2OVR
{
	/// <summary>
	/// Structure which holds song composer information role and name.
	/// </summary>
	public struct ComposerData
	{
		/// <summary>
		/// Composer role.
		/// </summary>
		public String Role { get; set; }
		/// <summary>
		/// Composer name.
		/// </summary>
		public String Name { get; set; }

		/// <summary>
		/// Create new ComposerData object with specified role and name.
		/// </summary>
		/// <param name="r">Composer role.</param>
		/// <param name="n">Composer name.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="n"/> or <paramref name="r"/> is null</exception>
		public ComposerData(String r, String n)
		{
			Role = r ?? throw new ArgumentNullException("r");
			Name = n ?? throw new ArgumentNullException("n");
		}
	};
}
