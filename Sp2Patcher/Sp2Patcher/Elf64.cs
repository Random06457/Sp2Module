using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NxUtils
{
    public struct Elf64_Dyn
    {
        public DtTagEnum d_tag;
        public ulong d_un;
        public Elf64_Dyn(BinaryReader br)
        {
            d_tag = (DtTagEnum)br.ReadUInt64();
            d_un = br.ReadUInt64();
        }
    };

    public struct Elf64_Sym
    {
        public const int ELF64_SYM_SIZE = 0x18;
        public uint st_name;
        public byte st_info;
        public byte st_other;
        public ushort st_shndx;
        public ulong st_value;
        public ulong st_size;

        public Elf64_Sym(Nso nso, uint addr)
        {
            byte[] buffer = nso.ReadNso(addr, ELF64_SYM_SIZE);
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                BinaryReader br = new BinaryReader(ms);
                st_name = br.ReadUInt32();
                st_info = br.ReadByte();
                st_other = br.ReadByte();
                st_shndx = br.ReadUInt16();
                st_value = br.ReadUInt64();
                st_size = br.ReadUInt64();
            }
        }
        public Elf64_Sym(BinaryReader br)
        {
            st_name = br.ReadUInt32();
            st_info = br.ReadByte();
            st_other = br.ReadByte();
            st_shndx = br.ReadUInt16();
            st_value = br.ReadUInt64();
            st_size = br.ReadUInt64();
        }
    };

    public enum DtTagEnum : ulong
    {
        DT_NULL = 0,
        DT_NEEDED = 1,
        DT_PLTRELSZ = 2,
        DT_PLTGOT = 3,
        DT_HASH = 4,
        DT_STRTAB = 5,
        DT_SYMTAB = 6,
        DT_RELA = 7,
        DT_RELASZ = 8,
        DT_RELAENT = 9,
        DT_STRSZ = 10,
        DT_SYMENT = 11,
        DT_INIT = 12,
        DT_FINI = 13,
        DT_SONAME = 14,
        DT_RPATH = 15,
        DT_SYMBOLIC = 16,
        DT_REL = 17,
        DT_RELSZ = 18,
        DT_RELENT = 19,
        DT_PLTREL = 20,
        DT_DEBUG = 21,
        DT_TEXTREL = 22,
        DT_JMPREL = 23,
        DT_BIND_NOW = 24,
        DT_INIT_ARRAY = 25,
        DT_FINI_ARRAY = 26,
        DT_INIT_ARRAYSZ = 27,
        DT_FINI_ARRAYSZ = 28,
        DT_RUNPATH = 29,
        DT_FLAGS = 30,
        DT_ENCODING = 32,
        DT_PREINIT_ARRAY = 32,
        DT_PREINIT_ARRAYSZ = 33,
        DT_NUM = 34,
        DT_GNU_HASH = 0x6ffffef5,
    }

    public enum SymbolBindingEnum
    {
        STB_LOCAL	= 0,
        STB_GLOBAL	= 1,
        STB_WEAK	= 2,
        STB_LOOS	= 10,
        STB_HIOS	= 12,
        STB_LOPROC	= 13,
        STB_HIPROC	= 15,
    }
    public enum SymbolTypeEnum
    {
        STT_NOTYPE	= 0,
        STT_OBJECT	= 1,
        STT_FUNC	= 2,
        STT_SECTION	= 3,
        STT_FILE	= 4,
        STT_COMMON	= 5,
        STT_TLS	    = 6,
        STT_LOOS	= 10,
        STT_HIOS	= 12,
        STT_LOPROC	= 13,
        STT_HIPROC	= 15,
    }

}
