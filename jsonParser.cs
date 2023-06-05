using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;

namespace JsonSerialize
{


    static class jsonParser
    {
        private static readonly HashSet<char> _chars = new HashSet<char>{'\n', '\t',
                                                            '\r', '\b', '\f', '\a', '\0', '\v'};

        private static bool IsEscapeChar(char c)
        {
            return _chars.Contains(c);
        }

        private static Dictionary<int,int> GetBracketPairs(StringBuilder sb)
        {
            Dictionary<int, int> res = new Dictionary<int, int>();
            Stack<int> stack = new Stack<int>();

            for(int i = 0; i < sb.Length; i++)
            {
                char c = sb[i];
                if (sb[i] == '{' || sb[i] == '[')
                {
                    stack.Push(i);
                    continue;
                }
                if (sb[i] == '}' || sb[i] == ']')
                {
                    int ind = stack.Pop();
                    res[ind] = i;
                }
            }

            return res;
        }

        public static Dictionary<string, object> toDict(StringBuilder sb, Dictionary<int, int> bracketPairs
            , int left, int right){

            Dictionary<string, object> res = new Dictionary<string, object>();
            StringBuilder curString = new StringBuilder();
            Stack<StringBuilder> st = new Stack<StringBuilder>();

            for (int i = left; i < right; i++)
            {

                if (sb[i] == ',')
                {
                    st.Push(curString);
                    string val = st.Pop().ToString();
                    string key = st.Pop().ToString();
                    res[key] = val;
                    curString = new StringBuilder();
                    continue;
                }

                if (sb[i] == ':')
                {
                    st.Push(curString);
                    curString = new StringBuilder();
                    continue;
                }
                if (sb[i] == '{')
                {
                    string key = st.Pop().ToString();
                    res[key] = toDict(sb, bracketPairs, i + 1, bracketPairs[i]);
                    i = bracketPairs[i] + 1;
                    continue;
                }
                if (sb[i] == '[')
                {
                    List<object> list = new List<object>();
                    StringBuilder t = new StringBuilder();
                    for(int j = i + 1; j < bracketPairs[i]; j++)
                    {
                        if(sb[j] == '{')
                        {
                            object temp = toDict(sb, bracketPairs, j + 1, bracketPairs[j]);
                            list.Add(temp);
                            j = bracketPairs[j] + 1;
                            t = new StringBuilder();
                            continue;
                        }
                        if(sb[j] == ',')
                        {
                            list.Add(t.ToString());
                            t = new StringBuilder();
                            continue;
                        }
                        t.Append(sb[j]);
                    }
                    if (t.Length > 0) list.Add(t.ToString());
                    i = bracketPairs[i] + 1;
                    string key = st.Pop().ToString();
                    res[key] = list;
                    continue;
                }
                curString.Append(sb[i]);
            }

            if(st.Count != 0)
                st.Push(curString);
            while (st.Count > 0)
            {
                string val = st.Pop().ToString();
                string key = st.Pop().ToString();
                res[key] = val;
            };
            //foreach (KeyValuePair<string, string> kv in res)
            //{
            //    Console.WriteLine($"Key: {kv.Key}, Value: {kv.Value}");
            //}

            return res;
        }


        private static void printDict(Dictionary<string, object> dict)
        {

            foreach(KeyValuePair<string, object> kvp in dict)
            {
                object ob = kvp.Value;
                if(ob is string)
                {
                    Console.WriteLine(kvp.Key + " : " + (string)ob);
                    continue;
                }
                if(ob is List<object>)
                {
                    Console.WriteLine(kvp.Key + " : " + "list");
                    List<object> r = (List<object>)ob;
                    
                    Console.WriteLine("size : " + r.Count);
                    //foreach (object o in (List<object>)ob)
                    //{
                    //    printDict((Dictionary<string, object>)o);
                    //}
                    foreach (object o in r)
                    {
                        Console.WriteLine(o.ToString());
                    }
                    continue;
                }
                if(ob is Dictionary<string,object>)
                {
                    Console.WriteLine(kvp.Key);
                    printDict((Dictionary<string, object>)ob);
                }
            }
        }
        public static Dictionary<string, object> Parse(string json)
        {
            //List<KeyValuePair<string, string>> res;
            StringBuilder sb = new StringBuilder();
            bool BracketIsOpen = false;

            for(int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (IsEscapeChar(c)) continue;
                if (c == ' ' && !BracketIsOpen) continue;
                if (c == '"')
                {
                    sb.Append('"');
                    BracketIsOpen = BracketIsOpen ? false : true;
                    continue;
                }
                sb.Append(c);
            }

            Console.WriteLine(sb) ;
            Dictionary<int, int> dict = GetBracketPairs(sb);
            Dictionary<string, object> keyValuePairs = toDict(sb, dict, 1, sb.Length - 1);
            //printDict(keyValuePairs);
            //Dictionary<string, string> keyValuePairsk = JsonToDict(sb, dict, 1, sb.Length - 1);
            return keyValuePairs;
        }

    }
}
