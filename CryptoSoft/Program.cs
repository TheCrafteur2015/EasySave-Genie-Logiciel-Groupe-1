using System.Threading;

namespace CryptoSoft;

public static class Program
{
    public static void Main(string[] args)
    {
        
        const string mutexId = "Global\\CryptoSoft_Instance_Lock_v1";


        using (var mutex = new Mutex(true, mutexId, out bool createdNew))
        {
            if (!createdNew)
            {
                Console.WriteLine("[CryptoSoft] Error: Another instance is already running.");

                Environment.Exit(-100);
                return;
            }

            try
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: CryptoSoft.exe <SourcePath> <Key>");
                    Environment.Exit(-1);
                }

                 foreach (var arg in args)
                {
                    Console.WriteLine(arg);
                }

                var fileManager = new FileManager(args[0], args[1]);
                int ElapsedTime = fileManager.TransformFile();

                Environment.Exit(ElapsedTime);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-99);
            }

        }
    }
}