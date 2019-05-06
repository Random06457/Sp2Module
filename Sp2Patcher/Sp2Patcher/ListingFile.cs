using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace Sp2Patcher
{

    [Serializable]
    public class ListingFileException : Exception
    {
        public ListingFileException() { }
        public ListingFileException(string message) : base(message) { }
        public ListingFileException(string message, Exception inner) : base(message, inner) { }
        protected ListingFileException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    public static class ListingFile
    {
        public static List<Tuple<string, uint>> GetSymbols(string filename)
        {
            List<Tuple<string, uint>> symbols = new List<Tuple<string, uint>>();

            var lines = File.ReadAllLines(filename);
            foreach (var line in lines)
            {
                var parts = line.Split(new char[] { ' ' }, 4);
                string symbol = "";
                switch (parts.Length)
                {
                    case 3: symbol = parts[2]; break;
                    case 4: symbol = parts[3]; break;
                    default: continue;
                }

                //int index = symbol.IndexOf("(");
                //if (index > 0)
                //    symbol = symbol.Substring(0, index);

                symbols.Add(new Tuple<string, uint>(symbol, uint.Parse(parts[0], NumberStyles.HexNumber)));
            }
            return symbols;
        }
    }
}
