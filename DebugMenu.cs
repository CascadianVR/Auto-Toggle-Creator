#if UNITY_EDITOR
using System;
using System.Collections.Generic;


using UnityEditor;

namespace CasTools.VRC_Auto_Toggle_Creator
{
    public class DebugMenu
    {
        public bool CheckErrors()
        {
            bool noError = true;
            noError = CheckToggleNames();
            noError = CheckMenuAssignment();
            return noError;
        }
        
        private bool CheckToggleNames()
        {
            string[] names = new string[AutoToggleCreator.Toggles.Count];

            for (int i = 0; i < AutoToggleCreator.Toggles.Count; i++)
            {
                names[i] = AutoToggleCreator.Toggles[i].toggleName;
            }

            foreach (var n in names)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (Array.IndexOf(names, n) == i || n != names[i]) continue;
                    EditorGUILayout.HelpBox("Error: Multiple toggle groups share the same name. Please rename to resolve.", MessageType.Error);
                    return false;
                }
            }

            return true;
        }

        private bool CheckMenuAssignment()
        {
            foreach (var t in AutoToggleCreator.Toggles)
            {
                if (t.expressionMenu != null) continue;
                EditorGUILayout.HelpBox("Error: Missing Expression Menu for \"" + t.toggleName + "\"", MessageType.Error);
                return false;
            }

            return true;
        }
        
    }
}
#endif