namespace EasySave.Extensions
{
	/// <summary>
	/// Provides helper methods for console interactions, specifically for reading and parsing user input.
	/// </summary>
	public static class ConsoleExt
	{

		/// <summary>
		/// Reads a line of text from the console and converts it to an integer.
		/// </summary>
		/// <returns>The integer representation of the user's input.</returns>
		/// <exception cref="FormatException">Thrown if the input is not a valid integer.</exception>
		public static int ReadDec()
		{
			if (int.TryParse(Console.ReadLine(), out int dec))
				return dec;
			throw new FormatException("Not an int!");
		}

	}
}