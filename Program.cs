

using JsonSerialize;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

public static class JsonSerializer
{

    private static string getString(object val)
    {

        if(val == null)
        {
            return "null";
        }
        if(val is string)
        {
            return val.ToString();
        }
        if (val is bool)
        {
            return ((bool)val) ? "true" : "false";
        }
        if (val is int || val is double || val is float || val is decimal)
        {
            return val.ToString();
        }
        if(val is IList)
        {
            IList list = (IList)val;
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach(object o in list)
            {
                if(o is string || o.GetType().IsPrimitive)
                {
                    sb.Append(getString(o));
                    sb.Append(',');
                }
                else
                {
                    MethodInfo m = typeof(JsonSerializer).GetMethod("Serialize").MakeGenericMethod(o.GetType());
                    sb.Append((string)m.Invoke(null, new object[] {o}));
                    sb.Append(',');
                }
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(']');
            return sb.ToString();
        }
        MethodInfo rec = typeof(JsonSerializer).GetMethod("Serialize").MakeGenericMethod(val.GetType());

        return (string)rec.Invoke(null, new object[] {val});

    }
    public static string Serialize<T>(T data)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{");
        Type t = typeof(T);
        foreach (PropertyInfo pi in t.GetProperties())
        {
            var propValue = pi.GetValue(data);
            sb.Append("\"" + pi.Name + "\":" + getString(propValue));
            sb.Append(",");
        }
        sb.Remove(sb.Length - 1, 1);
        sb.Append("}");
        return sb.ToString();
    }


    public static T DeserializeRec<T>(Dictionary<string, object> JsonKV)
    {
        Type type = typeof(T);
        var properties = type.GetProperties();
        T res = (T)FormatterServices.GetUninitializedObject(type);
        foreach (var prop in properties)
        {
            if (prop.PropertyType.IsPrimitive || prop.PropertyType == typeof(string))
            {
                string k = $"\"{prop.Name}\"";
                prop.SetValue(res, Convert.ChangeType(JsonKV[k], prop.PropertyType));
                continue;
            }
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
            {
                string k = $"\"{prop.Name}\"";

                Type t = prop.PropertyType.GetGenericArguments()[0];

                Type listType = typeof(List<>).MakeGenericType(t);
                IList ListToSet = (IList)Activator.CreateInstance(listType);
                List<object> ListToSetFrom = (List<object>)JsonKV[k];
                foreach(object o in ListToSetFrom)
                {
                    if (t.IsPrimitive || t == typeof(string))
                    {
                        ListToSet.Add(Convert.ChangeType(o, t));
                    }
                    else
                    {
                        MethodInfo recMet = typeof(JsonSerializer).GetMethod("DeserializeRec").MakeGenericMethod(t);
                        ListToSet.Add(recMet.Invoke(null, new object[] { o }));
                    }
                    prop.SetValue(res, ListToSet);
                }
            }
            else
            {
                MethodInfo recMet = typeof(JsonSerializer).GetMethod("DeserializeRec").MakeGenericMethod(prop.PropertyType);
                string k = $"\"{prop.Name}\"";
                prop.SetValue(res, recMet.Invoke(null, new object[] { JsonKV[k] }));

            }
        }
        return res;
    }
    

    public static T Deserialize<T>(string json)
    {
        Dictionary<string, object> JsonKV = jsonParser.Parse(json);
        Type type = typeof(T);
        MethodInfo recMet = typeof(JsonSerializer).GetMethod("DeserializeRec").MakeGenericMethod(type);
        T res = (T)recMet.Invoke(null, new object[] {JsonKV});


        return res;
    }

    public static void Main()
    {
        string json = File.ReadAllText(@"C:\Users\zshakulashvili\source\repos\BankAccount\JsonSerialize\jsonExample.json");
        example e = Deserialize<example>(json);
        Console.WriteLine(e.name);
        menuClass m = e.menu;
        Console.WriteLine(m.food1);
        Console.WriteLine(m.food2);
        List<int> l = e.list;
        foreach (int i in l)
        {
            Console.WriteLine(i);
        }

        List<objExample> lo = e.listOfObjects;
        foreach (objExample objExample in lo)
        {
            Console.WriteLine(objExample.objName);
        }

        Console.WriteLine(Serialize<example>(e));
    }

    public class example
    {
        public menuClass menu { get; set; }
        public string name { get; set; }
        public List<int> list { get; set; }
        public List<objExample> listOfObjects { get; set; }
    }

    public class menuClass
    {
        public int food1 { get; set; }
        public int food2 { get; set;}

    }

    public class objExample
    {
        public string objName { get; set; }
    }
}