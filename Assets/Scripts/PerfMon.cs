using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PerfMon
{
    private static Dictionary<string, Dictionary<string, FuncInfo>> data = new Dictionary<string, Dictionary<string, FuncInfo>>();

    private static bool disable_metrics = true;

    public static void Call(string ClassName, string FuncName, Int64 Runtime)
    {
        if (disable_metrics)
            return;

        Runtime = Ticks() - Runtime;

        // something to hold a match
        Dictionary<string, FuncInfo> test;
        FuncInfo index;

        // see if this class exists yet
        if (data.TryGetValue(ClassName, out test))
        {
            // found the class
            if (test.TryGetValue(FuncName, out index))
            {
                // found the function
                index.calls++;
                index.runtime += Runtime;
                data[ClassName][FuncName] = index;
            }
            else
            {
                // found class but no function
                index = new FuncInfo(Runtime);
                index.calls++;
                data[ClassName].Add(FuncName, index);
            }
        }
        else
        {
            // found no class
            test = new Dictionary<string, FuncInfo>();
            index = new FuncInfo(Runtime);
            index.calls++;
            test.Add(FuncName, index);
            data.Add(ClassName, test);
        }
    }

    public static void SetupFunc(string ClassName, string FuncName)
    {
        if (disable_metrics)
            return;

        // something to hold a match
        Dictionary<string, FuncInfo> test;

        if (!data.TryGetValue(ClassName, out test))
        {
            data.Add(ClassName, new Dictionary<string, FuncInfo>());
        }

        data[ClassName].Add(FuncName, new FuncInfo(0));
    }

    public static Int64 Ticks()
    {
        return DateTime.Now.Ticks;
    }

    public static void Report()
    {
        if (disable_metrics)
            return;

        string result = "";

        var classy = data.Keys.ToList();
        classy.Sort();

        foreach (string c in classy)
        {
            var funcy = data[c].Keys.ToList();
            funcy.Sort();

            foreach (string f in funcy)
            {
                result += "Class: " + c + ", Function: " + f + ", " + data[c][f].ToString() + "\r\n";
            }
        }

        UnityEngine.Debug.Log(result.TrimEnd());
    }
}

public struct FuncInfo
{
    public int calls { get; set; }
    public double runtime { get; set; }

    public FuncInfo(double Runtime)
    {
        calls = 0;
        runtime = Runtime;
    }

    public override string ToString()
    {
        return "Calls: " + calls + ", Runtime: " + Math.Round(runtime/TimeSpan.TicksPerSecond, 4);
    }
}