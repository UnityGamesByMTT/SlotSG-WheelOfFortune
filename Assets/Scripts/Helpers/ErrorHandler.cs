using System;
using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;


public class ErrorHandler
{

    public static void RunSafely(Action action, Action owner=null,[CallerMemberName] string context = "")
    {
        try
        {
            action.Invoke();
        }
        catch (Exception ex)
        {
            HandleError(ex, context,owner);
        }
    }

    public static T RunSafely<T>(Func<T> func, Action owner=null,[CallerMemberName] string context = "")
    {
        try
        {
            return func.Invoke();
        }
        catch (Exception ex)
        {
            HandleError(ex, context,owner);
            return default; 
        }
    }

    public static IEnumerator RunSafely(IEnumerator coroutine, Action owner=null,[CallerMemberName] string context = "")
    {
        while (true)
        {
            object current = null;

            try
            {
                if (!coroutine.MoveNext()) yield break;
                current = coroutine.Current;
            }
            catch (Exception ex)
            {
                HandleError(ex, context,owner);
                yield break; // Stop the coroutine if an error occurs
            }

            yield return current;
        }
    }

    private static void HandleError(Exception ex, string context, Action owner)
    {
        Debug.LogError($"Error in {context}: {ex.Message}\nStackTrace: {ex.StackTrace}");

        owner?.Invoke();
        
        // Optional: Add UI notification, retry logic, etc.
    }
}
