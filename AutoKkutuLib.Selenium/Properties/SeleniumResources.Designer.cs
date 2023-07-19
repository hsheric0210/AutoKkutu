﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AutoKkutuLib.Selenium.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SeleniumResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SeleniumResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("AutoKkutuLib.Selenium.Properties.SeleniumResources", typeof(SeleniumResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /* communicatorJs : AutoKkutu - Selenium communicator
        /// * 
        /// * Reserved names:
        /// * ___wsAddr___
        /// * ___originalWS___
        /// * ___wsGlobal___
        /// * ___wsBuffer___
        /// * ___commSend___
        /// * ___commRecv___
        /// */
        ///
        ///(function () {
        ///    window.___wsGlobal___ = new ___originalWS___(&apos;___wsAddr___&apos;);
        ///    window.___wsBuffer___ = []
        ///    let open = false
        ///    ___wsGlobal___.onopen = function () {
        ///        console.log(&apos;WebSocket connected.&apos;);
        ///        ___wsBuffer___.forEach(msg =&gt; ___wsGlobal___.send(msg));
        ///        ___wsBuffer [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string communicatorJs {
            get {
                return ResourceManager.GetString("communicatorJs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
        ///&lt;Selenium xmlns:xsi=&quot;http://www.w3.org/2001/XMLSchema-instance&quot; xmlns:xsd=&quot;http://www.w3.org/2001/XMLSchema&quot;&gt;
        ///    &lt;!-- 기본적으로 모든 스크립트를 주입할 JavaScript 네임스페이스를 선택합니다. --&gt;
        ///    &lt;JavaScriptInjectionBaseNamespace&gt;window&lt;/JavaScriptInjectionBaseNamespace&gt;
        ///
        ///    &lt;!-- 프로그램을 시작할 때 로드할 사이트 주소를 지정합니다. --&gt;
        ///    &lt;MainPage&gt;https://kkutu.pink/&lt;/MainPage&gt;
        ///
        ///    &lt;!-- 크롬 바이너리(chrome.exe)가 위치한 경로를 지정합니다. 빈 문자열일 경우 기본값이 사용됩니다. --&gt;
        ///    &lt;BinaryLocation&gt;&lt;/BinaryLocation&gt;
        ///
        ///    &lt;!-- 프로그 [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string Selenium {
            get {
                return ResourceManager.GetString("Selenium", resourceCulture);
            }
        }
    }
}
