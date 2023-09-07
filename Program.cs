using System.Net.Http.Headers;
using System.Net.Http.Json;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Linq.Dynamic.Core;
using System.Text.RegularExpressions;
using System.Data;
using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;
using Antlr.Runtime;

namespace Gayme
{
    public class Value
    {
        public string Name { get; set; }
        public float Float { get; set; }
    }
    public class Operations
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Value> Values { get; set; }
        public string Operation { get; set; }
        public string Target { get; set; }
        public string TargetName { get; set; }
        public string Log { get; set; }
        public string Ally { get; set; }
        public string Self { get; set; }
        public Operations() 
        {
            Name = string.Empty;
            Description = string.Empty;
            Values = new List<Value>();
            Operation = string.Empty;
            Target = string.Empty;
            TargetName = string.Empty;
            Log = $"";
            Ally = string.Empty;
            Self = string.Empty;
        }
    }
    class Program
    {
        static async Task Main(string[] args)
        {
            Game game = new Game();
            game.param = "combat"; //for testing, remove later
            await game.Start();
        }
    }
    public class Game
    {
        public string system = "Your goal is to analyze a given situation and summarize its outcome by following certain guidelines defined through labels.\r\n\r\nThe input contains two essential components: 'Labels', which elucidate certain properties you need to assess; and 'Task', which presents a task you are to follow to fill out the labels, along with a related string. Your responsibility is to evaluate the provided scenario based on the properties defined by the labels.\r\n\r\nOnce you complete your analysis, the outputs are to be formatted, in order, as JSON objects. Specifically, for every label, an output object is created with fields corresponding to the properties mentioned in the label.An example:\r\n\r\nuser input: {\r\n  \"instruction\": \"Labels: [Label1](property1,property2)...[LabelN](property1,...,propertyN) Task: (task) : \"(String related to task)\"\",\r\n}\r\n\r\nexpected output from you: [\r\n  {\r\n    \"Object\": \"[Label1]\",\r\n    \"[property1]\": \"[value1]\",\r\n    \"[property2]\": \"[value2]\",\r\n  },\r\n  ...,\r\n  {\r\n    \"Object\": \"[LabelN]\",\r\n    \"[property1]\": \"[value1]\",\r\n    ...,\r\n    \"[propertyN]\": \"[valueN]\"\r\n  }\r\n]\r\n\r\nThe values assigned to each property should be based on your analysis of the task scenario. Make sure the output JSON objects accurately represent the task given in relation to the properties defined in the labels. Make sure to not omit any labels or properties. Remember to strictly adhere to JSON rules. Last of all, remember that you can only use a label and a property once.";
        public static string[] types = { "string", "int", "float", "double", "Item", "Basic", "Properties", "Character" };
        public string param { get; set; }
        public Combat combat;
        public AI ai = new AI();
        public Menu menu = new Menu();
        public List<Basic> basic = new List<Basic>();
        public async Task Start()
        {
            Dictionary<string, Operations> operations = await OperationGeneration(5,"melee combat");
            /*
            string operation = "Health{Char1.Health} - (Attack{Char1.Attack} - Defence{Char2.Defence})";
            Operations op = new Operations()
            {
                Name = "attack",
                Description = "Uses attack stat to remove health",
                Values = new List<Value> { },
                Operation = operation,
                Target = "Health",
                TargetName = "Char2",
                Log = "{Char1.Name} attacked {Char2.Name}!",
                Ally = "False",
                Self = "False"
            };
            operations.Add(op.Name,op);
            operation = "Health{Char1.Health} + (Defence{Char1.Defence} / 2)";
            op = new Operations()
            {
                Name = "heal",
                Description = "Heals the target by defence divided by 2",
                Values = new List<Value> { },
                Operation = operation,
                Target = "Health",
                TargetName = "Char2",
                Log = "{Char1.Name} healed {Char2.Name} !",
                Ally = "True",
                Self = "False"
            };
            operations.Add(op.Name, op);
            op = new Operations()
            {
                Name = "heal_self",
                Description = "Heals the character by defence divided by 2",
                Values = new List<Value> { },
                Operation = operation,
                Target = "Health",
                TargetName = "Char1",
                Log = "{Char1.Name} healed themselves !",
                Ally = "True",
                Self = "True"
            };
            operations.Add(op.Name, op);*/
            if (param == "combat")
            {
                combat = new Combat();
                Character player = new Character { Name = "Moo", Health = 20};
                Character enemy = new Character { Name = "Glue", Attack = 4, Defence = 1 };
                Character smone = new Character { Name = "Jack", Attack = 1, Defence = 3 };
                List<Character> players = new List<Character>();
                List<Character> enemies = new List<Character>();
                players.Add(player);
                enemies.Add(enemy);
                enemies.Add(smone);
                await combat.Fight(players, enemies, operations);
            }
        }
        public async Task<Dictionary<string, Operations>> OperationGeneration(int n,string theme)
        {
            string prompt = $"Labels: Operation(Name,Description,Operation,Target,TargetName,Log,Ally,Self) Task: Create {n} different operations format suitable for a game context, using this theme: theme {theme}. The Operation label comes with the following properties, values, and usage instructions:\r\n\r\n* Name: (string) This is the name of the operation. Any unique identifier you prefer. Example: 'attack or heal_self'.\r\n* Description: (string) This describes what the operation does in the game. Make sure its description matches its name. Example: 'Uses attack stat to remove health'.\r\n* operation: (delegate/lambda) It's a function that takes the character and a list of values to describe how the operation is performed. The entry should code in textual format. Example: 'Health{{Char1.Health}} - (Attack{{Char1.Attack}} - Defence{{Char2.Defence}})'.\r\n* Target: (string) This specifies what the operation targets in the game. It could target 'Health', 'Mana', 'Experience' etc. Example: 'Health'.\r\n* TargetName: (string) This is the name of the target character which the operation is applied on. Either Char1 or Char2. Char1 is the executor of the task, while Char2 is the recipient\r\n* Log: (string) This log is for logging the result of the operation in-game. It usually uses the Name properties of characters involved. Example: '{{Char1.Name}} attacked {{Char2.Name}}!'\r\n* Ally: (string) This determines if the operation can be used on allies. Acceptable entries are 'True' or 'False'. Example: 'False'.\r\n* Self: (string) This determines if the operation can be applied on self. Acceptable entries are 'True' or 'False'. Example: 'False'. The generated operations should be distinct.";
            JArray response = await ai.Get_response(prompt,system,menu,800);
            Dictionary<string, Dictionary<string, List<string>>> respon = new Dictionary<string, Dictionary<string, List<string>>>();
            List<Dictionary<string, List<string>>> objects = new List<Dictionary<string, List<string>>>();
            List<string> values = new List<string> {"Name","Description","Operation","Target","TargetName","Log","Ally","Self" };
            Dictionary<string, List<string>> a = new Dictionary<string, List<string>>();
            a.Add("Operation", values);
            objects.Add(a);
            respon = GetValuesGroup(response,objects);
            Dictionary<string, Operations> result = new Dictionary<string, Operations>();

            foreach (string key in respon.Keys)
            {
                Operations operation = new Operations();
                foreach (string valueKey in respon[key].Keys)
                {
                    switch (valueKey)
                    {
                        case "Name": operation.Name = respon[key][valueKey].FirstOrDefault(); break;
                        case "Description": operation.Description = respon[key][valueKey].FirstOrDefault(); break;
                        case "Operation": operation.Operation = respon[key][valueKey].FirstOrDefault(); break;
                        case "Target": operation.Target = respon[key][valueKey].FirstOrDefault(); break;
                        case "TargetName": operation.TargetName = respon[key][valueKey].FirstOrDefault(); break;
                        case "Log": operation.Log = respon[key][valueKey].FirstOrDefault(); break;
                        case "Ally": operation.Ally = respon[key][valueKey].FirstOrDefault(); break;
                        case "Self": operation.Self = respon[key][valueKey].FirstOrDefault(); break;
                    }
                }
                result[key] = operation;
            }

            return result;
        }
        public static string GetValue(JArray jsonArray, string Object, string value)
        {
            JToken objectA = jsonArray.FirstOrDefault(jt => jt["Object"].ToString() == Object);
            string valueA = objectA?[value].ToString();
            return valueA;
        }
        public static List<string> GetValues(JArray jsonArray, string objectName, string value)
        {
            List<JToken> matchingObjects = jsonArray.Where(jt => jt["Object"].ToString() == objectName).ToList();
            List<string> values = new List<string>();

            foreach (var obj in matchingObjects)
            {
                values.Add(obj[value].ToString());
            }

            return values;
        }
        public static Dictionary<string, Dictionary<string, List<string>>> GetValuesGroup(JArray jsonArray, List<Dictionary<string, List<string>>> objects)
        {
            Dictionary<string, Dictionary<string, List<string>>> matching = new Dictionary<string, Dictionary<string, List<string>>>();
            foreach (Dictionary<string, List<string>> dictionaries in objects)
            {
                foreach (string objec in dictionaries.Keys)
                {
                    foreach (List<string> list in dictionaries.Values)
                    {
                        foreach (string value in list)
                        {
                            List<string> strings = new List<string>();
                            Dictionary<string, List<string>> e = new Dictionary<string, List<string>>();
                            strings = GetValues(jsonArray, objec, value);
                            e.Add(value, strings);
                            if (matching.ContainsKey(objec))
                            {
                                if (matching[objec].ContainsKey(value))
                                {
                                    matching[objec][value].AddRange(strings);
                                }
                                else
                                {
                                    matching[objec].Add(value, strings);
                                }
                            }
                            else
                            {
                                matching.Add(objec, e);
                            }
                        }
                    }
                }
            }
            return matching;
        }
        public IDictionary<string, string> GetPropertiesNamesAndTypes(dynamic obj, string[] propertySequence = null, int index = 0)
        {
            IDictionary<string, string> propertiesMap = new Dictionary<string, string>();
            PropertyInfo propertyInfo = null;
            dynamic listIndex = null;

            if (!(obj is string) && !(obj is ValueType))
            {
                PropertyInfo[] properties = obj.GetType().GetProperties();
                foreach (PropertyInfo property in properties)
                {
                    propertiesMap.Add(property.Name, property.PropertyType.Name);
                }
            }

            if (propertySequence != null && index < propertySequence.Length)
            {
                if (propertySequence[index].StartsWith("Index"))
                {
                    listIndex = int.Parse(propertySequence[index].Substring(5));
                }
                else
                {
                    propertyInfo = obj.GetType().GetProperty(propertySequence[index]);
                    if (propertyInfo == null)
                        throw new ArgumentException($"No property with name '{propertySequence[index]}' found!");

                    obj = propertyInfo.GetValue(obj);
                    if (listIndex != null)
                    {
                        IList<dynamic> listObj = obj as IList<dynamic>;
                        if (listObj != null && listObj.Count > (int)listIndex)
                        {
                            obj = listObj[(int)listIndex];
                            listIndex = null;
                        }
                    }

                    IDictionary<string, string> nestedPropertiesMap = GetPropertiesNamesAndTypes(obj, propertySequence, listIndex == null ? index + 1 : index);
                    propertiesMap = MergeDictionaries(propertiesMap, nestedPropertiesMap);
                }
            }

            return propertiesMap;
        }

        private IDictionary<string, string> MergeDictionaries(IDictionary<string, string> dict1, IDictionary<string, string> dict2)
        {
            foreach (var item in dict2)
            {
                dict1[item.Key] = item.Value;
            }
            return dict1;
        }
        public void Add_value(string name, string type, dynamic value, string[] propertySequence)
        {
            dynamic obj = null;
            foreach (Basic b in basic)
            {
                if (b.Name == name && b.Type == type)
                {
                    obj = b.Value;
                    break;
                }
            }

            if (obj == null)
            {
                basic.Add(new Basic(name, type, value));
                return;
            }

            PropertyInfo propertyInfo = null;
            dynamic listIndex = null;
            for (int i = 0; i < propertySequence.Length; i++)
            {
                if (propertySequence[i].StartsWith("Index"))
                {
                    listIndex = int.Parse(propertySequence[i].Substring(5));
                }
                else
                {
                    propertyInfo = obj.GetType().GetProperty(propertySequence[i]);
                    if (propertyInfo == null)
                        throw new ArgumentException($"No property with name '{propertySequence[i]}' found!");

                    obj = propertyInfo.GetValue(obj);
                    if (listIndex != null)
                    {
                        IList<dynamic> listObj = obj as IList<dynamic>;
                        if (listObj != null)
                        {
                            obj = listObj[(int)listIndex];
                            listIndex = null;
                        }
                    }
                }
            }
            if (listIndex != null)
            {
                IList<dynamic> listObjEnd = obj as IList<dynamic>;
                if (listObjEnd != null)
                {
                    listObjEnd.Add(value);
                }
            }
        }
        public dynamic Get_Value(string Name, string[] specific, int[] list)
        {
            Basic basicMatch = null;
            foreach (Basic b in basic)
            {
                if (b.Name == Name)
                {
                    basicMatch = b;
                    break;
                }
            }
            if (basicMatch == null)
            {
                return null;
            }

            switch (basicMatch.Type)
            {
                case "string":
                case "int":
                case "double":
                    return basicMatch.Value;
                case "Item":
                    return GetValueForItem(basicMatch.Value as Item, specific, list);
                case "Character":
                    return GetValueForCharacter(basicMatch.Value as Character, specific, list);
                case "Properties":
                    return GetValueForProperties(basicMatch.Value as Properties, specific, list);
                default:
                    return null;
            }
        }

        private dynamic GetValueForItem(Item item, string[] specific, int[] list)
        {
            if (specific[0] == "Property")
            {
                Properties matchedProperty = null;
                foreach (Properties p in item.Properties)
                {
                    if (p.Name == specific[1])
                    {
                        matchedProperty = p;
                        break;
                    }
                }
                return matchedProperty;
            }
            if (list.Length > 0)
            {
                List<Properties> propertiesList = new List<Properties>();
                for (int i = 0; i < item.Properties.Count; i++)
                {
                    if (list.Contains(i))
                    {
                        propertiesList.Add(item.Properties[i]);
                    }
                }
                return propertiesList;
            }
            return item;
        }

        private dynamic GetValueForCharacter(Character character, string[] specific, int[] list)
        {
            switch (specific[0])
            {
                case "Properties":
                    if (list.Length > 0)
                    {
                        List<Properties> propertiesList = new List<Properties>();
                        for (int i = 0; i < character.Properties.Count; i++)
                        {
                            if (list.Contains(i))
                            {
                                propertiesList.Add(character.Properties[i]);
                            }
                        }
                        return propertiesList;
                    }
                    return character.Properties;
                case "Inventory":
                    if (list.Length > 0)
                    {
                        List<Item> itemsList = new List<Item>();
                        for (int i = 0; i < character.Inventory.Count; i++)
                        {
                            if (list.Contains(i))
                            {
                                itemsList.Add(character.Inventory[i]);
                            }
                        }
                        return itemsList;
                    }
                    return character.Inventory;
                case "Equipment":
                    if (list.Length > 0)
                    {
                        List<Item> itemsList = new List<Item>();
                        for (int i = 0; i < character.Equipment.Count; i++)
                        {
                            if (list.Contains(i))
                            {
                                itemsList.Add(character.Equipment[i]);
                            }
                        }
                        return itemsList;
                    }
                    return character.Equipment;
                default:
                    return null;
            }
        }

        private dynamic GetValueForProperties(Properties properties, string[] specific, int[] list)
        {
            // Returns the properties instance directly since there is no further depth
            return properties;
        }
        public void ModifyValue(dynamic obj, string[] propertySequence, dynamic newValue)
        {
            PropertyInfo propertyInfo = null;
            dynamic listIndex = null;

            for (int i = 0; i < propertySequence.Length; i++)
            {
                if (propertySequence[i].StartsWith("Index"))
                {
                    listIndex = int.Parse(propertySequence[i].Substring(5));
                }
                else
                {
                    propertyInfo = obj.GetType().GetProperty(propertySequence[i]);
                    if (propertyInfo == null)
                        throw new ArgumentException($"No property with name '{propertySequence[i]}' found!");
                    if (i == propertySequence.Length - 1)
                        break;
                    obj = propertyInfo.GetValue(obj);
                    if (listIndex != null)
                    {
                        IList<dynamic> listObj = obj as IList<dynamic>;
                        if (listObj != null)
                        {
                            obj = listObj[(int)listIndex];
                            listIndex = null;
                        }
                    }
                }
            }
            if (listIndex != null)
            {
                IList<dynamic> listObjEnd = obj as IList<dynamic>;
                if (listObjEnd != null)
                {
                    listObjEnd[(int)listIndex] = newValue;
                }
            }
            else
            {
                propertyInfo.SetValue(obj, newValue);
            }
        }

        public void UpdateBasicEntity(string Name, string[] propertySequence, dynamic newValue)
        {
            Basic basicMatch = null;
            foreach (Basic b in basic)
            {
                if (b.Name == Name)
                {
                    basicMatch = b;
                    break;
                }
            }
            if (basicMatch == null)
            {
                return;
            }

            ModifyValue(basicMatch.Value, propertySequence, newValue);
        }
        public float CalculateExpression(Operations operations)
        {
            List<Value> values = operations.Values;
            var valueDictionary = new Dictionary<string, object>();
            string operation = operations.Operation;

            foreach (var value in values)
            {
                valueDictionary.Add(value.Name, value.Float);
            }
            /*
            Console.WriteLine($"\nOperation: {operation}");
            Console.WriteLine($"Values: {string.Join(", ", values.Select(v => v.Name + ":" + v.Float))}");
            Console.WriteLine($"Parameters: {string.Join(", ", valueDictionary.Keys)}");
            */
            var parameter = valueDictionary.Keys.Select(k => System.Linq.Expressions.Expression.Parameter(typeof(float), k)).ToArray();
            var expression = DynamicExpressionParser.ParseLambda(ParsingConfig.Default, parameter, null, operation);
            var compiled = expression.Compile();
            var result = compiled.DynamicInvoke(valueDictionary.Values.ToArray());

            return Convert.ToSingle(result);
        }
        public Operations GetOperation(Dictionary<string, Operations> operations, string name)
        {
             return operations[name];
        }
    }
    public class Combat : Game
    {
        string Log = "";
        string Player_action;
        string Prompt;
        string Procesed_Prompt;
        JArray Response;
        int turn = 1;
        public async Task Fight(List<Character> player, List<Character> enemy, Dictionary<string, Operations> operations)
        {
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
                await Load(PlayerTurn(player, enemy, operations, enemy_names),"Thinking","Thinking finished !");
                await Load(AiTurn(enemy, player, operations,true), "Thinking", "Thinking finished !");
                await Load(AiTurn(player, enemy, operations, false), "Thinking", "Thinking finished !");
                Log += "\n";
                Console.WriteLine(await Narrator(Log,turn));
                turn++;
            }
        }
        public async Task Load(Task task, string message, string final)
        {
            int dotCount = 0;
            while (true)
            {
                if(task.IsCompleted)
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
                else if(dotCount == 0)
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
        public async Task<(Character,string)> Bexecution(string Operation_name, Character char1, Character char2, Dictionary<string, Operations> operations)
        {
            // Create a new operation based on the template from the dictionary
            var operationTemplate = GetOperation(operations, Operation_name);
            var operation = new Operations
            {
                Name = operationTemplate.Name,
                Operation = operationTemplate.Operation,
                Description = operationTemplate.Description,
                Values = new List<Value>(), // Initialize the values
                Target = operationTemplate.Target,
                TargetName = operationTemplate.TargetName,
                Log = operationTemplate.Log

            };

            operation.Values = new List<Value>();
            Regex regex = new Regex(@"\{.*?\}");

            // Matches variable parts in operation (stuff inside {})
            var matches = regex.Matches(operation.Operation);
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

            operation.Operation = regex.Replace(operation.Operation, "");
            /*  for(int i = 0; i < operation.Values.Count;i++)
              {
                  Console.WriteLine(operation.Values[i].Float);
                  Console.WriteLine(operation.Values[i].Name);
              }*/
            float result = CalculateExpression(operation);
            Character target;
            if(operation.TargetName=="Char1")
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
        public async Task<string> Narrator(string Log,int turn)
        {
            Procesed_Prompt = $"Labels:\nNarration(narration)\nTask: You are the narrator, your goal is to summarize an action / actions performed by characters in a situation. Use narration to give a short and flavorful descriptions of actions characters in the log string have taken. Narrate for the current turn, but also base a bit of your narration on what has previously happened.\nThe current turn is: {turn}. Log:\n{Log}";
            Console.WriteLine(Procesed_Prompt);
            Response = await ai.Get_response(Procesed_Prompt, system, menu,200);
            string narration = GetValue(Response,"Narration","narration");
            return narration;
        }
        public async Task AiTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operations> operations,bool players)
        {
            if (players == true && player.Count == 1) return;
            if (players == true) enemy.Remove(enemy.First());
                for (int e = 0; e < enemy.Count; e++)
                {
                    Procesed_Prompt = $"Labels:\r\nReasoning(advantages,disadvantages,course_of_action)\r\nAction(name,target)\r\n\r\nTask: You are playing the role of {enemy[e]}. Your objective is to make prudent decisions based on the current game dynamics, prioritizing the team's interests and predicting possible moves from your allies and adversaries. Engage in a thorough analysis of potential actions by comparing their descriptions with your character's statistics to determine the best route. Use the label 'reasoning' along with its elements - 'advantages', 'disadvantages', and 'course of action' - to methodically evaluate all possibilities and ascertain the optimal course of action. This will involve weighing the merits of your character's stats against the disadvantages.\r\n\r\nThe 'name' label will denote your chosen action, whereas the 'target' label will indicate the entity at the receiving end of your action. Should you decide to direct the action towards yourself, utilize 'player' for the 'target'. Try to keep your reasoning short and to the point. You are only allowed to use one action, so choose carefully. Your available options are outlined below:\n";
                    foreach (Operations d in operations.Values)
                    {
                        Procesed_Prompt += $"Action name: {d.Name}. Action description: {d.Description}. Used on friendly: {d.Ally}. Used on self: {d.Self}.\n";
                    }
                    Procesed_Prompt += "Enemies:\n\n";
                    foreach (Character d in player)
                    {
                        Procesed_Prompt += $"Enemy: {d.Name}.\n";
                    }
                    Procesed_Prompt += "\nAllies:\n\n";
                    foreach (Character d in enemy)
                    {
                        Procesed_Prompt += $"Ally: {d.Name}.\n";
                    }
                    Response = await ai.Get_response(Procesed_Prompt, system, menu,400);
                    string uh = GetValue(Response, "Action", "name");
                    Console.WriteLine(Response);
                    if (operations[uh].Ally == "False")
                    {
                        for (int i = 0; i < player.Count; i++)
                        {

                            if (player[i].Name.ToLower() == GetValue(Response, "Action", "target").ToLower())
                            {
                                foreach (Operations ope in operations.Values)
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
                        if ((operations[uh].Ally == "True" && GetValue(Response, "Action", "target").ToLower() != "player")||(GetValue(Response, "Action", "target").ToLower() == "player" && operations[uh].Self.ToLower() == "true"))
                        {
                            if (enemy[i].Name.ToLower() == GetValue(Response, "Action", "target").ToLower()|| GetValue(Response, "Action", "target").ToLower() == "player")
                            {
                                foreach (Operations ope in operations.Values)
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
        public async Task PlayerTurn(List<Character> player, List<Character> enemy, Dictionary<string, Operations> operations, string enemy_names)
        {
            while (true)
            {
                Console.WriteLine("What do you want to do ?\n type Help for help");
                Player_action = Console.ReadLine().ToLower();
                if (Player_action.ToLower() == "local")
                {
                    ai.GPT = "local";
                }
                else if (Player_action.ToLower() == "help")
                {
                    Console.WriteLine("Type:\nAct - Action that will be given to the ai for processing, can be anything you wanna do, uses turn" +
                        "\n Menu - go to the menu\nLook - inspect a specific enemy");
                }
                else if (Player_action.ToLower() == "menu")
                {
                    menu.Pause_menu(); ai.GPT = menu.GPT;
                }
                else if (Player_action.ToLower() == "look")
                {
                    Console.WriteLine("Which enemy do you want to inspect ?");
                    Console.WriteLine(enemy_names);
                    Player_action = Console.ReadLine();
                    for (int i = 0; i < enemy.Count; i++)
                    {
                        if (enemy[i].Name.ToLower() == Player_action)
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
                else if (Player_action.ToLower() == "act")
                {
                    Console.WriteLine("What action?");
                    Prompt = Console.ReadLine();
                    Procesed_Prompt = "Labels:\nAction(name,target)\n" +
                                      "Task: Your task is to determine which action the player wants to take depending on what they said. Use name for the name of the action which the player wants to take. Use target to type the name of whatever creature the player wants to inflict the action on. If the player says they want to inflict some action on themselves, return target as \"player\". In that case, if the action the player chooses is ambigous between multiple different actions, choose the one that has Used on self as True. If information from the player is missing replace it with what you think the player most likely would do. avaliable actions:\n";
                    foreach (Operations d in operations.Values)
                    {
                        Procesed_Prompt += $"Action name: {d.Name}. Action description: {d.Description}. Used on friendly: {d.Ally}. Used on self: {d.Self}.\n";
                    }
                    Procesed_Prompt += "Enemies:\n\n";
                    foreach (Character d in enemy)
                    {
                        Procesed_Prompt += $"Enemy: {d.Name}.\n";
                    }
                    Procesed_Prompt += "\nAllies:\n\n";
                    foreach (Character d in player)
                    {
                        Procesed_Prompt += $"Ally: {d.Name}.\n";
                    }
                    Procesed_Prompt += "\n\nPlayers action: " + Prompt;
                    Response = await ai.Get_response(Procesed_Prompt, system, menu,400);
                    string uh = GetValue(Response, "Action", "name");
                    for (int i = 0; i < enemy.Count; i++)
                    {
                        if (operations[uh].Ally == "False")
                        {
                            if (enemy[i].Name.ToLower() == GetValue(Response, "Action", "target").ToLower())
                            {
                                foreach (Operations ope in operations.Values)
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
                        if (operations[uh].Ally == "True" && GetValue(Response, "Action", "target").ToLower() != "player")
                        {
                            if (player[i].Name.ToLower() == GetValue(Response, "Action", "target").ToLower())
                            {
                                foreach (Operations ope in operations.Values)
                                {
                                    if (ope.Name == uh.ToLower())
                                    {
                                        var c = await Bexecution(uh, player[0], player[i], operations);
                                        player[i] = c.Item1;
                                        Log +=  c.Item2;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (GetValue(Response, "Action", "target").ToLower() == "player")
                            {
                                foreach (Operations ope in operations.Values)
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
    public class Basic
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public dynamic Value { get; set; }
        public Basic(string name, string type, dynamic value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }
    public class Properties : Game
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Effect { get; set; }
        public double Value { get; set; }
        public Properties()
        {
            Name = "Foo";
            Description = "Bar";
            Effect = "Attack";
            Value = 10.0;
        }
    }
    public class Item : Game
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Properties> Properties { get; set; }
        public bool Wearable { get; set; }
        public Item()
        {
            Name = "Foo";
            Description = "Bar";
            Properties = new List<Properties>();
            Wearable = false;
        }

    }
    public class Character : Game
    {
        public float Health { get; set; }
        public string Name { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public List<Properties> Properties { get; set; }
        public List<Item> Inventory { get; set; }
        public List<Item> Equipment { get; set; }
        public Character()
        {
            Health = 12;
            Name = "Foo";
            Attack = 1;
            Defence = 1;
            Properties = new List<Properties>();
            Inventory = new List<Item>();
            Equipment = new List<Item>();
        }
    }
    public class Menu
    {
        public string GPT { get; set; }
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
                else if (option == "options") { Menu_Options();}
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
                else if (option == "options") { Menu_Options();}
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
    public class AI
    {
        public string GPT { get; set; }
        public JArray Response { get; set; }
        JToken Process { get; set; }
        public AI()
        {
            GPT = "gpt-4";
        }
        public async Task<JArray> Get_response(string prompt, string system, Menu menu,int tokens)
        {
            while (true)
            {
                if (GPT == "gpt-4" || GPT == "gpt-3.5-turbo")
                {
                    Process = await OpenAI_GPT.CallGPTGeneric(GPT, system, prompt,tokens);
                    Console.WriteLine(Process);
                    Response = JArray.Parse(Process.ToString());
                    return Response;
                }
                else if (GPT == "local")
                {
                    Local_GPT local_GPT = new Local_GPT();
                    prompt = "This is your system message, it is a message directing you on how you should act and what you should do, above all else that will be said later. You have to follow the system message no matter what. The system message will end like this: (end of system message). The system message:[\n\n" + system + "\n\n](end of system message)\n\n\n" + prompt+"\noutput: ";
                    Process = await local_GPT.Run(prompt,tokens);
                    Response = JArray.Parse(Process.ToString());
                    return Response;
                }
                else { Console.WriteLine("No valid gpt selected ! Select one NOW"); menu.Menu_Options(); }
            }

        }
    }
    public class Local_GPT
    {
        private static readonly string HOST = "localhost:5005";
        private static readonly string URI = $"ws://{HOST}/api/v1/stream";

        public async Task<string> Run(string prompt,int tokens)
        {
            Console.WriteLine(prompt);
            var httpClient = new HttpClient();
            var content = new
            {
                prompt = prompt,
                max_new_tokens = tokens,
                auto_max_new_tokens = false,
                preset = "None",
                do_sample = true,
                temperature = 0.7,
                top_p = 0.1,
                typical_p = 1,
                epsilon_cutoff = 0,
                eta_cutoff = 0,
                tfs = 1,
                top_a = 0,
                repetition_penalty = 1.18,
                repetition_penalty_range = 0,
                top_k = 40,
                min_length = 0,
                no_repeat_ngram_size = 0,
                num_beams = 1,
                penalty_alpha = 0,
                length_penalty = 1,
                early_stopping = true,
                mirostat_mode = 0,
                mirostat_tau = 5,
                mirostat_eta = 0.1,
                guidance_scale = 1,
                negative_prompt = "",
                seed = -1,
                add_bos_token = true,
                truncation_length = 2048,
                ban_eos_token = false,
                skip_special_tokens = false,
                stopping_strings = new string[] { }
            };

            var uri = "http://localhost:5000/api/v1/generate";
            var jsonContent = JsonConvert.SerializeObject(content);
            var data = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var result = await httpClient.PostAsync(uri, data);
            if (result.IsSuccessStatusCode)
            {
                var response = await result.Content.ReadAsStringAsync();
                var JsonResult = JsonConvert.DeserializeObject<dynamic>(response);
                var resultText = JsonResult.results[0].text;
                return resultText;
            }
            return "oops";
        }
    }
    public class OpenAI_GPT
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        static string GetApiKey()
        {
            string path = "OpenAI_Key\\key.txt";
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
            return "Key not found !";
        }
        public OpenAI_GPT()
        {
            string apiKey = GetApiKey();
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }

        public async Task<string> SendPrompt(string prompt, string model, double temperature, string system, int tokens)
        {
            var requestBody = new
            {
                messages = new[]
                {
                  new { role = "system", content = system },
                  new { role = "user", content = prompt }
                 },
                model = model,
                max_tokens = tokens,
                temperature = temperature
            };
            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestBody);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Error sending prompt to GPT API:");
                Console.WriteLine(response.StatusCode);
                Console.WriteLine(await response.Content.ReadAsStringAsync());
                return null;
            }
            var responseBody = await response.Content.ReadAsStringAsync();

            return responseBody;
        }
        public static async Task<JToken> CallGPTGeneric(string gpt, string system, string prompt,int tokens)
        {
            var gptAgent = new OpenAI_GPT();
        v:
            string Prompt = $"{prompt}";
            var Response = await gptAgent.SendPrompt(Prompt, gpt, 0.7, $"{system}", tokens);
            if (Response == null) { Console.WriteLine($"Didn't recieve a response from GPT, server is probably overloaded, try again ? y\\n"); string asl = Console.ReadLine()!; if (asl == "y") { goto v; } return "no command gpt response"; }
            JObject JsonResponse = JObject.Parse(Response);
            if (JsonResponse.SelectToken("$.choices[0].message.content") == null) { goto v; }
            JToken ListToken = JsonResponse.SelectToken("$.choices[0].message.content")!;
            return ListToken;
        }
    }
}

/*
            Properties damage = new Properties()
            {
                Name = "Damage",
                Description = "Increases attack",
                Effect = "Attack Buff",
                Value = 50.0
            };
            Add_value("DamageProperty", "Properties", damage, new string[] { });

            Item sword = new Item()
            {
                Name = "Sword",
                Description = "A sharp blade",
                Properties = new List<Properties>() { damage },
                Wearable = true
            };
            Add_value("SwordItem", "Item", sword, new string[] { });

            Character character = new Character()
            {
                Health = 100,
                Name = "Hero",
                Attack = 10,
                Defence = 20,
                Properties = new List<Properties>(),
                Inventory = new List<Item>() { sword },
                Equipment = new List<Item>()
            };
            Add_value("HeroCharacter", "Character", character, new string[] { });
            // Fetch the character 'HeroCharacter' from basic list
            Basic foundCharacterBasic = null;
            foreach (Basic b in basic)
            {
                if (b.Name == "HeroCharacter" && b.Type == "Character")
                {
                    foundCharacterBasic = b;
                    break;
                }
            }

            if (foundCharacterBasic != null)
            {
                Character foundCharacter = foundCharacterBasic.Value as Character;

                Console.WriteLine(foundCharacter.Name + "'s Inventory:");
                foreach (Item item in foundCharacter.Inventory)
                {
                    Console.WriteLine("Item: " + item.Name);
                    Console.WriteLine("Description: " + item.Description);
                    foreach (Properties property in item.Properties)
                    {
                        Console.WriteLine("Properties: " + property.Name + ", " + property.Description);
                    }
                }
            }
            */