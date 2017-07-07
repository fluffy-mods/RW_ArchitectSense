// Karel Kroeze
// Logger.cs
// 2017-05-07

using System;
using System.ComponentModel;
using RimWorld;
using Verse;

namespace ArchitectSense
{
    public class Logger
    {
        private string _identifier;
        public Logger( string identifier ) { _identifier = identifier; }

        public void Warning (string msg, params object [] args) { Log.Warning (String.Format ("{0} :: {1}", _identifier, string.Format (msg, args))); }
        public void Message (string msg, params object [] args) { Log.Warning (String.Format ("{0} :: {1}", _identifier, string.Format (msg, args))); }
        public void Error (string msg, params object [] args) { Log.Warning (String.Format ("{0} :: {1}", _identifier, string.Format (msg, args))); }
    }
}