using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Extensions
{
    /// <summary>
    /// Provides extension methods for console input and output operations.
    /// </summary>
    public class ConsoleExt
    {
        /// <summary>
        /// Initializes a new instance of the ConsoleExt class.
        /// </summary>
        private ConsoleExt() { }

        /// <summary>
        /// Reads a single decimal digit character from the standard input stream and returns its integer value.
        /// </summary>
        /// <remarks>This method reads the next character from the standard input and interprets it as a
        /// decimal digit ('0'–'9'). If the input is not a digit, the returned value may not be in the range 0–9. If the
        /// end of the input stream is reached, the method returns -48.</remarks>
        /// <returns>The integer value of the decimal digit read from the input stream, or a negative value if the end of the
        /// input stream is reached.</returns>
        public static int ReadDec()
        {
            return Console.Read() - 48;
        }

    }
}
