using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace UnityPostEffecs { 
    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonDrawer : PropertyDrawer{
        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label){
            var buttonAttr = attribute as ButtonAttribute;
            if(!GUI.Button(pos, buttonAttr.text)){ return; }

            var bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var obj = prop.serializedObject.targetObject;
            var type = obj.GetType();
            var method = type.GetMethod(buttonAttr.name, bindingAttr);

            method.Invoke(obj, buttonAttr.parameters);
        }
    }

    [CustomPropertyDrawer(typeof(SBRLayerAttribute))]
    public class SBRLayerDrawer : PropertyDrawer{
        private const int ROW_MAX = 7;
        private const int MARGIN1 = 75;
        private const int MARGIN2 = 61;
        private const int HEADING = 145;

        private Rect rect;
        private Vector2Int padding;
        private int rowHeight;
        private SerializedProperty property;

        public override void OnGUI(Rect pos, SerializedProperty prop, GUIContent label){
            EditorGUIUtility.labelWidth = MARGIN1;
            label.text = label.text.Replace("Element", "Layer");
            label = EditorGUI.BeginProperty(pos, label, prop);

            rect = EditorGUI.PrefixLabel(pos, label);
            rowHeight = Mathf.FloorToInt(rect.height / ROW_MAX);
            property = prop;

            var indentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float labelWidth0 = 100;
            float labelWidth1 = 55;
            float colWidth0 = HEADING;
            float colWidth1 = Mathf.FloorToInt((rect.width + MARGIN2 - HEADING) / 3.0f);

            padding.Set(0, 0); 
            SetField("enable", "", labelWidth0, colWidth0 * 0.58f);
            SetField("maskType", "", labelWidth1, colWidth1);
            SetField("memo", "[Memo]", labelWidth1, colWidth1 * 2.0f);

            padding.Set(-MARGIN2, padding.y + rowHeight); 
            SetField("gridCount", "[Grid Count]", labelWidth0, colWidth0);
            SetField("detailThresholdLow", "[Detail] Low", labelWidth1 * 2.75f, colWidth1 * 2.0f);
            SetField("detailThresholdHigh", "High", labelWidth1, colWidth1);

            padding.Set(-MARGIN2, padding.y + rowHeight); 
            SetField("strokeWidth", "[Stroke] Width", labelWidth0, colWidth0);
            SetField("strokeLen", "Length", labelWidth1, colWidth1);
            SetField("strokeOpacity", "Opacity", labelWidth1, colWidth1);
            SetField("strokeLenRand", "Rand", labelWidth1, colWidth1);

            padding.Set(-MARGIN2, padding.y + rowHeight); 
            SetField("scratchWidth", "[Scratch] Width", labelWidth0, colWidth0);
            SetField("scratchHeight", "Height", labelWidth1, colWidth1);
            SetField("scratchOpacity", "Opacity", labelWidth1, colWidth1);

            padding.Set(-MARGIN2, padding.y + rowHeight); 
            SetField("toleranceH1", "[Tolerance] H1", labelWidth0, colWidth0);
            SetField("toleranceH2", "H2", labelWidth1, colWidth1);
            SetField("toleranceS", "S", labelWidth1, colWidth1);
            SetField("toleranceV", "V", labelWidth1, colWidth1);
        
            padding.Set(-MARGIN2, padding.y + rowHeight); 
            SetField("addH1", "[Add] H1", labelWidth0, colWidth0);
            SetField("addH2", "H2", labelWidth1, colWidth1);
            SetField("addS", "S", labelWidth1, colWidth1);
            SetField("addV", "V", labelWidth1, colWidth1);
            padding.Set(-MARGIN2, padding.y + rowHeight); 
            SetField("mulH1", "[Mul] H1", labelWidth0, colWidth0);
            SetField("mulH2", "H2", labelWidth1, colWidth1);
            SetField("mulS", "S", labelWidth1, colWidth1);
            SetField("mulV", "V", labelWidth1, colWidth1);

            EditorGUI.indentLevel = indentLevel;
            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty prop, GUIContent label){
            return ROW_MAX * EditorGUIUtility.singleLineHeight + 3 * EditorGUIUtility.standardVerticalSpacing;
        }

        void SetField(string name, string label, float labelWidth, float colWidth){
            var prop = property.FindPropertyRelative(name);
            var pos = new Rect(rect.x + padding.x, rect.y + padding.y, colWidth, rowHeight);
            padding.x += Mathf.FloorToInt(colWidth);

            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUI.PropertyField(pos, prop, new GUIContent(label));
        }
    }
}