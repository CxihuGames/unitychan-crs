using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ParserState {
    None,
    HumanTemplate,
    BoneTemplate,
}

public class HTFile {
    public string ObjectHideFlags { get; set; }
    public string CorrespondingSourceObject { get; set; }
    public string PrefabInstance { get; set; }
    public string PrefabAsset { get; set; }
    public string Name { get; set; }

    public Dictionary<string, string> Bones;
}

public class HTFileLoader {

    public static int GetLineLeadingSpaceCount(string line) {
        int count = 0;
        foreach (char c in line) {
            if (c == ' ') {
                count++;
            } else {
                break;
            }
        }
        return count;
    }
    public static HTFile Load(string path) {
        string txt = File.ReadAllText(path);
        string[] lines = txt.Split('\n');
        HTFile file = new HTFile();
        file.Bones = new Dictionary<string, string>();

        var stateStack = new Stack<ParserState>();
        stateStack.Push(ParserState.None);

        int currentLeadingSpaceCount = 0;
        foreach (string line in lines) {
            if (string.IsNullOrWhiteSpace(line)) {
                continue; // Skip empty lines
            }

            int leadingSpaceCount = GetLineLeadingSpaceCount(line);
            if (currentLeadingSpaceCount > leadingSpaceCount) {
                stateStack.Pop();
                currentLeadingSpaceCount = leadingSpaceCount;
            }

            string[] parts = line.Split(':');
            if (parts.Length == 2) {
                string key = parts[0].Trim();
                string value = parts[1].Trim();

                ParseLine(key, value, file, stateStack);
            }
        }
        return file;
    }

    public static void ParseNoneState(string key, string value, HTFile file, Stack<ParserState> stateStack) {
        if (key == "HumanTemplate") {
            stateStack.Push(ParserState.HumanTemplate);
        }
    }

    public static void ParseHumanTemplateState(string key, string value, HTFile file, Stack<ParserState> stateStack) {
        switch (key) {
            case "m_ObjectHideFlags":
                file.ObjectHideFlags = value;
                break;
            case "m_CorrespondingSourceObject":
                file.CorrespondingSourceObject = value;
                break;
            case "m_PrefabInstance":
                file.PrefabInstance = value;
                break;
            case "m_PrefabAsset":
                file.PrefabAsset = value;
                break;
            case "m_Name":
                file.Name = value;
                break;
            case "m_BoneTemplate":
                stateStack.Push(ParserState.BoneTemplate);
                break;
        }
    }

    public static void ParseBoneTemplateState(string key, string value, HTFile file, Stack<ParserState> stateStack) {
        file.Bones.Add(key, value);
    }

    public static void ParseLine(string key, string value, HTFile file, Stack<ParserState> stateStack) {
        var state = stateStack.First();
        // Debug.Log($"key: {key} value: {value} state: {state}");
        if (state == ParserState.None) {
            ParseNoneState(key, value, file, stateStack);
        } else if (state == ParserState.HumanTemplate) {
            ParseHumanTemplateState(key, value, file, stateStack);
        } else if (state == ParserState.BoneTemplate) {
            ParseBoneTemplateState(key, value, file, stateStack);
        }
    }
}