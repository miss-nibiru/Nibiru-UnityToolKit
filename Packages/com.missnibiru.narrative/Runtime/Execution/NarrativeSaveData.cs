using System;
using System.Collections.Generic;
using UnityEngine;

namespace MissNibiru.Narrative
{
    [Serializable]
    public sealed class NarrativeFlagSaveValue
    {
        public string id = string.Empty;
        public bool value;
    }

    [Serializable]
    public sealed class NarrativeVariableSaveValue
    {
        public string id = string.Empty;
        public NarrativeVariableType type;
        public bool booleanValue;
        public int integerValue;
        public float floatValue;
        public string stringValue = string.Empty;
    }

    [Serializable]
    public sealed class NarrativeSaveData
    {
        public string storyId = string.Empty;
        public string currentNodeId = string.Empty;
        public List<NarrativeFlagSaveValue> flags =
            new List<NarrativeFlagSaveValue>();
        public List<NarrativeVariableSaveValue> variables =
            new List<NarrativeVariableSaveValue>();

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static NarrativeSaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            NarrativeSaveData data =
                JsonUtility.FromJson<NarrativeSaveData>(json);

            if (data == null)
                return null;

            data.flags ??= new List<NarrativeFlagSaveValue>();
            data.variables ??=
                new List<NarrativeVariableSaveValue>();
            return data;
        }
    }
}
