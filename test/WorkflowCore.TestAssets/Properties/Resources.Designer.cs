﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WorkflowCore.TestAssets.Properties {
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
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WorkflowCore.TestAssets.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to {
        ///  &quot;Id&quot;: &quot;Test&quot;,
        ///  &quot;Version&quot;: 1,
        ///  &quot;Description&quot;: &quot;&quot;,
        ///  &quot;DataType&quot;: &quot;WorkflowCore.TestAssets.DataTypes.CounterBoard, WorkflowCore.TestAssets&quot;,
        ///  &quot;Steps&quot;: [
        ///    {
        ///      &quot;Id&quot;: &quot;Step1&quot;,
        ///      &quot;StepType&quot;: &quot;WorkflowCore.TestAssets.Steps.Counter, WorkflowCore.TestAssets&quot;,
        ///      &quot;ErrorBehavior&quot;: &quot;Retry&quot;,
        ///      &quot;Inputs&quot;: { &quot;Value&quot;: &quot;data.Counter1&quot; },
        ///      &quot;Outputs&quot;: { &quot;Counter1&quot;: &quot;step.Value&quot; },
        ///      &quot;NextStepId&quot;: &quot;Step2&quot;
        ///    },
        ///    {
        ///      &quot;Id&quot;: &quot;Step2&quot;,
        ///      &quot;StepType&quot;: &quot;WorkflowCore.TestAsset [rest of string was truncated]&quot;;.
        /// </summary>
        public static string stored_definition_json {
            get {
                return ResourceManager.GetString("stored_definition_json", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized resource of type System.Byte[].
        /// </summary>
        public static byte[] stored_definition_yaml {
            get {
                object obj = ResourceManager.GetObject("stored_definition_yaml", resourceCulture);
                return ((byte[])(obj));
            }
        }
    }
}
