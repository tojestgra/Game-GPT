

namespace Generic
{
    public class Menu
    {
        public string GPT { get; set; }
        public Menu()
        {
            GPT = "gpt-3.5-turbo";
        }
        public string Main_menu()
        {
            string option;
            Console.WriteLine("Main menu hi >:333!!!");
            Console.WriteLine("what do ya wanna do");
            Console.WriteLine("Start,Exit,Options");
            while (true)
            {
                option = Console.ReadLine()!.ToLower();
                if (option == "start") { break; }
                else if (option == "exit") { Environment.Exit(0); }
                else if (option == "options") { Menu_Options(); }
                else { Console.WriteLine("Wrong"); }
            }
            return option;
        }
        public string Pause_menu()
        {
            string option;
            while (true)
            {
                Console.WriteLine("Pause menu woah ;3");
                Console.WriteLine("what do ya wanna do");
                Console.WriteLine("Resume,Options,Exit");
                option = Console.ReadLine()!.ToLower();
                if (option == "resume") { break; }
                else if (option == "exit") { Environment.Exit(0); }
                else if (option == "options") { Menu_Options(); }
                else { Console.WriteLine("Wrong"); }
            }
            return option;
        }
        public void Menu_Options()
        {
            string option;
            Console.WriteLine("Options on which gpt to use, gpt-4, local, gpt-3.5-turbo");
            while (true)
            {
                option = Console.ReadLine()!.ToLower();
                if (option != "gpt-4" && option != "gpt-3.5-turbo" && option != "local") Console.WriteLine("WRONG!!! try again");
                else { GPT = option; break; }
            }
        }
    }
    public class Debug
    {
        public static bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
            return false;
#endif
            }
        }
    }
}