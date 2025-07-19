using System;
using System.Collections.Generic;
using System.Linq;

namespace Wildcards;

public readonly struct LengthwisePrefixTrie
{
    private readonly Dictionary<int, PrefixTrie> _tries = new();
    
    public LengthwisePrefixTrie(string[] words)
    {
        foreach (var i in words.Select(x => x.Length).ToHashSet())
        {
            _tries[i] = new PrefixTrie(words.Where(x => x.Length == i).ToArray());
        }
    }
    
    public string PatternMatches(string pattern, bool sandz)
    {
        if (!_tries.ContainsKey(pattern.Length))
        {
            return null;
        }
        return _tries[pattern.Length].PatternMatches(pattern, sandz);
    }
    
    public override string ToString()
    {
        var children = string.Join(", ", _tries.Select((len, trie) => $"{len}: {trie}").ToArray());
        return $"[{children}]";
    }
}

readonly struct PrefixTrie
{
    private readonly TrieNode[] _rootNodes;

    public PrefixTrie(string[] words)
    {
        Array.Sort(words);
        _rootNodes = BuildTrie(0, new ArraySegment<string>(words))!;
    }

    public string PatternMatches(string pattern, bool sandz)
    {
        return WildcardMatches(_rootNodes, pattern, sandz);
    }

    private static string WildcardMatches(TrieNode[] nodes, string pattern, bool sandz)
    {
        if (nodes == null)
        {
            return pattern == "" ? "" : null;
        }

        if (pattern[0] == '*')
        {
            foreach (var node in nodes)
            {
                var match = MatchNode(node, pattern, sandz);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        if (sandz && (pattern[0] == 'S' || pattern[0] == 'Z'))
        {
            var nodeIdxS = BinarySearchByFirstChar(nodes, 'S');
            if (nodeIdxS != -1)
            {
                var node = nodes[nodeIdxS];
                var match = MatchNode(node, pattern, sandz);
                if (match != null)
                {
                    return match;
                }
            }

            var nodeIdxZ = BinarySearchByFirstChar(nodes, 'Z');
            if (nodeIdxZ != -1)
            {
                var node = nodes[nodeIdxZ];
                var match = MatchNode(node, pattern, sandz);
                if (match != null)
                {
                    return match;
                }
            }
        }
        else
        {
            var nodeIdx = BinarySearchByFirstChar(nodes, pattern[0]);
            if (nodeIdx != -1)
            {
                var node = nodes[nodeIdx];
                var match = MatchNode(node, pattern, sandz);
                if (match != null)
                {
                    return match;
                }
            }
        }

        return null;
    }

    private static string MatchNode(TrieNode node, string pattern, bool sandz)
    {
        if (node.Prefix.Length <= pattern.Length && Matches(node.Prefix, pattern, sandz))
        {
            var suffix = WildcardMatches(node.Children, pattern.Substring(node.Prefix.Length), sandz);
            if (suffix == null)
            {
                return null;
            }

            return node.Prefix + suffix;
        }

        return null;
    }

    private static int BinarySearchByFirstChar(TrieNode[] nodes, char firstLetter)
    {
        int min = 0;
        int max = nodes.Length - 1;
        while (min <= max)
        {
            int mid = ((max - min) / 2) + min;
            int cmp = -1;
            if (nodes[mid].Prefix != "")
            {
                cmp = nodes[mid].Prefix[0] - firstLetter;
            }

            if (cmp == 0) return mid;
            if (cmp < 0)
            {
                min = mid + 1;
            }
            else
            {
                max = mid - 1;
            }
        }

        return -1;
    }

    public static bool Matches(string word, string pattern, bool sandz)
    {
        bool complex = pattern.IndexOf('*') < word.Length;
        if (sandz)
        {
            complex |= pattern.IndexOfAny(new[] { 'S', 'Z' }) < word.Length;
        }

        if (!complex)
        {
            return pattern.StartsWith(word);
        }

        for (var i = 0; i < word.Length; i++)
        {
            var first = word[i];
            var second = pattern[i];

            if (second == '*')
            {
                continue;
            }

            if (sandz && ((first == 'S' && second == 'Z') || (first == 'Z' && second == 'S')))
            {
                continue;
            }

            if (first != second)
            {
                return false;
            }
        }

        return true;
    }

    private static TrieNode[] BuildTrie(int offset, ArraySegment<string> words)
    {
        if (words.Count == 1)
        {
            return null;
        }

        string longestCommonPrefix = "";
        int lastPrefixStart = 0;
        List<TrieNode> nodes = new List<TrieNode>();
        for (var i = 0; i < words.Count; i++)
        {
            string word = words[i];
            string subword = word.Substring(offset);
            for (int j = Math.Min(subword.Length, longestCommonPrefix.Length); j >= 0; j--)
            {
                if (subword.StartsWith(longestCommonPrefix.Substring(0, j)))
                {
                    if (j == 0)
                    {
                        if (i > 0)
                        {
                            var subwords = new ArraySegment<string>(words.Array, words.Offset + lastPrefixStart,
                                i - lastPrefixStart);
                            nodes.Add(new TrieNode(longestCommonPrefix,
                                BuildTrie(offset + longestCommonPrefix.Length, subwords)));
                        }

                        longestCommonPrefix = subword;
                        lastPrefixStart = i;
                    }
                    else
                    {
                        longestCommonPrefix = longestCommonPrefix.Substring(0, j);
                    }

                    break;
                }
            }
        }

        {
            var subwords = new ArraySegment<string>(words.Array, words.Offset + lastPrefixStart,
                words.Count - lastPrefixStart);
            nodes.Add(new TrieNode(longestCommonPrefix, BuildTrie(offset + longestCommonPrefix.Length, subwords)));
        }
        return nodes.ToArray();
    }

    public override string ToString()
    {
        var children = string.Join(" ", _rootNodes.Select(x => x.ToString()).ToArray());
        return $"[{children}]";
    }
}

readonly struct TrieNode
{
    public TrieNode(string prefix, TrieNode[] children)
    {
        Prefix = prefix;
        Children = children;
    }
    
    public readonly string Prefix;
    public readonly TrieNode[] Children;
    public override string ToString()
    {
        if (Children == null)
        {
            return Prefix;
        }
        var children = string.Join(" ", Children.Select(x => x.ToString()).ToArray());
        return $"{Prefix}[{children}]";
    }
}