using System.Threading; // Nécessaire pour le Mutex

namespace CryptoSoft;

public static class Program
{
    public static void Main(string[] args)
    {
        // Définition d'un nom unique pour le Mutex. 
        // "Global\" permet de le rendre visible sur tout le système.
        const string mutexId = "Global\\CryptoSoft_Instance_Lock_v1";

        // createdNew sera 'true' si le Mutex a été créé (donc on est le premier),
        // et 'false' si le Mutex existait déjà (donc une autre instance tourne).
        using (var mutex = new Mutex(true, mutexId, out bool createdNew))
        {
            if (!createdNew)
            {
                // Une autre instance est déjà en cours d'exécution
                Console.WriteLine("[CryptoSoft] Error: Another instance is already running.");

                // On retourne un code d'erreur spécifique (-100) pour indiquer le conflit
                Environment.Exit(-100);
                return;
            }

            // --- DEBUT DU CODE ORIGINAL ---
            try
            {
                // Vérification basique des arguments pour éviter les crashs si args est vide
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage: CryptoSoft.exe <SourcePath> <Key>");
                    Environment.Exit(-1);
                }

                // Log des arguments (comme dans votre code original)
                foreach (var arg in args)
                {
                    Console.WriteLine(arg);
                }

                var fileManager = new FileManager(args[0], args[1]);
                int ElapsedTime = fileManager.TransformFile();

                // Retourne le temps écoulé si succès
                Environment.Exit(ElapsedTime);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(-99);
            }
            // --- FIN DU CODE ORIGINAL ---

        } // Le Mutex est libéré automatiquement ici grâce au 'using'
    }
}