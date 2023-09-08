using AI;
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
                await Load(PlayerTurn(player, enemy, operations, enemy_names,GPT,system,menu,Log), "Thinking", "Thinking finished !");
                await Load(AiTurn(enemy, player, operations, true,GPT,system,menu,Log), "Thinking", "Thinking finished !");
                await Load(AiTurn(player, enemy, operations, false, GPT, system, menu, Log), "Thinking", "Thinking finished !");
                Log += "\n";
                Console.WriteLine(await Narrator(turn,GPT,system,menu, Log));
                turn++;
            }
        }
        public static async Task Load(Task task, string message, string final)
        {
            int dotCount = 0;
            while (true)
            {
                if (task.IsCompleted)
                {
                    Console.Write("\r   \r");
                    Console.Write($"{final}\n");

                    return;
                }
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
        }
        public static async Task<(Character, string)> Bexecution(string Operation_name, Character char1, Character char2, Dictionary<string, Operation> operations)
        {
            // Create a new operation based on the template from the dictionary
            var operationTemplate = Operation.GetOperation(operations, Operation_name);
            var operation = new Operation
            {
                Name = operationTemplate.Name,
                Operatio = operationTemplate.Operatio,
                Description = operationTemplate.Description,
                Values = new List<Value>(), // Initialize the values
                Target = operationTemplate.Target,
                TargetName = operationTemplate.TargetName,
                Log = operationTemplate.Log

            };

            operation.Values = new List<Value>();
            Regex regex = new Regex(@"\{.*?\}");

            // Matches variable parts in operation (stuff inside {})
            var matches = regex.Matches(operation.Operatio);
            foreach (Match match in matches)
            {
                var splitProp = match.Value.Trim('{', '}').Split('.');
                var propertyValue = 0f;

                if (splitProp[0] == "Char1")
                    propertyValue = (float)typeof(Character).GetProperty(splitProp[1]).GetValue(char1);
                else if (splitProp[0] == "Char2")
                    propertyValue = (float)typeof(Character).GetProperty(splitProp[1]).GetValue(char2);

                operation.Values.Add(new Value { Name = splitProp[1], Float = propertyValue });
            }

            operation.Operatio = regex.Replace(operation.Operatio, "");
            /*  for(int i = 0; i < operation.Values.Count;i++)
              {
                  Console.WriteLine(operation.Values[i].Float);
                  Console.WriteLine(operation.Values[i].Name);
              }*/
            float result = Operation.CalculateExpression(operation);
            Character target;
            if (operation.TargetName == "Char1")
            {
                target = char1;
            }
            else
            {
                target = char2;
            }
            PropertyInfo propertyInfo = target.GetType().GetProperty(operation.Target);
            propertyInfo.SetValue(target, result);

            string logString = operation.Log;

            logString = regex.Replace(logString, match =>
            {
                var splitProp = match.Value.Trim('{', '}').Split('.');
                var propertyName = splitProp[1];
                var propertyValue = "";

                if (splitProp[0] == "Char1" && char1.GetType().GetProperty(propertyName) != null)
                    propertyValue = char1.GetType().GetProperty(propertyName).GetValue(char1).ToString();
                else if (splitProp[0] == "Char2" && char2.GetType().GetProperty(propertyName) != null)
                    propertyValue = char2.GetType().GetProperty(propertyName).GetValue(char2).ToString();

                return string.IsNullOrEmpty(propertyValue) ? match.Value : propertyValue; // Keep original match if no value
            });

            matches = regex.Matches(logString);

            foreach (Match match in matches)
            {
                // Substitute variables with their values
                var expressionString = match.Value.Trim('{', '}');
                var expression = new NCalc.Expression(expressionString);
                expression.Parameters["Char1"] = char1;
                expression.Parameters["Char2"] = char2;
                expression.Parameters["result"] = result;

                var evalResult = "";
                try
                {
                    evalResult = expression.Evaluate().ToString();
                }
                catch (Exception ex)
                {
                    continue; // Or log this exception that evaluation failed
                }

                logString = logString.Replace(match.Value, evalResult);
            }

            logString = "\n" + logString;
            return (target, logString);
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
        public static async Task AiTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operation> operations, bool players,string GPT,string system,Menu menu,string Log)
        {
            JArray Response;
            if (players == true && player.Count == 1) return;
            if (players == true) enemy.Remove(enemy.First());
            for (int e = 0; e < enemy.Count; e++)
            {
                string ProcessedPrompt = $"Labels:\r\nReasoning(advantages,disadvantages,course_of_action)\r\nAction(name,target)\r\n\r\nTask: You are playing the role of {enemy[e]}. Your objective is to make prudent decisions based on the current game dynamics, prioritizing the team's interests and predicting possible moves from your allies and adversaries. Engage in a thorough analysis of potential actions by comparing their descriptions with your character's statistics to determine the best route. Use the label 'reasoning' along with its elements - 'advantages', 'disadvantages', and 'course of action' - to methodically evaluate all possibilities and ascertain the optimal course of action. This will involve weighing the merits of your character's stats against the disadvantages.\r\n\r\nThe 'name' label will denote your chosen action, whereas the 'target' label will indicate the entity at the receiving end of your action. Should you decide to direct the action towards yourself, utilize 'player' for the 'target'. Try to keep your reasoning short and to the point. You are only allowed to use one action, so choose carefully. Your available options are outlined below:\n";
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
                foreach (Character d in enemy)
                {
                    ProcessedPrompt += $"Ally: {d.Name}.\n";
                }
                Response = await AIHandler.Get_response(ProcessedPrompt, system, menu, 400, GPT);
                string uh = Json.GetValue(Response, "Action", "name");
                Console.WriteLine(Response);
                if (operations[uh].Ally == "False")
                {
                    for (int i = 0; i < player.Count; i++)
                    {

                        if (player[i].Name.ToLower() == Json.GetValue(Response, "Action", "target").ToLower())
                        {
                            foreach (Operation ope in operations.Values)
                            {
                                if (ope.Name == uh.ToLower())
                                {
                                    var c = await Bexecution(uh, enemy[e], player[i], operations);
                                    player[i] = c.Item1;
                                    Log += c.Item2;
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < enemy.Count; i++)
                {
                    if ((operations[uh].Ally == "True" && Json.GetValue(Response, "Action", "target").ToLower() != "player") || (Json.GetValue(Response, "Action", "target").ToLower() == "player" && operations[uh].Self.ToLower() == "true"))
                    {
                        if (enemy[i].Name.ToLower() == Json.GetValue(Response, "Action", "target").ToLower() || Json.GetValue(Response, "Action", "target").ToLower() == "player")
                        {
                            foreach (Operation ope in operations.Values)
                            {
                                if (ope.Name == uh.ToLower())
                                {
                                    var c = await Bexecution(uh, enemy[e], enemy[i], operations);
                                    enemy[i] = c.Item1;
                                    Log += c.Item2;
                                }
                            }
                        }
                    }
                }
            }
        }
        public static async Task PlayerTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operation> operations, string enemy_names,string GPT,string system,Menu menu,string Log)
        {
            string PlayerAction;
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
                    menu.Pause_menu(); GPT = menu.GPT;
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
                    string ProcessedPrompt = "Labels:\nAction(name,target)\n" +
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
                    string uh = Json.GetValue(Response, "Action", "name");
                    for (int i = 0; i < enemy.Count; i++)
                    {
                        if (operations[uh].Ally == "False")
                        {
                            if (enemy[i].Name.ToLower() == Json.GetValue(Response, "Action", "target").ToLower())
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh.ToLower())
                                    {
                                        var c = await Bexecution(uh, player[0], enemy[i], operations);
                                        enemy[i] = c.Item1;
                                        Log += c.Item2;
                                    }
                                }
                            }
                        }
                    }
                    for (int i = 0; i < player.Count; i++)
                    {
                        if (operations[uh].Ally == "True" && Json.GetValue(Response, "Action", "target").ToLower() != "player")
                        {
                            if (player[i].Name.ToLower() == Json.GetValue(Response, "Action", "target").ToLower())
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh.ToLower())
                                    {
                                        var c = await Bexecution(uh, player[0], player[i], operations);
                                        player[i] = c.Item1;
                                        Log += c.Item2;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (Json.GetValue(Response, "Action", "target").ToLower() == "player")
                            {
                                foreach (Operation ope in operations.Values)
                                {
                                    if (ope.Name == uh.ToLower())
                                    {
                                        var c = await Bexecution(uh, player[0], player[i], operations);
                                        player[0] = c.Item1;
                                        Log += c.Item2;
                                    }
                                }
                            }
                        }
                    }
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