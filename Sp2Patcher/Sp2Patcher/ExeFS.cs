using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NxUtils
{


    [Serializable]
    public class ExeFsException : Exception
    {
        public ExeFsException() { }
        public ExeFsException(string message) : base(message) { }
        public ExeFsException(string message, Exception inner) : base(message, inner) { }
        protected ExeFsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public enum NsoTypeEnum
    {
        rtld = 0,
        main = 1,
        subsdk0 = 2,
        subsdk1 = 3,
        subsdk2 = 4,
        subsdk3 = 5,
        subsdk4 = 6,
        subsdk5 = 7,
        subsdk6 = 8,
        subsdk7 = 9,
        subsdk8 = 10,
        subsdk9 = 11,
        sdk = 12,
    }

    public class MemPage
    {
        public long Address;
        public long Size;
        public SegPerm Permission;

        public MemPage(long addr, long size, SegPerm perm)
        {
            Address = addr;
            Size = size;
            Permission = perm;
        }
    }

    public class ExeFS
    {
        public const string NPDM_PATH = "main.npdm";
        public const int MAX_NSO_COUNT = 13;

        private Nso[] Nsos = new Nso[MAX_NSO_COUNT];
        public byte[] Npdm { get; private set; }

        public ExeFS(string dirpath)
        {
            for (int i = 0; i < MAX_NSO_COUNT; i++)
            {
                string filepath = $@"{dirpath}\{(NsoTypeEnum)i}";

                if (File.Exists(filepath))
                    Nsos[i] = new Nso(filepath);
            }

            if (File.Exists(dirpath + "\\" + NPDM_PATH))
                Npdm = File.ReadAllBytes(dirpath + "\\" + NPDM_PATH);
        }
        public ExeFS(Nso main)
        {
            Nsos[(int)NsoTypeEnum.main] = main;
        }

        public Nso GetNso(NsoTypeEnum type)
        {
            return Nsos[(int)type];
        }
        public void SetNso(Nso nso, NsoTypeEnum type)
        {
            Nsos[(int)type] = nso;
        }
        public long GetNsoAddress(NsoTypeEnum type)
        {
            long addr = 0;

            for (int i = 0; i < (int)type; i++)
                if (Nsos[i] != null)
                    addr += Nsos[i].EndAddress;

            return (addr + 0xFFF) & ~0xFFF;
        }

        public byte[] ReadExeFs(long addr, int size)
        {
            long pos = addr;
            byte[] fullBuffer = new byte[size];

            while (pos < addr + size)
            {
                var page = GetAddressPage(pos);
                if ((page.Permission & SegPerm.R) == SegPerm.R)
                {
                    Nso curNso = GetAddressNso(pos);
                    long baseAddr = GetNsoAddress((NsoTypeEnum)Nsos.ToList().IndexOf(curNso));
                    long off = pos - page.Address;
                    int curSize = (int)Math.Min(page.Size - off, size);

                    byte[] sub = curNso.ReadNso((uint)(pos - baseAddr), curSize);
                    Buffer.BlockCopy(sub, 0, fullBuffer, (int)(pos-addr), sub.Length);
                    pos += curSize;
                }
                else pos += page.Size;
            }

            return fullBuffer;
        }
        public void WriteExeFs(long addr, byte[] data)
        {
            var page = GetAddressPage(addr);
            if ((page.Permission & SegPerm.R) == SegPerm.R)
            {
                Nso curNso = GetAddressNso(addr);

                var baseAddr = GetNsoAddress(GetAddressNsoType(addr).Value);
                curNso.WriteNso((uint)(addr - baseAddr), data);
            }
            /*
            long pos = addr;
            int dataOff = 0;

            while (pos < addr + data.Length)
            {
                var page = GetAddressPage(pos);
                if ((page.Permission & SegPerm.R) == SegPerm.R)
                {
                    Nso curNso = GetAddressNso(pos);
                    long baseAddr = GetNsoAddress((NsoType)Nsos.ToList().IndexOf(curNso));
                    long off = pos - page.Address;
                    int curSize = (int)Math.Min(page.Size - off, data.Length);

                    byte[] sub = curNso.ReadNso((uint)(pos - baseAddr), curSize);
                    byte[] sub = new byte[curSize];
                    curNso.WriteNso();
                    Buffer.BlockCopy(sub, 0, fullBuffer, (int)(pos - addr), sub.Length);
                    pos += curSize;
                }
                else pos += page.Size;
            }
            */
        }

        public List<MemPage> GetMemoryMap()
        {
            List<MemPage> Pages = new List<MemPage>();

            long addr = 0;
            for (int i = 0; i < MAX_NSO_COUNT; i++)
            {
                var nso = Nsos[i];
                if (nso == null) continue;

                long baseAddr = GetNsoAddress((NsoTypeEnum)i);
                long textStart = baseAddr + nso.StartAddress;
                long rodataStart = baseAddr + nso.RodataSegment.Address;
                long rwdataStart = baseAddr + nso.RwdataSegment.Address;
                long end = baseAddr + nso.EndAddress;

                if (textStart - addr > 0)
                    Pages.Add(new MemPage(addr, textStart - addr, SegPerm.None));

                Pages.Add(new MemPage(textStart, rodataStart - textStart, SegPerm.RX));
                Pages.Add(new MemPage(rodataStart, rwdataStart - rodataStart, SegPerm.R));
                Pages.Add(new MemPage(rwdataStart, end - rwdataStart, SegPerm.RW));

                addr = end;
            }

            return Pages;
        }
        public MemPage GetAddressPage(long addr)
        {
            var pages = GetMemoryMap();
            foreach (var page in pages)
            {
                if (addr >= page.Address && addr < page.Address + page.Size)
                    return page;
            }
            return null;
        }
        public Nso GetAddressNso(long addr)
        {
            for (int i = 0; i < Nsos.Length; i++)
            {
                long baseAddr = GetNsoAddress((NsoTypeEnum)i);
                if (Nsos[i] != null && addr >= baseAddr + Nsos[i].StartAddress && addr < baseAddr + Nsos[i].EndAddress)
                    return Nsos[i];
            }
            return null;
        }
        public NsoTypeEnum? GetAddressNsoType(long addr)
        {
            for (int i = 0; i < Nsos.Length; i++)
            {
                long baseAddr = GetNsoAddress((NsoTypeEnum)i);
                if (Nsos[i] != null && addr >= baseAddr + Nsos[i].StartAddress && addr < baseAddr + Nsos[i].EndAddress)
                    return (NsoTypeEnum)i;
            }
            throw null;
        }

        public void ExportAsPfs0(string outfile)
        {
            string dir = Utils.GetTempDir();

            ExportAsDir(dir);
            Pfs0.BuildPfs0(dir, outfile);

            Directory.Delete(dir, true);
        }
        public void ExportAsDir(string outdir)
        {
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);

            for (int i = 0; i < Nsos.Length; i++)
                Nsos[i]?.BuildNso($@"{outdir}\{(NsoTypeEnum)i}");

            if (Npdm != null)
                File.WriteAllBytes(outdir + "\\" + NPDM_PATH, Npdm);
        }

        public static ExeFS FromFile(string filepath)
        {
            string outdir = Utils.GetTempDir();

            Pfs0.ExtractFiles(filepath, outdir);
            ExeFS exefs = new ExeFS(outdir);

            Directory.Delete(outdir, true);

            return exefs;
        }
    }
}
