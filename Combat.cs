using AI;
using Gayme;
using Generic;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using Types;

namespace Combat
{
    public class TurnCombat
    {
        public static async Task Fight(List<Character> player, List<Character> enemy, Dictionary<string, Operation> operations,string GPT,string system,Menu menu,string Log)
        {
            int turn = 1;
            string log;
            string enemy_names = "";
            for (int i = 0; i < enemy.Count - 1; i++)
            {
                enemy_names += enemy[i].Name + ",";
            }
            enemy_names += enemy.Last().Name;
            Console.WriteLine($"You were attacked by {enemy_names} !");
            while (true)
            {
                Log += $"--Turn: {turn}--\n";
                (player,enemy,log) = await Load(PlayerTurn(player, enemy, operations, enemy_names,GPT,system,menu), "wThinking", "wThinking finished !");
                Log += log+'\n';
                if(Debug.IsDebug)DebugInfo(enemy, player, Log);
                (enemy,player,log) = await Load(AllyTurn(enemy, player, operations,GPT,system,menu), "Thinking", "Thinking finished !");
                Log += log + '\n';
                if(Debug.IsDebug)DebugInfo(enemy, player, Log);
                (player,enemy,log) = await Load(EnemyTurn(player, enemy, operations,GPT, system, menu), "BThinking", "BThinking finished !");
                Log += log + '\n';
                if(Debug.IsDebug)DebugInfo(enemy, player,Log);
                Console.WriteLine(await Narrator(turn,GPT,system,menu, Log));
                turn++;
            }
        }
        public static async Task<(List<Character>, List<Character>, string)> Load(Task<(List<Character>, List<Character>, string)> task, string message, string final)
        {
            int dotCount = 0;

            // Pre-calculate the padding length to be used to clear the console line.
            int padLength = Math.Max(message.Length + 3, final.Length); // +3 is for adding '...'
            string padString = new string(' ', padLength);

            void WriteMessageAndDots()
            {
                var output = '\r' + message + new string('.', dotCount);
                var remaining = padLength - output.Length;
                Console.Write(output + new string(' ', remaining));
            }

            while (!task.IsCompleted)
            {
                if (dotCount == 3)
                {
                    dotCount = 0;
                }
                else
                {
                    dotCount++;
                }

                WriteMessageAndDots();

                await Task.Delay(1000);
            }

            Console.Write('\r' + padString);    // clear the last progress line
            Console.WriteLine(final);

            return await task;  // return the result of the completed task
        }
        public static async Task<string> Narrator(int turn,string GPT,string System,Menu Menu,string Log)
        {
            JArray Response;
            string ProcessedPrompt = $"Labels:\nNarration(narration)\nTask: You are the narrator, your goal is to summarize an action / actions performed by characters in a situation. Use narration to give a short and flavorful descriptions of actions characters in the log string have taken. Narrate for the current turn, but also base a bit of your narration on what has previously happened.\nThe current turn is: {turn}. Log:\n{Log}";
            Console.WriteLine(ProcessedPrompt);
            Response = await AIHandler.Get_response(ProcessedPrompt, System, Menu, 200,GPT, true);
            string narration = Json.GetValue(Response, "Narration", "narration");
            return narration;
        }
        static string PromptConstructoir(Dictionary<string,Operation> operations,List<Character> player,List<Character> enemy,int e)
        {
            string ProcessedPrompt = $"Labels:\r\nReasoning(advantages,disadvantages,course_of_action)\r\nAction(Name,Target)\r\n\r\nTask: You are playing the role of {enemy[e].Name}. Your objective is to make prudent decisions based on the current game dynamics, prioritizing the team's interests and predicting possible moves from your allies and adversaries. Engage in a thorough analysis of potential actions by comparing their descriptions with your character's statistics to determine the best route. Use the label 'reasoning' along with its elements - 'advantages', 'disadvantages', and 'course of action' - to methodically evaluate all possibilities and ascertain the optimal course of action. This will involve weighing the merits of your character's stats against the disadvantages.\r\n\r\nThe 'name' label will denote your chosen action, whereas the 'target' label will indicate the entity at the receiving end of your action. Should you decide to direct the action towards yourself, utilize 'player' for the 'target'. Try to keep your reasoning short and to the point. You are only allowed to use one action, so choose carefully. Your available options are outlined below:\n";
            foreach (Operation d in operations.Values)
            {
                ProcessedPrompt += $"Action name: {d.Name}. Action description: {d.Description}. Used on friendly: {d.Ally}. Used on self: {d.Self}.\n";
            }
            ProcessedPrompt += "Enemies:\n\n";
            foreach (Character d in player)
            {
                ProcessedPrompt += $"Enemy: {d.Name}.\n";
            }
            ProcessedPrompt += "\nAllies:\n\n";
            List<Character> allies = new List<Character>(enemy);
            allies.Remove(enemy[e]);
            foreach (Character c in allies)
            {
                ProcessedPrompt += $"Ally: {c.Name}.\n";
            }
            return ProcessedPrompt;
        }
        public static async Task<(List<Character>, List<Character>, string)> AllyTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operation> operations,string GPT, string system, Menu menu)
        {
            JArray Response;
            string Logs = "";
            List<Character> playerList = new List<Character>(enemy);
            if (enemy.Count == 1) return (player, enemy, Logs);
            else
            {
                for (int e = 1; e < playerList.Count; e++)
                {
                    bool finished = false;
                    string ProcessedPrompt = PromptConstructoir(operations,player,playerList,e);
                    Response = await AIHandler.Get_response(ProcessedPrompt, system, menu, 400, GPT, true);
                    string uh = Json.GetValue(Response, "Action", "Name");
                    Console.WriteLine(ProcessedPrompt);
                    if (operations[uh].Ally == "False")
                    {
                        for (int i = 0; i < player.Count; i++)
                        {
                            if (player[i].Name == Json.GetValue(Response, "Action", "Target"))
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (finished)
                                    {
                                        break;
                                    }
                                    if (ope.Name == uh)
                                    {
                                        (player[i],string log) = Operation.Bexecution(uh, playerList[e], player[i], operations);
                                        Logs += log;
                                        finished = true;
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 1; i < playerList.Count; i++)
                    {
                        if (operations[uh].Ally == "True" && Json.GetValue(Response, "Action", "Target") != "player"|| Json.GetValue(Response, "Action", "Target") != "Player")
                        {
                            if (playerList[i].Name == Json.GetValue(Response, "Action", "Target"))
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (finished)
                                    {
                                        break;
                                    }
                                    if (ope.Name == uh)
                                    {
                                        ( playerList[i],string log ) = Operation.Bexecution(uh, playerList[e], playerList[i], operations);
                                        Logs += log;
                                        finished = true;
                                    }
                                }
                            }
                        }
                        else if (Json.GetValue(Response, "Action", "Target").ToLower() == "player" && operations[uh].Self == "True")
                        {
                            foreach (Operation ope in operations.Values)
                            {
                                if (finished)
                                {
                                    break;
                                }
                                if (ope.Name.ToLower() == uh.ToLower())
                                {
                                    (playerList[e], string log) = Operation.Bexecution(uh, playerList[e], playerList[e], operations);
                                    Logs += log;
                                    finished = true;
                                }
                            }
                        }
                    }
                }
                return (player, playerList, Logs);
            }
        }
        public static async Task<(List<Character>, List<Character>, string)> EnemyTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operation> operations,string GPT, string system, Menu menu)
        {
            string Logs = "";
            JArray Response;
            for (int e = 0; e < enemy.Count; e++)
            {
                bool finished = false;
                string ProcessedPrompt = PromptConstructoir(operations, player, enemy, e);
                Response = await AIHandler.Get_response(ProcessedPrompt, system, menu, 400, GPT, true);
                string uh = Json.GetValue(Response, "Action", "Name");
                if (operations[uh].Ally == "False")
                {
                    for (int i = 0; i < player.Count; i++)
                    {
                        if (player[i].Name == Json.GetValue(Response, "Action", "Target"))
                        {
                            foreach (Operation ope in operations.Values)
                            {
                                if (finished)
                                {
                                    break;
                                }
                                if (ope.Name == uh)
                                {
                                    (player[i],string log) = Operation.Bexecution(uh, enemy[e], player[i], operations);
                                    Logs += log;
                                    finished = true;
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < enemy.Count; i++)
                {
                    if (operations[uh].Ally == "True" && Json.GetValue(Response, "Action", "Target") != "player")
                    {
                        if (enemy[i].Name == Json.GetValue(Response, "Action", "Target"))
                        {
                            foreach (Operation ope in operations.Values)
                            {
                                if(finished)
                                {
                                    break;
                                }
                                if (ope.Name.ToLower() == uh.ToLower())
                                {
                                    (enemy[i], string log) = Operation.Bexecution(uh, enemy[e], enemy[i], operations);
                                    Logs += log;
                                    finished = true;
                                }
                            }
                        }
                    }
                    else if(Json.GetValue(Response, "Action", "Target").ToLower() == "player" && operations[uh].Self == "True")
                    {
                        foreach(Operation ope in operations.Values)
                        {
                            if (finished)
                            {
                                break;
                            }
                            if (ope.Name.ToLower() == uh.ToLower())
                            {
                                (enemy[e], string log) = Operation.Bexecution(uh, enemy[e], enemy[e], operations);
                                Logs += log;
                                finished = true;
                            }
                        }
                    }
                }
            }
            return (player, enemy, Logs);
        }
        public static async Task<(List<Character>, List<Character>,string)> PlayerTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operation> operations, string enemy_names,string GPT,string system,Menu menu)
        {
            string PlayerAction;
            string Log = "";
            while (true)
            {
                Console.WriteLine("What do you want to do ?\n type Help for help");
                PlayerAction = Console.ReadLine().ToLower();
                if (PlayerAction.ToLower() == "local")
                {
                    GPT = "local";
                }
                else if (PlayerAction.ToLower() == "help")
                {
                    Console.WriteLine("Type:\nAct - Action that will be given to the ai for processing, can be anything you wanna do, uses turn" +
                        "\n Menu - go to the menu\nLook - inspect a specific enemy");
                }
                else if (PlayerAction.ToLower() == "menu")
                {
                    menu.Pause_menu(); GPT = menu.GPT; Game.GPT = GPT;
                }
                else if (PlayerAction.ToLower() == "look")
                {
                    Console.WriteLine("Which enemy do you want to inspect ?");
                    Console.WriteLine(enemy_names);
                    PlayerAction = Console.ReadLine();
                    for (int i = 0; i < enemy.Count; i++)
                    {
                        if (enemy[i].Name.ToLower() == PlayerAction)
                        {
                            string properties = "";
                            for (int j = 0; j < enemy[i].Properties.Count; j++)
                            {
                                properties += enemy[i].Properties[j].Name + " - " + enemy[i].Properties[j].Description + "\n";
                            }
                            Console.WriteLine($"Health = {enemy[i].Health},Attack = {enemy[i].Attack}, Defence = {enemy[i].Defence},Properties:\n{properties}");
                        }
                    }
                }
                else if (PlayerAction.ToLower() == "act")
                {
                    string Prompt = Console.ReadLine();
                    string ProcessedPrompt = "Labels:\nAction(Name,Target)\n" +
                                      "Task: Your task is to determine which action the player wants to take depending on what they said. Use name for the name of the action which the player wants to take. Use target to type the name of whatever creature the player wants to inflict the action on. If the player says they want to inflict some action on themselves, return target as \"player\". In that case, if the action the player chooses is ambigous between multiple different actions, choose the one that has Used on self as True. If information from the player is missing replace it with what you think the player most likely would do. avaliable actions:\n";
                    foreach (Operation d in operations.Values)
                    {
                        ProcessedPrompt += $"Action name: {d.Name}. Action description: {d.Description}. Used on friendly: {d.Ally}. Used on self: {d.Self}.\n";
                    }
                    ProcessedPrompt += "Enemies:\n\n";
                    foreach (Character d in enemy)
                    {
                        ProcessedPrompt += $"Enemy: {d.Name}.\n";
                    }
                    ProcessedPrompt += "\nAllies:\n\n";
                    foreach (Character d in player)
                    {
                        ProcessedPrompt += $"Ally: {d.Name}.\n";
                    }
                    ProcessedPrompt += "\n\nPlayers action: " + Prompt;
                    JArray Response = await AIHandler.Get_response(ProcessedPrompt, system, menu, 400,GPT, true);
                    string uh = Json.GetValue(Response, "Action", "Name");
                    for (int i = 0; i < enemy.Count; i++)
                    {
                        if (operations[uh].Ally == "False")
                        {
                            if (enemy[i].Name == Json.GetValue(Response, "Action", "Target"))
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh)
                                    {
                                        (enemy[i],string log) = Operation.Bexecution(uh, player[0], enemy[i], operations);
                                        Log = log;
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 0; i < player.Count; i++)
                    {
                        if (operations[uh].Ally == "True" && Json.GetValue(Response, "Action", "Target") != "player")
                        {
                            if (player[i].Name == Json.GetValue(Response, "Action", "Target"))
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh)
                                    {
                                        (player[i],string log) = Operation.Bexecution(uh, player[0], player[i], operations);
                                        Log = log;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Json.GetValue(Response, "Action", "Target") == "player")
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh)
                                    {
                                        (player[0],string log) = Operation.Bexecution(uh, player[0], player[i], operations);
                                        Log = log;
                                    }
                                }
                            }
                        }
                    }
                    return (player, enemy,Log);
                }
                else
                {
                    Console.WriteLine("Not a valid action");
                }
            }
        }
        public static void DebugInfo(List<Character> player,List<Character> enemy,string log)
        {
            foreach(Character c in player) 
            {
                Console.WriteLine(c.Name);
                Console.WriteLine(c.Health);
                Console.WriteLine(c.Attack);
                Console.WriteLine(c.Defence);
            }
            foreach (Character c in enemy)
            {
                Console.WriteLine(c.Name);
                Console.WriteLine(c.Health);
                Console.WriteLine(c.Attack);
                Console.WriteLine(c.Defence);
            }
            Console.WriteLine(log);
        }
    }
}