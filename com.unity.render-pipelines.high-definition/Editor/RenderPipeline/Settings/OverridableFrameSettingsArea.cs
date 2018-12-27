using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Reflection;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    internal struct OverridableFrameSettingsArea
    {
        static readonly GUIContent overrideTooltip = EditorGUIUtility.TrTextContent("", "Override this setting in component.");
        static readonly Dictionary<FrameSettingsField, FrameSettingsFieldAttribute> attributes;

        FrameSettings defaultFrameSettings;
        SerializedFrameSettings serializedFrameSettings;

        static OverridableFrameSettingsArea()
        {
            attributes = new Dictionary<FrameSettingsField, FrameSettingsFieldAttribute>();
            Type type = typeof(FrameSettingsField);
            foreach (FrameSettingsField value in Enum.GetValues(type))
            {
                attributes[value] = type.GetField(Enum.GetName(type, value)).GetCustomAttribute<FrameSettingsFieldAttribute>();
            }
        }

        private struct Field
        {
            public FrameSettingsField field;
            public Func<bool> overrideable;
            public Func<object> customGetter;
            public Action<object> customSetter;
            public object overridedDefaultValue;
            public int indent;
            public bool overrideEnabled => overrideable == null || overrideable();
            public GUIContent label => EditorGUIUtility.TrTextContent(attributes[field].displayedName);
        }
        private List<Field> fields;

        public OverridableFrameSettingsArea(int capacity, FrameSettings defaultFrameSettings, SerializedFrameSettings serializedFrameSettings)
        {
            fields = new List<Field>(capacity);
            this.defaultFrameSettings = defaultFrameSettings;
            this.serializedFrameSettings = serializedFrameSettings;
        }

        /// <summary>Add an overrideable field to be draw when Draw(bool) will be called.</summary>
        /// <param name="serializedFrameSettings">The overrideable property to draw in inspector</param>
        /// <param name="field">The field drawn</param>
        /// <param name="overrideable">The enabler will be used to check if this field could be overrided. If null or have a return value at true, it will be overrided.</param>
        /// <param name="overridedDefaultValue">The value to display when the property is not overrided. If null, use the actual value of it.</param>
        /// <param name="indent">Add this value number of indent when drawing this field.</param>
        public void Add(FrameSettingsField field, Func<bool> overrideable = null, Func<object> customGetter = null, Action<object> customSetter = null, object overridedDefaultValue = null, int indent = 0)
            => fields.Add(new Field { overrideable = overrideable, overridedDefaultValue = overridedDefaultValue, indent = indent, customGetter = customGetter, customSetter = customSetter });

        public void Draw(bool withOverride)
        {
            if (fields == null)
                throw new ArgumentOutOfRangeException("Cannot be used without using the constructor with a capacity initializer.");
            if (withOverride & GUI.enabled)
                OverridesHeaders();
            for (int i = 0; i< fields.Count; ++i)
                DrawField(fields[i], withOverride);
        }

        void DrawField(Field field, bool withOverride)
        {
            if (field.indent == 0)
                --EditorGUI.indentLevel;    //alignment provided by the space for override checkbox
            else
            {
                for (int i = field.indent - 1; i > 0; --i)
                    ++EditorGUI.indentLevel;
            }
            bool enabled = field.overrideEnabled;
            withOverride &= enabled & GUI.enabled;
            bool shouldBeDisabled = withOverride || !enabled || !GUI.enabled;
            using (new EditorGUILayout.HorizontalScope())
            {
                var overrideRect = GUILayoutUtility.GetRect(15f, 17f, GUILayout.ExpandWidth(false)); //15 = kIndentPerLevel
                if (withOverride)
                {
                    bool originalValue = serializedFrameSettings.rootOveride.GetBitArrayAt((uint)field.field);
                    overrideRect.yMin += 4f;
                    bool modifiedValue = GUI.Toggle(overrideRect, originalValue, overrideTooltip, CoreEditorStyles.smallTickbox);

                    if (originalValue ^ modifiedValue)
                        serializedFrameSettings.rootOveride.SetBitArrayAt((uint)field.field, modifiedValue);

                    shouldBeDisabled = !modifiedValue;
                }
                using (new EditorGUI.DisabledScope(shouldBeDisabled))
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        //the following block will display a default value if provided instead of actual value (case if(true))
                        if (shouldBeDisabled && field.overridedDefaultValue != null)
                        {
                            if (field.overridedDefaultValue == null)
                                EditorGUILayout.Toggle(field.label, defaultFrameSettings.IsEnable(field.field));
                            else
                                DrawFieldShape(field.label, field.overridedDefaultValue);
                        }
                        else
                        {
                            switch (attributes[field.field].type)
                            {
                                case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                                    bool oldBool = serializedFrameSettings.rootData.GetBitArrayAt((uint)field.field);
                                    bool newBool = (bool)DrawFieldShape(field.label, oldBool);
                                    if (oldBool ^ newBool)
                                    {
                                        Undo.RecordObject(serializedFrameSettings.rootData.serializedObject.targetObject, "Changed FrameSettings " + field.field);
                                        serializedFrameSettings.rootData.SetBitArrayAt((uint)field.field, newBool);
                                    }
                                    break;
                                case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                                    var oldEnum = Convert.ChangeType(serializedFrameSettings.rootData.GetBitArrayAt((uint)field.field) ? 1 : 0, attributes[field.field].targetType);
                                    var newEnum = (Enum)DrawFieldShape(field.label, oldEnum);
                                    if (oldEnum != newEnum)
                                    {
                                        Undo.RecordObject(serializedFrameSettings.rootData.serializedObject.targetObject, "Changed FrameSettings " + field.field);
                                        serializedFrameSettings.rootData.SetBitArrayAt((uint)field.field, Convert.ToInt32(newEnum) == 1);
                                    }
                                    break;
                                case FrameSettingsFieldAttribute.DisplayType.Others:
                                    var oldValue = field.customGetter();
                                    var newValue = DrawFieldShape(field.label, oldValue);
                                    if (oldValue != newValue)
                                    {
                                        Undo.RecordObject(serializedFrameSettings.rootData.serializedObject.targetObject, "Changed FrameSettings " + field.field);
                                        field.customSetter(newValue);
                                    }
                                    break;
                                default:
                                    throw new ArgumentException("Unknown FrameSettingsFieldAttribute");
                            }
                            
                        }
                    }
                }
            }
            if (field.indent == 0)
            {
                ++EditorGUI.indentLevel;
            }
            else
            {
                for (int i = field.indent - 1; i > 0; --i)
                {
                    --EditorGUI.indentLevel;
                }
            }
        } 

        object DrawFieldShape(GUIContent label, object field)
        {
            if (field is GUIContent)
            {
                EditorGUILayout.LabelField(label, (GUIContent)field);
                return null;
            }
            else if (field is string)
                return EditorGUILayout.TextField(label, (string)field);
            else if (field is bool)
                return EditorGUILayout.Toggle(label, (bool)field);
            else if (field is int)
                return EditorGUILayout.IntField(label, (int)field);
            else if (field is float)
                return EditorGUILayout.FloatField(label, (float)field);
            else if (field is Color)
                return EditorGUILayout.ColorField(label, (Color)field);
            else if (field is Enum)
                return EditorGUILayout.EnumPopup(label, (Enum)field);
            else if (field is LayerMask)
                return EditorGUILayout.MaskField(label, (LayerMask)field, GraphicsSettings.renderPipelineAsset.renderingLayerMaskNames);
            else if (field is UnityEngine.Object)
                return EditorGUILayout.ObjectField(label, (UnityEngine.Object)field, field.GetType(), true);
            else if (field is SerializedProperty)
                return EditorGUILayout.PropertyField((SerializedProperty)field, label, includeChildren: true);
            else
            {
                EditorGUILayout.LabelField(label, new GUIContent("Unsupported type"));
                Debug.LogError("Unsupported format " + field.GetType() + " in OverridableSettingsArea.cs. Please add it!");
                return null;
            }
        }

        void OverridesHeaders()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayoutUtility.GetRect(0f, 17f, GUILayout.ExpandWidth(false));
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("All", "Toggle all overrides on. To maximize performances you should only toggle overrides that you actually need."), CoreEditorStyles.miniLabelButton, GUILayout.Width(17f), GUILayout.ExpandWidth(false)))
                {
                    foreach (var field in fields)
                    {
                        if (field.overrideEnabled)
                            serializedFrameSettings.rootOveride.SetBitArrayAt((uint)field.field, true);
                    }
                }

                if (GUILayout.Button(EditorGUIUtility.TrTextContent("None", "Toggle all overrides off."), CoreEditorStyles.miniLabelButton, GUILayout.Width(32f), GUILayout.ExpandWidth(false)))
                {
                    foreach (var field in fields)
                    {
                        if (field.overrideEnabled)
                            serializedFrameSettings.rootOveride.SetBitArrayAt((uint)field.field, false);
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
