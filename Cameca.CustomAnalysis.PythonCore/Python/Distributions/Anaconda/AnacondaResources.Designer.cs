﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Cameca.CustomAnalysis.PythonCore {
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
    public class AnacondaResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal AnacondaResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Cameca.CustomAnalysis.PythonCore.Python.Distributions.Anaconda.AnacondaResources", typeof(AnacondaResources).Assembly);
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
        ///   Looks up a localized string similar to https://www.anaconda.com/.
        /// </summary>
        public static string AnacondaDownloadUrl {
            get {
                return ResourceManager.GetString("AnacondaDownloadUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No Anaconda Python interpreter could be located..
        /// </summary>
        public static string AnacondaNotFoundDialogInfoHeader {
            get {
                return ResourceManager.GetString("AnacondaNotFoundDialogInfoHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Please download and install Anaconda to continue..
        /// </summary>
        public static string AnacondaNotFoundDialogInfoLine1 {
            get {
                return ResourceManager.GetString("AnacondaNotFoundDialogInfoLine1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Download link will open in your default browser..
        /// </summary>
        public static string AnacondaNotFoundDialogInfoLine2 {
            get {
                return ResourceManager.GetString("AnacondaNotFoundDialogInfoLine2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Close.
        /// </summary>
        public static string CloseButtonLabel {
            get {
                return ResourceManager.GetString("CloseButtonLabel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Download.
        /// </summary>
        public static string DownloadButtonLabel {
            get {
                return ResourceManager.GetString("DownloadButtonLabel", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exception resolving Anaconda install path from Registry.
        /// </summary>
        public static string LogWarningGeneralCheckRegistryException {
            get {
                return ResourceManager.GetString("LogWarningGeneralCheckRegistryException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Exception initializing Anaconda Python distribution.
        /// </summary>
        public static string LogWarningGeneralInitializeException {
            get {
                return ResourceManager.GetString("LogWarningGeneralInitializeException", resourceCulture);
            }
        }
    }
}