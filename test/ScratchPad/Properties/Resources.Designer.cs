﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ScratchPad.Properties {
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
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ScratchPad.Properties.Resources", typeof(Resources).Assembly);
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
        ///   Looks up a localized string similar to {
        ///  &quot;Id&quot;: &quot;Test02&quot;,
        ///  &quot;Version&quot;: 1,
        ///  &quot;Description&quot;: &quot;&quot;,
        ///  &quot;DataType&quot;: &quot;ScratchPad.WfData, ScratchPad&quot;,
        ///  &quot;Steps&quot;: [
        ///    {
        ///      &quot;Id&quot;: &quot;Hello&quot;,
        ///      &quot;StepType&quot;: &quot;ScratchPad.HelloWorld, ScratchPad&quot;,
        ///      &quot;NextStepId&quot;: &quot;decide&quot;
        ///    },
        ///    {
        ///      &quot;Id&quot;: &quot;decide&quot;,
        ///      &quot;StepType&quot;: &quot;WorkflowCore.Primitives.Decide, WorkflowCore&quot;,
        ///      &quot;SelectNextStep&quot;:
        ///      {
        ///        &quot;Print1&quot;: &quot;data.Value1 == \&quot;one\&quot;&quot;,
        ///        &quot;Print2&quot;: &quot;data.Value1 == \&quot;two\&quot;&quot;
        ///      }
        ///    },
        ///    {
        ///      &quot;Id&quot;: &quot;Print1&quot;,        /// [rest of string was truncated]&quot;;.
        /// </summary>
        internal static string HelloWorld {
            get {
                return ResourceManager.GetString("HelloWorld", resourceCulture);
            }
        }
    }
}
