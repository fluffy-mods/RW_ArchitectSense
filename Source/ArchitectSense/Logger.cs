// Karel Kroeze
// Logger.cs
// 2017-05-07

using System;
using System.ComponentModel;
using System.Diagnostics;
using RimWorld;
using Verse;

namespace ArchitectSense
{
    public class Logger
    {
        private string _identifier;

        public Logger(string identifier)
        {
            _identifier = identifier;
        }

        private string FormatMessage(string msg, params object[] args)
        {
            return $"{_identifier} :: {string.Format(msg, args)}";
        }

        public void Warning(string msg, params object[] args)
        {
            Log.Warning(FormatMessage(msg, args));
        }

        public void Message(string msg, params object[] args)
        {
            Log.Message(FormatMessage(msg, args));
        }

        public void Error(string msg, params object[] args)
        {
            Log.Error(FormatMessage(msg, args));
        }

        [Conditional("DEBUG")]
        public void Debug(string msg, params object[] args)
        {
            Message(msg, args);
        }
    }
}