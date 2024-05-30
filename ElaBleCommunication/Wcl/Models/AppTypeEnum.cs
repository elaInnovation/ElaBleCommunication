using System;
using System.Collections.Generic;
using System.Text;

namespace ElaBleCommunication.Wcl.Models
{
    /// <summary>
    /// Defines the different application types supported by the library.
    /// </summary>
    public enum AppTypeEnum
    {
        /// <summary>
        /// For backend applications (asp.net, console, etc.)
        /// </summary>
        Default,
        /// <summary>
        /// For UI applications (WPF, WinForms, etc.)
        /// </summary>
        UI
    }
}
