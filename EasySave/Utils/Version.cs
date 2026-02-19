namespace EasySave.Utils
{
	public class Version : IComparable<Version>
	{

		public readonly string VersionString;

		public readonly int VersionInt;

		private Version(string version)
		{
			VersionString = version;
			try
			{
				VersionInt = int.Parse(version.Replace(".", ""));
			}
			catch (Exception)
			{
				VersionInt = -1;
			}
		}

		public static Version Create(string? version)
		{
			if (version == null)
				return new InvalidVersion(string.Empty);
			return new Version(version);
		}

		public virtual int CompareTo(Version? other)
		{
			if (other == null)
				return 1;
			return VersionInt.CompareTo(other.VersionInt);
		}

		public override string ToString()
		{
			return VersionString;
		}

		private class InvalidVersion(string version) : Version(version)
		{
			public override int CompareTo(Version? other)
			{
				if (other == null)
					return 0;
				return -1;
			}
		}
	}
}