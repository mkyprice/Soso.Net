using System;
using System.Text;
using Soso.Utils.Logging;
using Soso.Utils.Logging.Internals;
using UnityEngine;

namespace DefaultNamespace.Utils
{
    public class UnityLogFormatter : ILogWriter
    {
        public static string DEFAULT_COLOR = ToColorHex(Color.green);
        public static string PRIMITIVE_COLOR = ToColorHex(Color.cyan);
        private static string EXCEPTION_COLOR = ToColorHex(Color.magenta);
        public static string NULL_COLOR = ToColorHex(Color.red);

        public static string ToColorHex(Color color)
        {
            return string.Format("<color=#{0:X2}{1:X2}{2:X2}>", ToByte(color.r), ToByte(color.g), ToByte(color.b));
        }

        private static byte ToByte(float value)
        {
            value = Mathf.Clamp01(value);
            return (byte)(value * 255);
        }

        private static readonly StringBuilder _reausableStringBuilder = new StringBuilder();

        private string GetColor(Type type)
        {
            if (type == null)
            {
                return NULL_COLOR;
            }

            if (type.IsPrimitive)
            {
                return PRIMITIVE_COLOR;
            }

            if (type.BaseType == typeof(Exception))
            {
                return EXCEPTION_COLOR;
            }

            return DEFAULT_COLOR;
        }

        [HideInCallstack]
        public void Write(LOG_LEVEL level, string template)
        {
            switch (level)
            {
                case LOG_LEVEL.Debug:
                    UnityEngine.Debug.Log(template);
                    break;
                case LOG_LEVEL.Info:
                    UnityEngine.Debug.Log(template);
                    break;
                case LOG_LEVEL.Warn:
                    UnityEngine.Debug.LogWarning(template);
                    break;
                case LOG_LEVEL.Error:
                    UnityEngine.Debug.LogError(template);
                    break;
            }
        }

        [HideInCallstack]
        public void Write(LOG_LEVEL level, char[] template, ReadOnlySpan<MessageToken> tokens, ReadOnlySpan<object> props)
        {
            _reausableStringBuilder.Clear();

            foreach (var token in tokens)
            {
                if (token.PropertyIndex >= 0)
                {
                    var prop = props[token.PropertyIndex];
                    if (prop != null)
                    {
                        Type type = prop.GetType();
                        _reausableStringBuilder
                            .Append(GetColor(type))
                            .Append(prop)
                            .Append("</color>");
                    }
                }
                else
                {
                    _reausableStringBuilder.Append(template, token.Index, token.Length);
                }
            }

            string formatted = _reausableStringBuilder.ToString();
            Write(level, formatted);
        }
    }
}