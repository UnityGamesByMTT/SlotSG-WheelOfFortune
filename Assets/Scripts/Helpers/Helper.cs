using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
public class Helper{


    internal List<List<int>> ConvertStringListsToIntLists(List<List<string>> stringLists)
    {
        List<List<int>> intLists = new List<List<int>>();

        foreach (var stringList in stringLists)
        {
            List<int> intList = new List<int>();
            foreach (var str in stringList)
            {
                if (int.TryParse(str, out int number))
                {
                    intList.Add(number);
                }
                else
                {
                    Debug.LogError($"Failed to convert '{str}' to an integer.");
                }
            }
            intLists.Add(intList);
        }
        
        return intLists;
    }

    internal  bool IsValidJson(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            return false;
        }

        try
        {
            JToken.Parse(jsonString);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


}