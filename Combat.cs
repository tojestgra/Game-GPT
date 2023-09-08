using AI;
using Gayme;
using Generic;
using Newtonsoft.Json.Linq;
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
                var pl = await Load(PlayerTurn(player, enemy, operations, enemy_names,GPT,system,menu), "Thinking", "Thinking finished !");
                player = pl.Item1;
                enemy = pl.Item2;
                Log += pl.Item3+'\n';
                Console.WriteLine(pl.Item3);
                var all = await Load(AiTurn(enemy, player, operations, true,GPT,system,menu), "Thinking", "Thinking finished !");
                player = all.Item2;
                enemy = all.Item1;
                Log += all.Item3 + '\n';
                Console.WriteLine(all.Item3);
                var ene = await Load(AiTurn(player, enemy, operations, false, GPT, system, menu), "Thinking", "Thinking finished !");
                player = ene.Item1;
                enemy = ene.Item2;
                Log += ene.Item3 + '\n';
                Console.WriteLine(ene.Item3);
                Log += "\n";
                Console.WriteLine(await Narrator(turn,GPT,system,menu, Log));
                turn++;
            }
        }
        public static async Task<(List<Character>, List<Character>, string)> Load(Task<(List<Character>, List<Character>, string)> task, string message, string final)
        {
            int dotCount = 0;
            while (!task.IsCompleted)
            {
                if (dotCount == 3)
                {
                    Console.Write("\r   \r"); // clear the line after 3 dots
                    dotCount = 0;
                }
                else if (dotCount == 0)
                {
                    Console.Write($"{message}.");
                    dotCount++;
                }
                else if (dotCount == 1)
                {
                    Console.Write($"{message}..");
                    dotCount++;
                }
                else if (dotCount == 2)
                {
                    Console.Write($"{message}...");
                    dotCount++;
                }

                await Task.Delay(1000);
                Console.Write("\r   \r");
            }
            Console.Write("\r   \r");
            Console.Write($"{final}\n");

            return await task;  // return the result of the completed task
        }
        public static async Task<string> Narrator(int turn,string GPT,string System,Menu Menu,string Log)
        {
            JArray Response;
            string ProcessedPrompt = $"Labels:\nNarration(narration)\nTask: You are the narrator, your goal is to summarize an action / actions performed by characters in a situation. Use narration to give a short and flavorful descriptions of actions characters in the log string have taken. Narrate for the current turn, but also base a bit of your narration on what has previously happened.\nThe current turn is: {turn}. Log:\n{Log}";
            Console.WriteLine(ProcessedPrompt);
            Response = await AIHandler.Get_response(ProcessedPrompt, System, Menu, 200,GPT);
            string narration = Json.GetValue(Response, "Narration", "narration");
            return narration;
        }
        public static async Task<(List<Character>, List<Character>, string)> AiTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operation> operations, bool players, string GPT, string system, Menu menu)
        {
            JArray Response;
            string Logs = "";
            List<Character> playerList = new List<Character>(enemy);
            if (players == true && enemy.Count == 1) return (player, enemy, Logs);
            else if (players == true)
            {
                Console.WriteLine("allied");
                for (int e = 1; e < playerList.Count; e++)
                {
                    string ProcessedPrompt = $"Labels:\r\nReasoning(advantages,disadvantages,course_of_action)\r\nAction(Name,Target)\r\n\r\nTask: You are playing the role of {playerList[e].Name}. Your objective is to make prudent decisions based on the current game dynamics, prioritizing the team's interests and predicting possible moves from your allies and adversaries. Engage in a thorough analysis of potential actions by comparing their descriptions with your character's statistics to determine the best route. Use the label 'reasoning' along with its elements - 'advantages', 'disadvantages', and 'course of action' - to methodically evaluate all possibilities and ascertain the optimal course of action. This will involve weighing the merits of your character's stats against the disadvantages.\r\n\r\nThe 'name' label will denote your chosen action, whereas the 'target' label will indicate the entity at the receiving end of your action. Should you decide to direct the action towards yourself, utilize 'player' for the 'target'. Try to keep your reasoning short and to the point. You are only allowed to use one action, so choose carefully. Your available options are outlined below:\n";
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
                    List<Character> allies = new List<Character>(playerList);
                    allies.Remove(playerList[e]);
                    foreach (Character c in allies)
                    {
                        ProcessedPrompt += $"Ally: {c.Name}.\n";
                    }
                    Response = await AIHandler.Get_response(ProcessedPrompt, system, menu, 400, GPT);
                    string uh = Json.GetValue(Response, "Action", "Name");
                    Console.WriteLine(ProcessedPrompt);
                    if (operations[uh].Ally == "False")
                    {
                        Console.WriteLine("ghou");
                        for (int i = 0; i < player.Count; i++)
                        {
                            Console.WriteLine("bdfg");
                            if (player[i].Name == Json.GetValue(Response, "Action", "Target"))
                            {
                                Console.WriteLine("ee");
                                foreach (Operation ope in operations.Values)
                                {
                                    Console.WriteLine("das");
                                    if (ope.Name == uh)
                                    {
                                        var c = await Operation.Bexecution(uh, playerList[e], player[i], operations);
                                        player[i] = c.Item1;
                                        Console.WriteLine("gay");
                                        Console.WriteLine(c.Item2);
                                        Console.WriteLine("gaye");
                                        Logs += c.Item2;
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 0; i < playerList.Count; i++)
                    {
                        if ((operations[uh].Ally == "True" && Json.GetValue(Response, "Action", "Target") != "player") || (Json.GetValue(Response, "Action", "Target") == "player" && operations[uh].Self == "True"))
                        {
                            if (playerList[i].Name == Json.GetValue(Response, "Action", "Target") || Json.GetValue(Response, "Action", "Target") == "player")
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh)
                                    {
                                        var c = await Operation.Bexecution(uh, playerList[e], playerList[i], operations);
                                        playerList[i] = c.Item1;
                                        Console.WriteLine("er");
                                        Console.WriteLine(c.Item2);
                                        Console.WriteLine("err");
                                        Logs += c.Item2;
                                    }
                                }
                            }
                        }
                    }
                }
                return (player, playerList, Logs);
            }
            else
            {
                for (int e = 0; e < enemy.Count; e++)
                {
                    Console.WriteLine("enemied");
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
                    Response = await AIHandler.Get_response(ProcessedPrompt, system, menu, 400, GPT);
                    string uh = Json.GetValue(Response, "Action", "Name");
                    Console.WriteLine(ProcessedPrompt);
                    if (operations[uh].Ally == "False")
                    {
                        Console.WriteLine("ghou");
                        for (int i = 0; i < player.Count; i++)
                        {
                            Console.WriteLine("bdfg");
                            if (player[i].Name == Json.GetValue(Response, "Action", "Target"))
                            {
                                Console.WriteLine("ee");
                                foreach (Operation ope in operations.Values)
                                {
                                    Console.WriteLine("das");
                                    if (ope.Name == uh)
                                    {
                                        var c = await Operation.Bexecution(uh, enemy[e], player[i], operations);
                                        player[i] = c.Item1;
                                        Console.WriteLine("gay");
                                        Console.WriteLine(c.Item2);
                                        Console.WriteLine("gaye");
                                        Logs += c.Item2;
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 0; i < enemy.Count; i++)
                    {
                        if ((operations[uh].Ally == "True" && Json.GetValue(Response, "Action", "Target") != "player") || (Json.GetValue(Response, "Action", "Target") == "player" && operations[uh].Self == "True"))
                        {
                            if (enemy[i].Name == Json.GetValue(Response, "Action", "Target") || Json.GetValue(Response, "Action", "Target") == "player")
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh)
                                    {
                                        var c = await Operation.Bexecution(uh, enemy[e], enemy[i], operations);
                                        enemy[i] = c.Item1;
                                        Console.WriteLine("er");
                                        Console.WriteLine(c.Item2);
                                        Console.WriteLine("err");
                                        Logs += c.Item2;
                                    }
                                }
                            }
                        }
                    }
                }
                return (player, enemy, Logs);
            }
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
                    Console.WriteLine("What action?");
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
                    JArray Response = await AIHandler.Get_response(ProcessedPrompt, system, menu, 400,GPT);
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
                                        var c = await Operation.Bexecution(uh, player[0], enemy[i], operations);
                                        enemy[i] = c.Item1;
                                        Log = c.Item2;
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
                                        var c = await Operation.Bexecution(uh, player[0], player[i], operations);
                                        player[i] = c.Item1;
                                        Log = c.Item2;
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
                                        var c = await Operation.Bexecution(uh, player[0], player[i], operations);
                                        player[0] = c.Item1;
                                        Log = c.Item2;
                                    }
                                }
                            }
                        }
                    }
                    return (player, enemy,Log);
                    break;
                }
                else
                {
                    Console.WriteLine("Not a valid action");
                }
            }
        }
    }
}