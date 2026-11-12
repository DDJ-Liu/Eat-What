// Additional logging boundary only; the original F01 Unity/CM boundary files remain unchanged.
using System.Collections.Generic;
namespace UnityEngine
{
    public static class Debug
    {
        public static readonly List<string> Lines=new List<string>();
        public static void LogWarning(string line,object context){}
        public static void Log(object line){Lines.Add(line.ToString());}
    }
}
