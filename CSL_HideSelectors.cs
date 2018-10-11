using ColossalFramework.IO;
using ColossalFramework.UI;
using ICities;
using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using UnityEngine;


namespace CSL_HideSelectors
{
    public class CSL_HideSelectors : IUserMod
    {
        public static readonly string m_name = "Hide Selectors";
        public string Name => m_name;
        public string Description => "Toggle hover selectors with Alt+H";
        internal static readonly string settingsFilePath = Path.Combine(DataLocation.localApplicationData, "CSL_HideSelectors.xml");
        public static bool isVisible = true;
        public static float oldAlpha = 0f;

        private static HideSelectorsSettings m_settings;
        public static HideSelectorsSettings Settings
        {
            get
            {
                if (m_settings == null)
                {

                    m_settings = HideSelectorsSettings.LoadConfiguration();

                    if (m_settings == null)
                    {
                        m_settings = new HideSelectorsSettings();
                        m_settings.SaveConfiguration();
                    }
                }
                return m_settings;
            }
        }
        private string[] modes = new string[] { ((TooltipMode)0).ToString(), ((TooltipMode)1).ToString() };
        private UIDropDown dropDown;

        public void OnSettingsUI(UIHelperBase helper)
        {
            helper.AddSpace(20);
            dropDown = (UIDropDown)helper.AddDropdown("Cost Tooltip When Selectors Are Invisible", modes, Settings.TooltipMode, (i) =>
            {
                Settings.TooltipMode = i;
                Settings.SaveConfiguration();
                if (HideSelectors.Loaded) HideSelectors.Toggle();
            });
        }
    }


    public class HideSelectorsSettings
    {
        public int TooltipMode = 0;

        public HideSelectorsSettings() { }

        public void OnPreSerialize() { }

        public void OnPostDeserialize() { }

        public void SaveConfiguration()
        {
            var fileName = CSL_HideSelectors.settingsFilePath;
            var config = CSL_HideSelectors.Settings;
            var serializer = new XmlSerializer(typeof(HideSelectorsSettings));
            using (var writer = new StreamWriter(fileName))
            {
                config.OnPreSerialize();
                serializer.Serialize(writer, config);
            }
        }


        public static HideSelectorsSettings LoadConfiguration()
        {
            var fileName = CSL_HideSelectors.settingsFilePath;
            var serializer = new XmlSerializer(typeof(HideSelectorsSettings));
            try
            {
                using (var reader = new StreamReader(fileName))
                {
                    var config = serializer.Deserialize(reader) as HideSelectorsSettings;
                    return config;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[{CSL_HideSelectors.m_name}]: Error Parsing {fileName}: {ex}");
                return null;
            }
        }
    }

    public class HideSelectorsLoading : LoadingExtensionBase
    {
        public override void OnLevelUnloading()
        {
            base.OnLevelUnloading();
            HideSelectors.Loaded = false;
        }
    }

    public class HideSelectorsThreading : ThreadingExtensionBase
    {
        private bool _processed = false;

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKey(KeyCode.H))
            {
                if (_processed) return;
                _processed = true;

                HideSelectors.Toggle();
            }
            else
            {
                _processed = false;
            }
        }
    }

    public enum TooltipMode
    {
        None = -1,
        Invisible,
        Visible,
        Count
    }

    public class HideSelectors
    {
        public static bool Loaded;

        internal static void Toggle()
        {
            float alpha = 0f;
            var settings = CSL_HideSelectors.Settings;

            ToolController toolController = ToolsModifierControl.toolController;

            GameObject.Find("CursorInfo").GetComponent<UILabel>().opacity = settings.TooltipMode == (int)TooltipMode.Invisible ? 0f : settings.TooltipMode == (int)TooltipMode.Visible ? 1f : alpha;

            if (CSL_HideSelectors.isVisible || (toolController.m_validColor.a != 0f))
            {
                // If isVisible == false but m_validcolor isn't zero, TransparentSelectors' options have been modified so selectors have been re-enabled

                CSL_HideSelectors.oldAlpha = toolController.m_validColor.a;
                CSL_HideSelectors.isVisible = false;
            }
            else
            {
                alpha = CSL_HideSelectors.oldAlpha;
                CSL_HideSelectors.isVisible = true;
            }

            toolController.m_errorColor.a = toolController.m_errorColorInfo.a =
            toolController.m_warningColor.a = toolController.m_warningColorInfo.a =
            toolController.m_validColor.a = toolController.m_validColorInfo.a = alpha;

            try
            {
                Color oldColor; Color newColor; FieldInfo field;

                field = Type.GetType("MoveIt.MoveItTool, MoveIt").GetField("m_hoverColor", BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null) goto Prop_Line_Tool;
                oldColor = (Color)field.GetValue(null);
                newColor = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
                field.SetValue(null, newColor);

                field = Type.GetType("MoveIt.MoveItTool, MoveIt").GetField("m_moveColor", BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null) goto Prop_Line_Tool;
                oldColor = (Color)field.GetValue(null);
                newColor = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
                field.SetValue(null, newColor);

                field = Type.GetType("MoveIt.MoveItTool, MoveIt").GetField("m_selectedColor", BindingFlags.NonPublic | BindingFlags.Static);
                if (field == null) goto Prop_Line_Tool;
                oldColor = (Color)field.GetValue(null);
                newColor = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
                field.SetValue(null, newColor);
            }

            catch (Exception exception)
            {
                Debug.Log($"[{CSL_HideSelectors.m_name}]: {exception}");
            }

        Prop_Line_Tool:
            try
            {
                Type propLineTool = Type.GetType("PropLineTool.PropLineTool, PropLineTool_v1");
                if (propLineTool == null) return;
                FieldInfo field = propLineTool.GetField("m_PLTColor_default", BindingFlags.Public | BindingFlags.Instance);
                object component = toolController.GetComponent(propLineTool);//this time it's an instance field, so we need to get a reference to the instance of PropLineTool.
                Color oldColor = (Color)field.GetValue(component);//then we get the value from the instance
                Color newColor = new Color(oldColor.r, oldColor.g, oldColor.b, alpha);
                field.SetValue(component, newColor);
            }
            catch (Exception exception)
            {
                Debug.Log($"[{CSL_HideSelectors.m_name}]: {exception}");
            }
        }
    }
}
