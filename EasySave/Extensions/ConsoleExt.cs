using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Extensions
{
    public static class ConsoleExt
    {

        public static int ReadDec()
        {
            return Console.Read() - 48;
        }

    }
}
