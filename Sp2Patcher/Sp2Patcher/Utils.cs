using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NxUtils
{

    [Serializable]
    public class Armv8Exception : Exception
    {
        public Armv8Exception() { }
        public Armv8Exception(string message) : base(message) { }
        public Armv8Exception(string message, Exception inner) : base(message, inner) { }
        protected Armv8Exception(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public static class ArmUtils
    {
        public const byte WZR = 0x1F;

        //https://github.com/qemu/qemu/blob/master/target/arm/translate-a64.c
        //https://yurichev.com/mirrors/ARMv8-A_Architecture_Reference_Manual_(Issue_A.a).pdf

        private const uint MOVZ_ID = 0b010100101;
        private const uint MOVK_ID = 0b011100101;
        private const uint NOP_ID = 0xD503201F;
        private const uint ADRP_ID = 0b100100000;
        private const uint ADD_imm_ID = 0b100100010;
        private const uint B_ID = 0x14000000;
        private const uint BL_ID = 0x94000000;
        private const uint SVC_ID = 0xD4000001;
        private const uint ORR_ID = 0b0011001000;
        
        private static int HighestSetBit(uint x)
        {
            for (int i = sizeof(uint) - 1; i >= 0; i++)
                if ((i & (1 << i)) != 0)
                    return i;
            return -1;
        }
        private static ulong Bitmask64(int length)
        {
            return ~0UL >> (64 - length);
        }
        static ulong BitfieldReplicate(ulong mask, int e)
        {
            while (e < 64)
            {
                mask |= mask << e;
                e *= 2;
            }
            return mask;
        }
        private static ulong DecodeBitMasks(uint immn, uint imms, uint immr)
        {
            int len = HighestSetBit((immn << 6) | (~imms));

            if (len < 1)
                throw new Armv8Exception("Reserved");

            int e = 1 << len;

            int levels = e - 1;
            int s = (int)(imms & levels);
            int r = (int)(immr & levels);

            if (s == levels)
                throw new Armv8Exception();

            ulong mask = Bitmask64(s + 1);
            if (r != 0)
            {
                mask = (mask >> r) | (mask << (e - r));
                mask &= Bitmask64(e);
            }
            return BitfieldReplicate(mask, e);
        }

        public static byte[] Armv8Encode_movz(byte Wd, ushort imm, int lsl = 0)
        {
            if (lsl != 0 && lsl != 16 && lsl != 32 && lsl != 48)
                throw new Armv8Exception("LSL Must be 0, 16, 32 or 48");
            if (Wd > 0x1F)
                throw new Armv8Exception("Register must be between 0 and 31");

            int regSize = 5;
            int immSize = 16;
            int shiftSize = 2;

            uint ins = (MOVZ_ID << (regSize + immSize + shiftSize));
            ins |= ((uint)(lsl / 16) << (regSize + immSize));
            ins |= ((uint)imm << regSize);
            ins |= Wd;

            return BitConverter.GetBytes(ins);
        }
        public static byte[] Armv8Encode_movk(byte Wd, ushort imm, int lsl = 0)
        {
            if (lsl != 0 && lsl != 16 && lsl != 32 && lsl != 48)
                throw new Armv8Exception("LSL Must be 0, 16, 32 or 48");
            if (Wd > 0x1F)
                throw new Armv8Exception("Register must be between 0 and 31");

            int regSize = 5;
            int immSize = 16;
            int shiftSize = 2;

            uint ins = (MOVK_ID << (regSize + immSize + shiftSize));
            ins |= ((uint)(lsl / 16) << (regSize + immSize));
            ins |= ((uint)imm << regSize);
            ins |= Wd;

            return BitConverter.GetBytes(ins);
        }
        public static byte[] Armv8Encode_adrp(byte Xd, long label, long pc)
        {
            bool neg = label < 0;
            
            if (Math.Abs(label) > 0xFFFFF000)
                throw new Armv8Exception("Label must be between -0xFFFFF000 and 0xFFFFF000");

            int regSize = 5;
            int labelSize = 18;

            uint v = ((uint)(long)(((ulong)label >> 12) - ((ulong)pc >> 12))) & 0xFFFFF;

            uint ins = ADRP_ID << (labelSize + regSize);
            ins |= (v & 3) << (labelSize + regSize + 6);
            ins |= (v >> 2) << regSize;
            ins |= Xd;
            ins |= Convert.ToUInt32(neg) << 23;

            return BitConverter.GetBytes(ins);
        }
        public static byte[] Armv8Encode_add_imm(byte Xd, byte Xn, ushort imm, int lsl = 0)
        {
            if (lsl != 0 && lsl != 12)
                throw new Armv8Exception("LSL Must be either 0 or 12");
            if (Xd > 0x1F || Xn > 0x1F)
                throw new Armv8Exception("Register must be between 0 and 31");

            int regSize = 5;
            int immSize = 12;
            int shiftSize = 1;

            uint ins = (ADD_imm_ID << (regSize + regSize + immSize + shiftSize));
            ins |= ((uint)(lsl / 12) << (regSize + regSize + immSize));
            ins |= ((uint)imm << (regSize + regSize));
            ins |= (uint)(Xn << regSize);
            ins |= Xd;

            return BitConverter.GetBytes(ins);
        }
        public static byte[] Armv8Encode_nop()
        {
            return BitConverter.GetBytes(NOP_ID);
        }
        public static byte[] Armv8Encode_b(uint label)
        {
            if (label > 0x7FFFFFC)
                throw new Armv8Exception("label cannot be higher than 0x7FFFFFC");
            if (label % 4 != 0)
                throw new Armv8Exception("label must be a power of 4");

            uint enc = B_ID | (uint)((label & ~0xF) >> 2);

            enc += (label & 0xF) / 4;
            return BitConverter.GetBytes(enc);
        }
        public static byte[] Armv8Encode_bl(uint label)
        {
            if (label > 0x7FFFFFC)
                throw new Armv8Exception("label cannot be higher than 0x7FFFFFC");
            if (label % 4 != 0)
                throw new Armv8Exception("label must be a power of 4");

            uint enc = BL_ID | (uint)((label & ~0xF) >> 2);

            enc += (label & 0xF) / 4;
            return BitConverter.GetBytes(enc);
        }
        public static byte[] Armv8Encode_svc(ushort svc)
        {
            uint enc = (uint)(SVC_ID | (svc << 5));
            return BitConverter.GetBytes(enc);
        }
        public static byte[] Armv8Encode_ret()
        {
            return new byte[] { 0xC0, 0x03, 0x5F, 0xD6 };
        }

        public static ulong Armv8Decode_orr(byte[] opcode, byte Wd, byte Wn)
        {
            int regSize = 5;
            int immSize = 6;
            uint ins = BitConverter.ToUInt32(opcode, 0);

            byte regd = (byte)(ins & 0x1F);
            byte regn = (byte)((ins >> regSize) & 0x1F);
            uint imms = (ins >> (regSize * 2)) & 0x1F;
            uint immr = (ins >> (regSize * 2 + immSize)) & 0x3F;
            uint id   = ins >> (regSize * 2 + immSize*2);

            if (id != ORR_ID)
                throw new Armv8Exception("Invalid ADD : 0x" + ins.ToString("X8"));
            if (regn != Wn)
                throw new Armv8Exception("Source register doesn't match");
            if (regd != Wd)
                throw new Armv8Exception("Destination register doesn't match");

            return DecodeBitMasks(0, imms, immr);
        }
        public static uint Armv8Decode_movz(byte[] opcode, byte Wd)
        {
            try
            { return (uint)Armv8Decode_orr(opcode, Wd, WZR); }
            catch (Armv8Exception)
            { }

            int regSize = 5;
            int immSize = 16;
            int shiftSize = 2;

            uint ins = BitConverter.ToUInt32(opcode, 0);

            uint id = ins >> (regSize + immSize + shiftSize);
            byte shift = (byte)((ins >> (regSize + immSize)) & 3);
            ushort val = (ushort)((ins >> regSize) & ushort.MaxValue);
            byte reg = (byte)(ins & 0x1F);

            if (id != MOVZ_ID || shift > 1)
                throw new Armv8Exception("Invalid MOVZ : 0x" + ins.ToString("X8"));
            if (reg != Wd)
                throw new Armv8Exception("Destination register doesn't match");


            return ((uint)val << (shift * 16));
        }

        public static uint Armv8Decode_movk(byte[] opcode, byte Wd)
        {
            int regSize = 5;
            int immSize = 16;
            int shiftSize = 2;

            uint ins = BitConverter.ToUInt32(opcode, 0);

            uint id = ins >> (regSize + immSize + shiftSize);
            byte shift = (byte)((ins >> (regSize + immSize)) & 3);
            ushort val = (ushort)((ins >> regSize) & ushort.MaxValue);
            byte reg = (byte)(ins & 0x1F);

            if (id != MOVK_ID || shift > 1)
                throw new Armv8Exception("Invalid MOVK : 0x" + ins.ToString("X8"));
            if (reg != Wd)
                throw new Armv8Exception("Destination register doesn't match");


            return ((uint)val << (shift * 16));
        }
        public static long Armv8Decode_adrp(byte[] opcode, byte Xd, uint pc)
        {
            int regSize = 5;
            int labelSize = 18;

            uint ins = BitConverter.ToUInt32(opcode, 0);

            uint id = (uint)((ins >> (regSize + labelSize)) & ~0b11000000);
            uint val = ((ins >> regSize) & 0x3FFFF) << 2;
            val |= (ins >> (regSize + labelSize + 6)) & 3;
            byte reg = (byte)(ins & 0x1F);

            if ((id & ~1) != ADRP_ID)
                throw new Armv8Exception("Invalid ADRP : 0x" + ins.ToString("X8"));
            if (reg != Xd)
                throw new Armv8Exception("Destination register doesn't match");

            val <<= 12;

            long ret = (long)val;

            if ((id & 1) != 0)
                ret = 0 - (long)(0 - val); //sxtw

            return ret + (pc & ~0xFFF);
        }
        public static long Armv8Decode_add_imm(byte[] opcode, byte Xd, byte Xn, long XnValue)
        {
            int regSize = 5;
            int immSize = 12;
            int shiftSize = 1;

            uint ins = BitConverter.ToUInt32(opcode, 0);

            uint id = ins >> (regSize + regSize + immSize + shiftSize);
            byte shift = (byte)((ins >> (regSize + regSize + immSize)) & 1);
            ushort val = (ushort)((ins >> (regSize + regSize)) & 0xFFF);
            byte regn = (byte)((ins >> regSize ) & 0x1F);
            byte regd = (byte)(ins & 0x1F);

            if (id != ADD_imm_ID)
                throw new Armv8Exception("Invalid ADD : 0x" + ins.ToString("X8"));
            if (regn != Xn)
                throw new Armv8Exception("Source register doesn't match");
            if (regd != Xd)
                throw new Armv8Exception("Destination register doesn't match");

            return ((long)val << (shift * 12)) + XnValue;
        }

        //wrappers
        public static long Armv8Decode_adrp_add(byte[] opcode, uint pc, byte Xd)
        {
            byte[] adrp = opcode.Take(4).ToArray();
            byte[] add = opcode.Skip(4).Take(4).ToArray();
            long data = Armv8Decode_adrp(adrp, Xd, pc);
            return Armv8Decode_add_imm(add, Xd, Xd, data);
        }
        public static uint Armv8Decode_movz_movk(byte[] opcode, byte Wd)
        {
            byte[] movz = opcode.Take(4).ToArray();
            byte[] movk = opcode.Skip(4).Take(4).ToArray();
            uint val = Armv8Decode_movz(movz, Wd);
            val |= Armv8Decode_movk(movk, Wd);
            return val;
        }

        public static byte[] Armv8Encode_adrp_add(uint pc, byte Xd, long value)
        {
            byte[] adrp = Armv8Encode_adrp(Xd, value, pc);
            byte[] add = Armv8Encode_add_imm(Xd, Xd, (ushort)(value & 0xFFF));
            return adrp.Concat(add).ToArray();
        }
        public static byte[] Armv8Encode_movz_movk(uint value, byte Wd)
        {
            ushort upper = (ushort)((value >> 16) & ushort.MaxValue);
            ushort lower = (ushort)(value & ushort.MaxValue);
            byte[] movz = Armv8Encode_movz(Wd, upper, 16);
            byte[] movk = Armv8Encode_movk(Wd, lower);
            return movz.Concat(movk).ToArray();
        }
    }
    public static class Utils
    {
        public static string GetTempDir()
        {
            string outdir = "";
            do outdir = Path.GetRandomFileName();
            while (Directory.Exists(outdir));

            Directory.CreateDirectory(outdir);
            return outdir;
        }

        public static int BIT(int shift)
        {
            return 1 << shift;
        }

        public static string BytesToHex(byte[] b, string separator = "")
        {
            return BitConverter.ToString(b, 0).Replace("-", separator);
        }
        public static byte[] HexToBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
