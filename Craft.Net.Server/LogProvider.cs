using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Craft.Net.Server
{
    public static class LogProvider
    {
        static LogProvider()
        {
            logProviders = new List<ILogProvider>();
            MemoryStream = new MemoryStream();
            MinecraftStream = new MinecraftStream(MemoryStream);
        }

        private static List<ILogProvider> logProviders { get; set; }

        public static void RegisterProvider(ILogProvider logProvider)
        {
            logProviders.Add(logProvider);
        }

        public static void Log(string text)
        {
            Log(text, LogImportance.High);
        }

        public static void Log(string text, LogImportance importance)
        {
            foreach (ILogProvider provider in logProviders)
                provider.Log(text, importance);
        }

        private static MemoryStream MemoryStream { get; set; } // Used for getting raw packet data
        private static MinecraftStream MinecraftStream { get; set; } // Used for getting raw packet data
        public static void Log(IPacket packet, bool clientToServer)
        {
            var type = packet.GetType(); 
            var fields = type.GetFields();
            var builder = new StringBuilder();
            // Log time, direction, name
            builder.Append(DateTime.Now.ToString("{hh:mm:ss.fff} "));
            if (clientToServer)
                builder.Append("[CLIENT->SERVER] ");
            else
                builder.Append("[SERVER->CLIENT] ");
            builder.Append(FormatPacketName(type.Name));
            builder.Append(" (0x"); builder.Append(packet.Id.ToString("X2")); builder.Append(")");
            builder.AppendLine();

            // Log raw data
            MemoryStream.Seek(0, SeekOrigin.Begin);
            MemoryStream.SetLength(0);
            packet.WritePacket(MinecraftStream);
            builder.Append(DumpArrayPretty(MemoryStream.GetBuffer().Take((int)MemoryStream.Length).ToArray()));

            // Log fields
            foreach (var field in fields)
            {
                if (field.IsStatic)
                    continue;
                var name = field.Name;
                name = AddSpaces(name);
                var fValue = field.GetValue(packet);

                if (!(fValue is Array))
                    builder.Append(string.Format(" {0} ({1})", name, field.FieldType.Name));
                else
                {
                    var array = fValue as Array;
                    builder.Append(string.Format(" {0} ({1}[{2}])", name,
                        array.GetType().GetElementType().Name, array.Length));
                }

                if (fValue is byte[])
                    builder.Append(": " + DumpArray(fValue as byte[]) + "\n");
                else if (fValue is Array)
                {
                    builder.Append(": ");
                    var array = fValue as Array;
                    foreach (var item in array)
                        builder.Append(string.Format("{0}, ", item.ToString()));
                    builder.AppendLine();
                }
                else
                    builder.Append(": " + fValue + "\n");
            }
            Log(builder.ToString(), LogImportance.Low);
        }

        private static string FormatPacketName(string name)
        {
            if (name.EndsWith("Packet"))
                name = name.Remove(name.Length - "Packet".Length);
            // TODO: Consider adding spaces before capital letters
            return name;
        }

        public static string DumpArray(byte[] array)
        {
            if (array.Length == 0)
                return "[]";
            var sb = new StringBuilder((array.Length * 2) + 2);
            foreach (byte b in array)
                sb.AppendFormat("{0} ", b.ToString("X2"));
            return "[" + sb.ToString().Remove(sb.Length - 1) + "]";
        }

        public static string DumpArrayPretty(byte[] array)
        {
            if (array.Length == 0)
                return "[Empty arry]";
            int length = 5 * array.Length + (4 * (array.Length / 16)) + 2; // rough estimate of final length
            var sb = new StringBuilder(length);
            sb.AppendLine("[");
            for (int i = 0; i < array.Length; i += 16)
            {
                sb.Append(" ");
                // Hex dump
                int hexCount = 16;
                for (int j = i; j < array.Length && j < i + 16; j++, hexCount--)
                    sb.AppendFormat("{0} ", array[j].ToString("X2"));
                sb.Append(" ");
                for (; hexCount > 0; hexCount--)
                    sb.Append("   ");
                for (int j = i; j < array.Length && j < i + 16; j++)
                {
                    char value = Encoding.ASCII.GetString(new byte[] { array[j] })[0];
                    if (char.IsLetterOrDigit(value))
                        sb.AppendFormat("{0} ", value);
                    else
                        sb.Append(". ");
                }
                sb.AppendLine();
            }
            sb.AppendLine("]");
            string result = " " + sb.ToString().Replace("\n", "\n ");
            return result.Remove(result.Length - 2);
        }

        public static string AddSpaces(string value)
        {
            string newValue = "";
            foreach (char c in value)
            {
                if (char.IsLower(c))
                    newValue += c;
                else
                    newValue += " " + c;
            }
            return newValue.Substring(1);
        }
    }
}
