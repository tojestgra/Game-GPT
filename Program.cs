using AI;
using Types;
using Combat;
using Generic;
using Microsoft.CodeAnalysis;

namespace Gayme
{
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
        public string param { get; set; }
        public List<Basic> basic = new List<Basic>();
        public static string GPT;
        public Menu menu = new Menu();
        string Log;
        public async Task Start()
        {
            GPT = menu.GPT;
            Dictionary<string, Operation> operations = await Generation.OperationGeneration(3,"melee combat",GPT,system,menu);
            /*
            string operatio = "Health{Char2.Health} - (Attack{Char1.Attack}*2 - Defence{Char2.Defence})";
            Dictionary<string, Operation> operations = new Dictionary<string, Operation>();
            Operation op = new Operation()
            {
                Name = "attack",
                Description = "Uses attack stat to remove health",
                Values = new List<Value> { },
                Operatio = operatio,
                Target = "Health",
                TargetName = "Char2",
                Log = "{Char1.Name} attacked {Char2.Name}!",
                Ally = "False",
                Self = "False"
            };
            operations.Add(op.Name, op);
            operatio = "Health{Char1.Health} + (Defence{Char1.Defence} / 2)";
            op = new Operation()
            {
                Name = "heal",
                Description = "Heals the target by defence divided by 2",
                Values = new List<Value> { },
                Operatio = operatio,
                Target = "Health",
                TargetName = "Char2",
                Log = "{Char1.Name} healed {Char2.Name} !",
                Ally = "True",
                Self = "False"
            };
            operations.Add(op.Name, op);
            op = new Operation()
            {
                Name = "heal_self",
                Description = "Heals the character by defence divided by 2",
                Values = new List<Value> { },
                Operatio = operatio,
                Target = "Health",
                TargetName = "Char1",
                Log = "{Char1.Name} healed themselves !",
                Ally = "True",
                Self = "True"
            };
            operations.Add(op.Name, op);
                        */
            if(Debug.IsDebug)foreach (var operation in operations) { Console.WriteLine(operation.Value.Name);Console.WriteLine(operation.Value.Ally); }
            if (param == "combat")
            {
                Character player = new Character { Name = "Moo"};
                Character ally = new Character { Name = "Mark"};
                Character enemy = new Character { Name = "Glue"};
                Character smone = new Character { Name = "Jack"};
                List<Character> players = new List<Character>();
                List<Character> enemies = new List<Character>();
                players.Add(player);
                players.Add(ally);
                enemies.Add(enemy);
                enemies.Add(smone);
                await TurnCombat.Fight(players, enemies, operations,GPT,system,menu,Log);
            }
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