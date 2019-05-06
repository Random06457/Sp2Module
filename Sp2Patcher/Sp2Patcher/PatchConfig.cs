using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NxUtils;
using System.Globalization;
using System.IO;

namespace Sp2Patcher
{

    [Serializable]
    public class PatchConfigException : Exception
    {
        public PatchConfigException() { }
        public PatchConfigException(string message) : base(message) { }
        public PatchConfigException(string message, Exception inner) : base(message, inner) { }
        protected PatchConfigException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    public enum PatchTypeEnum
    {
        bl,
        b,
        data,
        ptr,
    }

    public class MemAddress
    {
        public bool IsSymbol;
        public string Symbol;
        public NsoTypeEnum NsoType;
        public uint Offset;

        public MemAddress(string s)
        {
            IsSymbol = s.StartsWith("!");
            if (IsSymbol)
            {
                Symbol = s.Substring(1);
            }
            else
            {
                if (!Regex.IsMatch(s, @"^(rtld|main|sdk)\+(([0-9]{1,})|(0x[0-9a-fA-F]{1,}))$"))
                    throw new PatchConfigException($"Invalid address : {s}");

                var split = s.Split('+');
                NsoType = (NsoTypeEnum)Enum.Parse(typeof(NsoTypeEnum), split[0]);

                Offset = (split[1].StartsWith("0x"))
                    ? uint.Parse(split[1].Substring(2), NumberStyles.HexNumber)
                    : uint.Parse(split[1]);
            }
        }

        public long GetAddress(ExeFS exefs, List<Tuple<string, uint>> symbols)
        {
            if (IsSymbol)
            {
                foreach (var sym in symbols)
                    if (sym.Item1 == Symbol)
                        return exefs.GetNsoAddress(NsoTypeEnum.sdk) + sym.Item2;
                throw new PatchConfigException($"Could not find symbol : {Symbol}");
            }
            else
            {
                NsoTypeEnum type = NsoType;
                if (type == NsoTypeEnum.sdk)
                    type = NsoTypeEnum.subsdk0;
                return exefs.GetNsoAddress(type) + Offset;
            }
        }
        public override string ToString() => IsSymbol ? $"!{Symbol}" : $"{NsoType}+0x{Offset:X}";
    }

    public class PatchEntry
    {
        public MemAddress Address;
        public PatchTypeEnum PatchType;
        public object Data;

        public PatchEntry(string line)
        {
            var parts = line.Split(new string[] { "->" }, StringSplitOptions.None);
            if (parts.Length < 2)
                throw new PatchConfigException($"Invalid Entry : \"{line}\"");

            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim(' ');


            PatchType = (PatchTypeEnum)Enum.Parse(typeof(PatchTypeEnum), parts[0]);
            Address = new MemAddress(parts[1]);

            switch (PatchType)
            {
                case PatchTypeEnum.bl:
                case PatchTypeEnum.b:
                case PatchTypeEnum.ptr:
                    if (parts.Length != 3)
                        throw new PatchConfigException($"Invalid Entry : \"{line}\"");
                    Data = new MemAddress(parts[2]);
                    break;
                case PatchTypeEnum.data:
                    if (parts.Length != 3)
                        throw new PatchConfigException($"Invalid Entry : \"{line}\"");
                    Data = Utils.HexToBytes(parts[2]);
                    break;
                default:
                    throw new PatchConfigException("Invalid patch type");
            }
        }
        public void Apply(ExeFS exefs, List<Tuple<string, uint>> symbols)
        {
            long patchAddr = Address.GetAddress(exefs, symbols);
            byte[] buffer = new byte[0];

            switch (PatchType)
            {
                case PatchTypeEnum.ptr:
                    buffer = BitConverter.GetBytes(((MemAddress)Data).GetAddress(exefs, symbols));
                    break;
                case PatchTypeEnum.bl:
                    long blHookAddr = ((MemAddress)Data).GetAddress(exefs, symbols);
                    buffer = ArmUtils.Armv8Encode_bl((uint)(blHookAddr - patchAddr));
                    break;
                case PatchTypeEnum.b:
                    long bHookAddr = ((MemAddress)Data).GetAddress(exefs, symbols);
                    buffer = ArmUtils.Armv8Encode_b((uint)(bHookAddr - patchAddr));
                    break;
                case PatchTypeEnum.data:
                    buffer = (byte[])Data;
                    break;
                default:
                    throw new Exception($"Invalid Patch Type : {PatchType}");
            }

            exefs.WriteExeFs(patchAddr, buffer);
        }

        public override string ToString()
        {
            string dataStr = "";
            switch (PatchType)
            {
                case PatchTypeEnum.bl:
                case PatchTypeEnum.b:
                case PatchTypeEnum.ptr:
                    dataStr = ((MemAddress)Data).ToString();
                    break;
                case PatchTypeEnum.data:
                    dataStr = Utils.BytesToHex((byte[])Data);
                    break;
            }
            return $"{Address} : {PatchType} -> {dataStr}";
        }
    }

    public static class PatchConfig
    {
        public static List<PatchEntry> GetHooks(string file)
        {
            List<PatchEntry> entries = new List<PatchEntry>();
            var lines = File.ReadAllLines(file);
            foreach (var line in lines)
            {
                string s = line.Trim('\t', ' ');
                if (string.IsNullOrEmpty(s) || s.StartsWith("#"))
                    continue;
                entries.Add(new PatchEntry(s));
            }
            return entries;
        }
    }
}