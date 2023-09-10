using Newtonsoft.Json.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Types
{
    public class Value
    {
        public string Name { get; set; }
        public float Float { get; set; }
    }
    public static class Json
    {
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
    }
    public class Operation
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Value> Values { get; set; }
        public string Operatio { get; set; }
        public string Target { get; set; }
        public string TargetName { get; set; }
        public string Log { get; set; }
        public string Ally { get; set; }
        public string Self { get; set; }
        public Operation()
        {
            Name = string.Empty;
            Description = string.Empty;
            Values = new List<Value>();
            Operatio = string.Empty;
            Target = string.Empty;
            TargetName = string.Empty;
            Log = $"";
            Ally = string.Empty;
            Self = string.Empty;
        }
        public static float CalculateExpression(Operation operations)
        {
            List<Value> values = operations.Values;
            var valueDictionary = new Dictionary<string, object>();
            string operation = operations.Operatio;
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
        public static Operation GetOperation(Dictionary<string, Operation> operations, string name)
        {
            return operations[name];
        }
        public static (Character, string) Bexecution(string Operation_name, Character char1, Character char2, Dictionary<string, Operation> operations)
        {
            // Create a new operation based on the template from the dictionary
            var operationTemplate = GetOperation(operations, Operation_name);
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
            float result = CalculateExpression(operation);
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
        public void Add_value(string name, string type, dynamic value, string[] propertySequence,List<Basic> basic)
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
        public dynamic Get_Value(string Name, string[] specific, int[] list, List<Basic> basic)
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

        public void UpdateBasicEntity(string Name, string[] propertySequence, dynamic newValue, List<Basic> basic)
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
    }
    public class Properties
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
    public class Item
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
    public class Character
    {
        public float Health { get; set; }
        public string Name { get; set; }
        public float Attack { get; set; }
        public float Defence { get; set; }
        public List<Properties> Properties { get; set; }
        public List<Item> Inventory { get; set; }
        public List<Item> Equipment { get; set; }
        public int NumOfProperties { get; set; }
        public Character()
        {
            Health = 12;
            Name = "Foo";
            Attack = 5;
            Defence = 2;
            Properties = new List<Properties>();
            Inventory = new List<Item>();
            Equipment = new List<Item>();
            NumOfProperties = 4;
        }
    }
}