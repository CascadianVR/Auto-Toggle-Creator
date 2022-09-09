#if UNITY_EDITOR
using System;
using System.Collections.Generic;


using UnityEditor;

namespace CasTools.VRC_Auto_Toggle_Creator
{
    public class DebugMenu
    {
        public bool CheckToggleNames(List<AutoToggleCreator.ToggleType> toggles)
        {
            string[] names = new string[toggles.Count];

            for (int i = 0; i < toggles.Count; i++)
            {
                names[i] = toggles[i].toggleName;
            }

            foreach (var n in names)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (Array.IndexOf(names, n) != i && n == names[i])
                    {
                        EditorGUILayout.HelpBox("Error: Multiple toggle groups share the same name. Please rename to resolve.", MessageType.Error);
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
#endif