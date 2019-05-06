using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LZ4;
using System.Security.Cryptography;

namespace NxUtils
{

    [Serializable]
    public class NsoException : Exception
    {
        public NsoException() { }
        public NsoException(string message) : base(message) { }
        public NsoException(string message, Exception inner) : base(message, inner) { }
        protected NsoException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public enum SegPerm
    {
        None = 0,
        R = 1 << 0,
        W = 1 << 1,
        X = 1 << 2,
        RW = R | W,
        RX = R | X,
    }

    public enum NsoSegmentType
    {
        text,
        rodata,
        rwdata,
    }

    public interface INsoSegment
    {
        NsoSegmentType SegmentType { get; }
        bool Compressed { get; set; }
        uint FileOffset { get; }
        uint Address { get; }
        uint DecompressedSize { get; }
        uint CompressedSize { get; }
        uint BssOrAlign { get; set; }
        byte[] Sha256 { get; }
    }

    public class DynamicInfo
    {
        private static List<Tuple<DtTagEnum, string>> fields = new List<Tuple<DtTagEnum, string>>()
        {
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_SYMTAB, nameof(SymTab)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_REL, nameof(Rela)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_RELASZ, nameof(RelaSize)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_JMPREL, nameof(JumpRela)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_PLTRELSZ, nameof(PltRelSize)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_STRTAB, nameof(StringTable)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_STRSZ, nameof(StringSize)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_PLTGOT, nameof(PltGot)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_HASH, nameof(Hash)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_GNU_HASH, nameof(GnuHash)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_INIT, nameof(Init)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_FINI, nameof(Fini)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_INIT_ARRAY, nameof(InitArray)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_INIT_ARRAYSZ, nameof(InitArraySize)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_FINI_ARRAY, nameof(FiniArray)),
            new Tuple<DtTagEnum, string>(DtTagEnum.DT_FINI_ARRAYSZ, nameof(FiniArraySize)),
        };
        public ulong SymTab { get; set; }
        public ulong Rela { get; set; }
        public ulong RelaSize { get; set; }
        public ulong JumpRela { get; set; }
        public ulong PltRelSize { get; set; }
        public ulong StringTable { get; set; }
        public ulong StringSize { get; set; }
        public ulong PltGot { get; set; }
        public ulong Hash { get; set; }
        public ulong GnuHash { get; set; }
        public ulong Init { get; set; }
        public ulong Fini { get; set; }
        public ulong InitArray { get; set; }
        public ulong InitArraySize { get; set; }
        public ulong FiniArray { get; set; }
        public ulong FiniArraySize { get; set; }

        public DynamicInfo(Nso nso, long addr)
        {
            byte[] buffer = nso.ReadNso((uint)addr, 0x200);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                Elf64_Dyn dyn = new Elf64_Dyn(br);

                while (dyn.d_tag != DtTagEnum.DT_NULL)
                {
                    foreach (var item in fields)
                        if (dyn.d_tag == item.Item1)
                            this.GetType().GetProperty(item.Item2).SetValue(this, dyn.d_un);

                    dyn = new Elf64_Dyn(br);
                }
            }

        }
    }

    public class NsoSymbol
    {
        public string Value { get; private set; }
        public ulong Address { get; private set; }
        public ulong Size { get; private set; }
        public SymbolBindingEnum Binding { get; private set; }
        public SymbolTypeEnum Type { get; private set; }

        public NsoSymbol(Elf64_Sym sym, byte[] strTableBuff)
        {
            List<byte> strBuff = new List<byte>();

            for (uint c = sym.st_name; c < strTableBuff.Length && strTableBuff[c] != 0; c++)
                strBuff.Add(strTableBuff[c]);

            Value = Encoding.UTF8.GetString(strBuff.ToArray());
            Address = sym.st_value;
            Size = sym.st_size;
            Binding = (SymbolBindingEnum)(sym.st_info >> 4);
            Type = (SymbolTypeEnum)(sym.st_info & 0xF);
        }

        public override string ToString()
        {
            return $"{Address:X16} : {Value}";
        }
    }

    public class ModHeader
    {
        private const string HEADER_MAGIC = "MOD0";
        public const int HEADER_SIZE = 0x1C;

        private long baseAddr;

        private int dynAddr;
        private int bssStart;
        private int bssEnd;
        private int ehFrameStart;
        private int ehFrameEnd;
        private int moduleObj;

        public DynamicInfo DynInfo { get; set; }
        public long DynamicAddr { get { return baseAddr + dynAddr; } private set { dynAddr = (int)(value - baseAddr); } }
        public long BssStartAddr { get { return baseAddr + bssStart; } private set { bssStart = (int)(value - baseAddr); } }
        public long BssEndAddr { get { return baseAddr + bssEnd; } private set { bssEnd = (int)(value - baseAddr); } }
        public long EhStartAddr { get { return baseAddr + ehFrameStart; } private set { ehFrameStart = (int)(value - baseAddr); } }
        public long EhEndAddr { get { return baseAddr + ehFrameEnd; } private set { ehFrameEnd = (int)(value - baseAddr); } }
        public long ModuleObjAddr { get { return baseAddr + moduleObj; } private set { moduleObj = (int)(value - baseAddr); } }

        public ModHeader(Nso nso)
        {
            baseAddr = nso.GetModAddr();

            byte[] buffer = nso.ReadNso((uint)baseAddr, 0x1C);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);

                string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
                if (magic != HEADER_MAGIC)
                    throw new NsoException("Invalid MOD0 header");

                dynAddr = br.ReadInt32();
                bssStart = br.ReadInt32();
                bssEnd = br.ReadInt32();
                ehFrameStart = br.ReadInt32();
                ehFrameEnd = br.ReadInt32();
                moduleObj = br.ReadInt32();
                
                DynInfo = new DynamicInfo(nso, DynamicAddr);
            }
        }
        private ModHeader()
        {

        }        

        public void Rewrite(Nso nso, uint newAddr)
        {
            dynAddr = (int)(DynamicAddr - newAddr);
            bssStart = (int)(BssStartAddr - newAddr);
            bssEnd = (int)(BssEndAddr - newAddr);
            ehFrameStart = (int)(EhStartAddr - newAddr);
            ehFrameEnd = (int)(EhEndAddr - newAddr);
            moduleObj = (int)(ModuleObjAddr - newAddr);

            Write(nso);
        }

        public void Write(Nso nso)
        {
            byte[] raw = new byte[HEADER_SIZE];
            using (MemoryStream ms = new MemoryStream(raw))
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(Encoding.ASCII.GetBytes(HEADER_MAGIC));
                bw.Write(dynAddr);
                bw.Write(bssStart);
                bw.Write(bssEnd);
                bw.Write(ehFrameStart);
                bw.Write(ehFrameEnd);
                bw.Write(moduleObj);
            }

            nso.WriteNso(4, BitConverter.GetBytes((uint)baseAddr));
            nso.WriteNso((uint)baseAddr, raw);
        }

        public static ModHeader CreateModHeader(Nso nso, long modBase, long dyn, long ehstart, long ehend, long modObj)
        {
            ModHeader mod = new ModHeader();
            mod.baseAddr = modBase;

            mod.DynamicAddr = dyn;
            mod.BssStartAddr = nso.RwdataSegment.Address + nso.RwdataSegment.DecompressedSize;
            mod.BssEndAddr = mod.BssStartAddr + nso.RwdataSegment.BssOrAlign;
            mod.EhStartAddr = ehstart;
            mod.EhEndAddr = ehend;
            mod.ModuleObjAddr = modObj;

            mod.Write(nso);

            return mod;
        }
    }

    public struct DataExtent
    {
        public uint offset;
        public uint size;

        public DataExtent(BinaryReader br)
        {
            offset = br.ReadUInt32();
            size = br.ReadUInt32();
        }
    };

    public class Nso
    {
        private class NsoSegment : INsoSegment
        {
            internal byte[] RawData;

            public NsoSegmentType SegmentType { get; set; }
            public bool Compressed { get; set; }
            public uint FileOffset { get; set; }
            public uint Address { get; set; }
            public uint DecompressedSize { get; set; }
            public uint CompressedSize { get; set; }
            public uint BssOrAlign { get; set; }
            public byte[] Sha256 { get; set; }

            public NsoSegment(BinaryReader br, int index, uint flag)
            {
                SegmentType = (NsoSegmentType)index;
                Compressed = ((flag >> index) & 1) != 0;
                FileOffset = br.ReadUInt32();
                Address = br.ReadUInt32();
                DecompressedSize = br.ReadUInt32();
                BssOrAlign = br.ReadUInt32();
            }
            public NsoSegment(uint addr, byte[] data, NsoSegmentType type, uint bssOrAlign = 0x100, bool compressed = true)
            {
                SegmentType = type;
                Compressed = compressed;
                FileOffset = 0;
                RawData = data;
                Address = addr;
                DecompressedSize = (uint)data.Length;
                BssOrAlign = bssOrAlign;
            }
        }

        private const int SEGMENT_NB = 3;
        private const int HEADER_SIZE = 0x100;
        private const string HEADER_MAGIC = "NSO0";

        public ModHeader ModSection { get; set; }
        private NsoSegment[] segments = new NsoSegment[SEGMENT_NB];
        
        public byte[] BuildId = new byte[0x20];
        public uint Flag { get; private set; }
        public INsoSegment TextSegment { get { return segments[(int)NsoSegmentType.text]; } }
        public INsoSegment RodataSegment { get { return segments[(int)NsoSegmentType.rodata]; } }
        public INsoSegment RwdataSegment { get { return segments[(int)NsoSegmentType.rwdata]; } }
        public DataExtent DynamicStringTable { get; private set; }
        public DataExtent DynamicSymbolTable { get; private set; }

        public uint StartAddress { get { return TextSegment.Address; } }
        public uint EndAddress { get { return RwdataSegment.Address + RwdataSegment.DecompressedSize + RwdataSegment.BssOrAlign; } }

        private Nso() { }
        public Nso(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                BinaryReader br = new BinaryReader(fs);
                ParseFile(br);
            }
        }
        public Nso(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            ParseFile(br);
        }

        private void ParseFile(BinaryReader br)
        {
            string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (magic != HEADER_MAGIC)
                throw new NsoException("Invalid magic");

            br.ReadUInt64();//padding
            Flag = br.ReadUInt32();

            for (int i = 0; i < SEGMENT_NB; i++)
                segments[i] = new NsoSegment(br, i, Flag);

            BuildId = br.ReadBytes(0x20);

            for (int i = 0; i < SEGMENT_NB; i++)
                segments[i].CompressedSize = br.ReadUInt32();

            br.BaseStream.Seek(0x24, SeekOrigin.Current); //padding

            DynamicStringTable = new DataExtent(br);
            DynamicSymbolTable = new DataExtent(br);

            for (int i = 0; i < SEGMENT_NB; i++)
                segments[i].Sha256 = br.ReadBytes(0x20);

            for (int i = 0; i < SEGMENT_NB; i++)
            {
                segments[i].RawData = new byte[segments[i].DecompressedSize];

                br.BaseStream.Position = segments[i].FileOffset;

                if (segments[i].Compressed)
                {
                    byte[] comp = br.ReadBytes((int)segments[i].CompressedSize);
                    segments[i].RawData = LZ4Codec.Decode(comp, 0, comp.Length, (int)segments[i].DecompressedSize);
                }
                else
                    segments[i].RawData = br.ReadBytes((int)segments[i].CompressedSize);
            }

            try
            {
                ModSection = new ModHeader(this);
            }
            catch (NsoException)
            {
                ModSection = null;
            }
        }
        private void WriteFile(BinaryWriter bw)
        {
            bw.Write(Encoding.ASCII.GetBytes(HEADER_MAGIC));
            bw.Write((ulong)0); //padding
            bw.Write(Flag);
            //bw.Write(0);

            byte[][] comp = new byte[SEGMENT_NB][];

            uint off = Math.Max(segments[0].FileOffset, HEADER_SIZE);
            for (int i = 0; i < SEGMENT_NB; i++)
            {
                segments[i].FileOffset = off;
                //segments[i].Compressed = false;
                comp[i] = segments[i].Compressed
                    ? LZ4Codec.Encode(segments[i].RawData, 0, segments[i].RawData.Length)
                    : segments[i].RawData;

                segments[i].CompressedSize = (uint)comp[i].Length;
                off += segments[i].CompressedSize;
            }

            for (int i = 0; i < SEGMENT_NB; i++)
            {
                bw.Write(segments[i].FileOffset);
                bw.Write(segments[i].Address);
                bw.Write(segments[i].DecompressedSize);
                bw.Write(segments[i].BssOrAlign);
            }

            bw.Write(BuildId);

            for (int i = 0; i < SEGMENT_NB; i++)
                bw.Write(segments[i].CompressedSize);

            bw.Write(new byte[0x24]);//padding

            bw.Write(DynamicStringTable.offset);
            bw.Write(DynamicStringTable.size);
            bw.Write(DynamicSymbolTable.offset);
            bw.Write(DynamicSymbolTable.size);

            for (int i = 0; i < SEGMENT_NB; i++)
            {
                SHA256 sha = SHA256.Create();
                segments[i].Sha256 = sha.ComputeHash(segments[i].RawData);
                bw.Write(segments[i].Sha256);
            }

            for (int i = 0; i < SEGMENT_NB; i++)
            {
                while (bw.BaseStream.Position < segments[i].FileOffset)
                    bw.Write((byte)0);

                bw.Write(comp[i]);
            }
        }

        public void BuildNso(string path)
        {
            using (var fs = File.Create(path))
            {
                BinaryWriter bw = new BinaryWriter(fs);
                WriteFile(bw);
            }
        }

        public string GetBinName()
        {
            string name = "";
            using (var rodata = new MemoryStream(((NsoSegment)RodataSegment).RawData))
            {
                BinaryReader br = new BinaryReader(rodata);
                br.BaseStream.Position = 0x4;
                int len = br.ReadInt32();
                name = Encoding.ASCII.GetString(br.ReadBytes(len)).Replace("\0", "");
            }
            return name;
        }
        public uint GetModAddr()
        {
            return BitConverter.ToUInt32(ReadNso(4, 4), 0);
        }
        public byte[] ReadNso(uint addr, int size)
        {
            NsoSegment seg = (NsoSegment)GetAddressSegment(addr);
            if (seg == null)
                throw new NsoException("Invalid Address");

            if (addr + size > seg.Address + seg.DecompressedSize)
                throw new NsoException("Invalid Size");

            using (MemoryStream ms = new MemoryStream(seg.RawData))
            {
                BinaryReader br = new BinaryReader(ms);
                br.BaseStream.Position = addr - seg.Address;
                return br.ReadBytes(size);
            }
        }
        public void WriteNso(uint addr, byte[] data)
        {
            NsoSegment seg = (NsoSegment)GetAddressSegment(addr);
            if (seg == null)
                throw new NsoException("Invalid Address");

            if (addr + data.Length > seg.Address + seg.DecompressedSize)
                throw new NsoException("Invalid Size");

            using (MemoryStream ms = new MemoryStream(seg.RawData))
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.BaseStream.Position = addr - seg.Address;
                bw.Write(data);
                SHA256 sha = SHA256.Create();
                seg.Sha256 = sha.ComputeHash(seg.RawData);
            }
        }
        public void ResizeSegment(INsoSegment segment, uint newSize)
        {
            if (!(segment is NsoSegment))
                throw new NsoException("Cannot resize unknown segment");

            ((NsoSegment)segment).DecompressedSize = newSize;
            byte[] buff = new byte[newSize];
            Buffer.BlockCopy(((NsoSegment)segment).RawData, 0, buff, 0, (int)Math.Min(((NsoSegment)segment).RawData.Length, newSize));
            ((NsoSegment)segment).RawData = buff;
        }

        public List<NsoSymbol> GetSymbolTable()
        {
            List<NsoSymbol> symbols = new List<NsoSymbol>();

            byte[] symTableBuff = ReadNso(RodataSegment.Address + DynamicSymbolTable.offset, (int)DynamicSymbolTable.size);
            byte[] strTableBuff = ReadNso(RodataSegment.Address + DynamicStringTable.offset, (int)DynamicStringTable.size);

            using (MemoryStream ms = new MemoryStream(symTableBuff))
            {
                BinaryReader br = new BinaryReader(ms);
                for (int i = 0; i < symTableBuff.Length; i += Elf64_Sym.ELF64_SYM_SIZE)
                {
                    var sym = new Elf64_Sym(br);
                    symbols.Add(new NsoSymbol(sym, strTableBuff));
                }
            }


            return symbols;
        }

        public SegPerm GetAddressPerm(uint addr)
        {
            var seg = GetAddressSegment(addr);
            if (seg == null) return SegPerm.None;

            switch (seg.SegmentType)
            {
                case NsoSegmentType.text: return SegPerm.RX;
                case NsoSegmentType.rodata: return SegPerm.R;
                case NsoSegmentType.rwdata: return SegPerm.RW;
                default: return SegPerm.None;
            }
        }
        public INsoSegment GetAddressSegment(uint addr)
        {
            for (int i = 0; i < SEGMENT_NB; i++)
                if (addr >= segments[i].Address && addr < segments[i].Address + segments[i].DecompressedSize)
                    return segments[i];

            return null;
        }
        public void ExtractSegment(string path, INsoSegment segment)
        {
            if (!(segment is NsoSegment))
                throw new NsoException("Invalid segment");

            using (MemoryStream ms = new MemoryStream(((NsoSegment)segment).RawData))
            using (var fs = File.Create(path))
                ms.CopyTo(fs);
        }
        public void ExtractSegment(string path, NsoSegmentType segType)
        {
            ExtractSegment(path, segments[(int)segType]);
        }

        public static Nso CreateRwDataNso(byte[] rwdata)
        {
            Nso nso = new Nso();
            nso.Flag = 0x1F;

            byte[] text = new byte[0];
            byte[] rodata;

            //mod0
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(Encoding.ASCII.GetBytes("MOD0"));
                bw.Write(0); //dyninfo
                bw.Write(0); //bss start
                bw.Write(0); //bss end
                bw.Write(0); //eh_frame_hdr start
                bw.Write(0); //eh_frame_hdr end

                rodata = ms.GetBuffer();
            }

            uint addr = 0;
            nso.segments[(int)NsoSegmentType.text] = new NsoSegment(addr, text, NsoSegmentType.text);
            addr += (uint)(((uint)text.Length + 0xFFF) & ~0xFFF);

            nso.segments[(int)NsoSegmentType.rodata] = new NsoSegment(addr, rodata, NsoSegmentType.rodata);
            addr += (uint)(((uint)rodata.Length + 0xFFF) & ~0xFFF);

            nso.segments[(int)NsoSegmentType.rwdata] = new NsoSegment(addr, rwdata, NsoSegmentType.rwdata, 0);

            try
            {
                nso.ModSection = new ModHeader(nso);
            }
            catch (NsoException)
            {
                nso.ModSection = null;
            }

            return nso;
        }
        public static Nso CreateNso(byte[] text, byte[] rodata, byte[] rwdata, uint bssSize)
        {
            Nso nso = new Nso();
            nso.Flag = 0x1F;

            uint addr = 0;
            nso.segments[(int)NsoSegmentType.text] = new NsoSegment(addr, text, NsoSegmentType.text);
            addr += (uint)(((uint)text.Length + 0xFFF) & ~0xFFF);

            nso.segments[(int)NsoSegmentType.rodata] = new NsoSegment(addr, rodata, NsoSegmentType.rodata);
            addr += (uint)(((uint)rodata.Length + 0xFFF) & ~0xFFF);

            nso.segments[(int)NsoSegmentType.rwdata] = new NsoSegment(addr, rwdata, NsoSegmentType.rwdata, bssSize);
            return nso;
        }
    }

    public class NsoStream : Stream
    {
        private bool IgnoreReadPerm;
        private uint pos = 0;
        private Nso nso;

        public override bool CanRead {
            get {
                return IgnoreReadPerm || (nso.GetAddressPerm((uint)Position) & SegPerm.R) != SegPerm.None;
            }
        }
        public override bool CanSeek
        {
            get {
                return true;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return (nso.GetAddressPerm((uint)Position) & SegPerm.W) != SegPerm.None;
            }
        }
        public override long Length {
            get {
                return nso.RodataSegment.Address + nso.RodataSegment.DecompressedSize + nso.RodataSegment.BssOrAlign;
            }
        }
        public override long Position
        {
            get { return pos; }
            set {
                if (value < 0 || value > uint.MaxValue || (!IgnoreReadPerm && value > Length))
                    throw new EndOfStreamException();

                pos = (uint)value;
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int newSize = (pos + count) > Length
            ? (int)(Length - (pos + count))
            : count;
            
            if (IgnoreReadPerm)
            {
                byte[] buff = new byte[count];
                var seg = nso.GetAddressSegment(pos);
                Buffer.BlockCopy(nso.ReadNso(pos, newSize), 0, buff, 0, count);
                Buffer.BlockCopy(buff, 0, buffer, offset, newSize);
                return count;
            }
            else
            {
                byte[] buff = nso.ReadNso(pos, newSize);
                Buffer.BlockCopy(buff, 0, buffer, offset, newSize);
                return newSize;
            }
            Position += newSize;
        }

        public override long Seek(long offset, SeekOrigin origin = SeekOrigin.Current)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: Position = offset; break;
                case SeekOrigin.Current: Position += offset; break;
                case SeekOrigin.End: Position = Length + offset; break;
                default: throw new InvalidDataException("Unknown origin");
            }
            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public NsoStream(Nso nso, bool ignorePermR = false) : base()
        {
            IgnoreReadPerm = ignorePermR;
            this.nso = nso;
        }
    }
}
