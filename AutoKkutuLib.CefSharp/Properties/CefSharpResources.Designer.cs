﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AutoKkutuLib.CefSharp.Properties {
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
    public class CefSharpResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal CefSharpResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("AutoKkutuLib.CefSharp.Properties.CefSharpResources", typeof(CefSharpResources).Assembly);
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &lt;?xml version=&quot;1.0&quot; encoding=&quot;utf-8&quot; ?&gt;
        ///&lt;CefSharp xmlns:xsi=&quot;http://www.w3.org/2001/XMLSchema-instance&quot; xmlns:xsd=&quot;http://www.w3.org/2001/XMLSchema&quot;&gt;
        ///    &lt;!-- 기본적으로 모든 스크립트를 주입할 JavaScript 네임스페이스를 선택합니다. --&gt;
        ///    &lt;JavaScriptInjectionBaseNamespace&gt;window&lt;/JavaScriptInjectionBaseNamespace&gt;
        ///
        ///    &lt;!-- 프로그램을 시작할 때 로드할 사이트 주소를 지정합니다. --&gt;
        ///    &lt;MainPage&gt;https://kkutu.pink/&lt;/MainPage&gt;
        ///
        ///    &lt;!-- 리소스 파일들(cef.pak, devtools_resources.pak)이 위치한 폴더를 지정합니다. 빈 문자열로 둘 시 기본값이 사용됩니다. --&gt;
        ///    &lt;!-- https://cefsharp.githu [rest of string was truncated]&quot;;.
        /// </summary>
        public static string CefSharp {
            get {
                return ResourceManager.GetString("CefSharp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to /* communicatorJs : AutoKkutu - CefSharp JSB communicator
        /// * 
        /// * Reserved names:
        /// * ___jsbGlobal___
        /// * ___jsbObj___
        /// * onSend
        /// * onReceive
        /// * bindObjectAsync
        /// * BindObjectAsync
        /// * ___commSend___
        /// * ___commRecv___
        /// */
        ///
        ///___commSend___ = (async function (msg) {
        ///    let fn = window[&apos;___jsbGlobal___&apos;][&apos;bindObjectAsync&apos;] ?? window[&apos;___jsbGlobal___&apos;][&apos;BindObjectAsync&apos;];
        ///    await fn(&apos;___jsbObj___&apos;);
        ///    window[&apos;___jsbObj___&apos;][&apos;onSend&apos;](msg)
        ///});
        ///
        ///___commRecv___ = (async function (msg) {
        ///    let  [rest of string was truncated]&quot;;.
        /// </summary>
        public static string communicatorJs {
            get {
                return ResourceManager.GetString("communicatorJs", resourceCulture);
            }
        }
    }
}
