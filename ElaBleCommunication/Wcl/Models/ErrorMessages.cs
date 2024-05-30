using System;
using System.Collections.Generic;
using System.Text;

namespace ElaBleCommunication.Wcl.Models
{
    public static class ErrorMessages
    {
        private static Dictionary<int, string> _messages = new Dictionary<int, string>();

        static ErrorMessages()
        {
            var fields = typeof(wclCommon.wclErrors).GetFields();
            foreach (var field in fields)
            {
                var errorCodeStr = field.GetValue(null).ToString();
                if (int.TryParse(errorCodeStr, out var errorCode) && !_messages.ContainsKey(errorCode))
                {
                    _messages.Add(errorCode, field.Name);
                }
            }
        }

        public static string Get(int errorCode)
        {
            if (!_messages.ContainsKey(errorCode)) return string.Empty;
            return _messages[errorCode];
        }
    }
}
