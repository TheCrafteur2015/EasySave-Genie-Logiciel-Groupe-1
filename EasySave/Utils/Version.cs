namespace EasySave.Utils
{
    /// <summary>
    /// Represents a version number and provides functionality to compare different versions.
    /// Implements <see cref="IComparable{T}"/> to allow version sorting.
    /// </summary>
    public class Version : IComparable<Version>
    {
        /// <summary>
        /// The original string representation of the version (e.g., "1.0.2").
        /// </summary>
        public readonly string VersionString;

        /// <summary>
        /// An integer representation of the version used for comparison logic.
        /// Dots are removed to parse the string into a single integer.
        /// </summary>
        public readonly int VersionInt;

        /// <summary>
        /// Initializes a new instance of the <see cref="Version"/> class.
        /// </summary>
        /// <param name="version">The version string to parse.</param>
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

        /// <summary>
        /// Factory method to create a new Version instance.
        /// Returns an <see cref="InvalidVersion"/> if the input string is null.
        /// </summary>
        /// <param name="version">The version string (nullable).</param>
        /// <returns>A new <see cref="Version"/> object.</returns>
        public static Version Create(string? version)
        {
            if (version == null)
                return new InvalidVersion(string.Empty);
            return new Version(version);
        }

        /// <summary>
        /// Compares the current instance with another version.
        /// </summary>
        /// <param name="other">The version to compare with this instance.</param>
        /// <returns>A value indicating the relative order of the objects being compared.</returns>
        public virtual int CompareTo(Version? other)
        {
            if (other == null)
                return 1;
            return VersionInt.CompareTo(other.VersionInt);
        }

        /// <summary>
        /// Returns the string representation of the version.
        /// </summary>
        /// <returns>The <see cref="VersionString"/> property.</returns>
        public override string ToString()
        {
            return VersionString;
        }

        /// <summary>
        /// Represents an invalid version state, typically used when input data is missing or corrupted.
        /// </summary>
        private class InvalidVersion(string version) : Version(version)
        {
            /// <summary>
            /// Overrides comparison logic to ensure invalid versions are handled correctly.
            /// </summary>
            public override int CompareTo(Version? other)
            {
                if (other == null)
                    return 0;
                return -1;
            }
        }
    }
}