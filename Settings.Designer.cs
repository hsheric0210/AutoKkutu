//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AutoKkutu {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.2.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool AutoEnterEnabled {
            get {
                return ((bool)(this["AutoEnterEnabled"]));
            }
            set {
                this["AutoEnterEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool AutoDBUpdateEnabled {
            get {
                return ((bool)(this["AutoDBUpdateEnabled"]));
            }
            set {
                this["AutoDBUpdateEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("OnGameEnd")]
        public global::AutoKkutu.Constants.AutoDBUpdateMode AutoDBUpdateMode {
            get {
                return ((global::AutoKkutu.Constants.AutoDBUpdateMode)(this["AutoDBUpdateMode"]));
            }
            set {
                this["AutoDBUpdateMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("5;6;4;1;2;0")]
        public global::AutoKkutu.Constants.WordPreference ActiveWordPreference {
            get {
                return ((global::AutoKkutu.Constants.WordPreference)(this["ActiveWordPreference"]));
            }
            set {
                this["ActiveWordPreference"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::AutoKkutu.Constants.WordPreference InactiveWordPreference {
            get {
                return ((global::AutoKkutu.Constants.WordPreference)(this["InactiveWordPreference"]));
            }
            set {
                this["InactiveWordPreference"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool EndWordEnabled {
            get {
                return ((bool)(this["EndWordEnabled"]));
            }
            set {
                this["EndWordEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool AttackWordEnabled {
            get {
                return ((bool)(this["AttackWordEnabled"]));
            }
            set {
                this["AttackWordEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool ReturnModeEnabled {
            get {
                return ((bool)(this["ReturnModeEnabled"]));
            }
            set {
                this["ReturnModeEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool AutoFixEnabled {
            get {
                return ((bool)(this["AutoFixEnabled"]));
            }
            set {
                this["AutoFixEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool MissionAutoDetectionEnabled {
            get {
                return ((bool)(this["MissionAutoDetectionEnabled"]));
            }
            set {
                this["MissionAutoDetectionEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool DelayEnabled {
            get {
                return ((bool)(this["DelayEnabled"]));
            }
            set {
                this["DelayEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool DelayPerCharEnabled {
            get {
                return ((bool)(this["DelayPerCharEnabled"]));
            }
            set {
                this["DelayPerCharEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int DelayInMillis {
            get {
                return ((int)(this["DelayInMillis"]));
            }
            set {
                this["DelayInMillis"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool DelayStartAfterWordEnterEnabled {
            get {
                return ((bool)(this["DelayStartAfterWordEnterEnabled"]));
            }
            set {
                this["DelayStartAfterWordEnterEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool GameModeAutoDetectionEnabled {
            get {
                return ((bool)(this["GameModeAutoDetectionEnabled"]));
            }
            set {
                this["GameModeAutoDetectionEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("20")]
        public int MaxDisplayedWordCount {
            get {
                return ((int)(this["MaxDisplayedWordCount"]));
            }
            set {
                this["MaxDisplayedWordCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool FixDelayEnabled {
            get {
                return ((bool)(this["FixDelayEnabled"]));
            }
            set {
                this["FixDelayEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FixDelayPerCharEnabled {
            get {
                return ((bool)(this["FixDelayPerCharEnabled"]));
            }
            set {
                this["FixDelayPerCharEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int FixDelayInMillis {
            get {
                return ((int)(this["FixDelayInMillis"]));
            }
            set {
                this["FixDelayInMillis"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("255, 17, 0")]
        public global::System.Drawing.Color EndWordColor {
            get {
                return ((global::System.Drawing.Color)(this["EndWordColor"]));
            }
            set {
                this["EndWordColor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("255, 128, 0")]
        public global::System.Drawing.Color AttackWordColor {
            get {
                return ((global::System.Drawing.Color)(this["AttackWordColor"]));
            }
            set {
                this["AttackWordColor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("64, 255, 64")]
        public global::System.Drawing.Color MissionWordColor {
            get {
                return ((global::System.Drawing.Color)(this["MissionWordColor"]));
            }
            set {
                this["MissionWordColor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("32, 192, 168")]
        public global::System.Drawing.Color EndMissionWordColor {
            get {
                return ((global::System.Drawing.Color)(this["EndMissionWordColor"]));
            }
            set {
                this["EndMissionWordColor"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("255, 255, 64")]
        public global::System.Drawing.Color AttackMissionWordColor {
            get {
                return ((global::System.Drawing.Color)(this["AttackMissionWordColor"]));
            }
            set {
                this["AttackMissionWordColor"] = value;
            }
        }
    }
}
