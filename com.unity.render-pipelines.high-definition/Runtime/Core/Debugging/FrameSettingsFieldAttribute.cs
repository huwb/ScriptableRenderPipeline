using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public static partial class StringExtention
    {
        public static string CamelToPascalCaseWithSpace(this string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(char.ToUpper(text[0]));
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]))
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                            i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }
    }

    /// <summary>Should only be used on enum value of field to describe aspect in DebugMenu</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class FrameSettingsFieldAttribute : Attribute
    {
        public enum DisplayType { BoolAsCheckbox, BoolAsEnumPopup, Others }
        public readonly DisplayType type;
        public readonly string displayedName;
        public readonly int group;
        public readonly Type targetType;

        public FrameSettingsFieldAttribute(int group, string displayedName = null, DisplayType type = DisplayType.BoolAsCheckbox, Type targetType = null, string targetPropertyName = null, [CallerMemberName] string autoDisplayedNameWithPropertyName = null)
        {
            if (string.IsNullOrEmpty(displayedName))
                displayedName = autoDisplayedNameWithPropertyName.CamelToPascalCaseWithSpace();
            this.group = group;
            this.displayedName = displayedName;
            this.type = type;
            this.targetType = targetType;
        }
    }
}
