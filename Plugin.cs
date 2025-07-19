using System.Diagnostics;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Wildcards;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Word Play.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        Harmony.CreateAndPatchAll(typeof(WordCheckerPatch));
    }
}

class TrieStorageDummyBehaviour : MonoBehaviour
{
    public Task<LengthwisePrefixTrie> Trie;
}

class WordCheckerPatch {
    [HarmonyPatch(typeof(WordChecker), "LoadData")]
    [HarmonyPostfix]
    static void LoadDataPostfix(WordChecker __instance, string[] ___validWords)
    {
        var component = __instance.gameObject.GetComponent<TrieStorageDummyBehaviour>();
        if (component == null)
        {
            component = __instance.gameObject.AddComponent<TrieStorageDummyBehaviour>();
        }
        component.Trie = Task.Run(() =>
        {
            var watch = Stopwatch.StartNew();
            var trie = new LengthwisePrefixTrie(___validWords);
            Plugin.Logger.LogInfo($"Baked trie in {watch.ElapsedMilliseconds} ms");
            return trie;
        });
    }
    
    [HarmonyPatch(typeof(WordChecker), "DoWildCardMatch")]
    [HarmonyPrefix]
    static bool DoWildCardMatchPrefix(string pattern, bool changeWordInSubmit, bool appendRE, WordChecker __instance, ref bool __result, ref string ___latestWord)
    {
        var component = __instance.gameObject.GetComponent<TrieStorageDummyBehaviour>();
        if (component == null)
        {
            Plugin.Logger.LogError("Failed to fetch constructed trie!");
            return true;
        }
        string str = component.Trie.Result.PatternMatches(pattern, false);
        if (str == null)
        {
            __result = false;
            return false;
        }
        ___latestWord = str;
        if (appendRE)
            ___latestWord = "RE" + ___latestWord;
        if (changeWordInSubmit)
        {
            Debug.Log("Changing Current Word in Submit to " + ___latestWord);
            SubmitWord.Instance.ReplaceCurrentWord(___latestWord);
        }
        __result = true;
        return false;
    }
    
    [HarmonyPatch(typeof(WordChecker), "SandZMatch")]
    [HarmonyPrefix]
    static bool SandZMatchPrefix(string pattern, bool changeSubmitWord, bool appendRE, WordChecker __instance, ref bool __result, ref string ___latestWord)
    {
        var component = __instance.gameObject.GetComponent<TrieStorageDummyBehaviour>();
        if (component == null)
        {
            Plugin.Logger.LogError("Failed to fetch constructed trie!");
            return true;
        }
        Debug.Log("Hello, checking for S and Z using a trie with " + pattern);
        string str = component.Trie.Result.PatternMatches(pattern, true);
        if (str == null)
        {
            __result = false;
            return false;
        }
        ___latestWord = str;
        if (appendRE)
            ___latestWord = "RE" + str;
        if (changeSubmitWord)
        {
            Debug.Log("Changing Current Word in Submit to " + ___latestWord);
            SubmitWord.Instance.ReplaceCurrentWord(___latestWord);
        }
        __result = true;
        return false;
    }
    
    [HarmonyPatch(typeof(WordChecker), "SandZMatchSimple")]
    [HarmonyPrefix]
    static bool SandZMatchSimplePrefix(string pattern, bool changeSubmitWord, bool appendRE, WordChecker __instance, ref bool __result, ref string ___latestWord)
    {
        var component = __instance.gameObject.GetComponent<TrieStorageDummyBehaviour>();
        if (component == null)
        {
            Plugin.Logger.LogError("Failed to fetch constructed trie!");
            return true;
        }
        Debug.Log("Hello, checking for S and Z using a trie with " + pattern);
        string str = component.Trie.Result.PatternMatches(pattern, true);
        if (str == null)
        {
            __result = false;
            return false;
        }
        ___latestWord = str;
        if (appendRE)
            ___latestWord = "RE" + str;
        if (changeSubmitWord)
        {
            Debug.Log("Changing Current Word in Submit to " + ___latestWord);
            SubmitWord.Instance.ReplaceCurrentWord(___latestWord);
        }
        __result = true;
        return false;
    }
}