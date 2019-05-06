using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NxUtils
{
    [Serializable]
    public class Pfs0Exception : Exception
    {
        public Pfs0Exception() { }
        public Pfs0Exception(string message) : base(message) { }
        public Pfs0Exception(string message, Exception inner) : base(message, inner) { }
        protected Pfs0Exception(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public static class Pfs0
    {
        private struct FileEntry
        {
            public byte[] Data;
            public string Name;

            public FileEntry(BinaryReader br, long dataTableOff, long strTableOff, long strTableLen)
            {
                long dataOff = br.ReadInt64();
                long dataSize = br.ReadInt64();
                int strOff = br.ReadInt32();
                br.ReadInt32(); //padding

                long tempPos = br.BaseStream.Position;
                br.BaseStream.Position = strTableOff + strOff;

                List<byte> strBuff = new List<byte>();
                do strBuff.Add(br.ReadByte());
                while (strBuff.Last() != 0);
                
                Name = Encoding.UTF8.GetString(strBuff.ToArray()).Replace("\0", "");

                br.BaseStream.Position = dataTableOff + dataOff;
                Data = br.ReadBytes((int)dataSize); //pwease don't beat me it's not supposed to parse big files :(

                br.BaseStream.Position = tempPos;
            }
        }

        private const string HEADER_MAGIC = "PFS0";
        private const int ENTRY_SIZE = 0x18;
        

        public static void ExtractFiles(string infile, string outdir)
        {
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);

            using (var fs = File.OpenRead(infile))
            {
                BinaryReader br = new BinaryReader(fs);
                ExtractFiles(br, outdir);
            }
        }
        private static void ExtractFiles(BinaryReader br, string dirPath)
        {
            List<FileEntry> FileEntries = new List<FileEntry>();

            string magic = Encoding.ASCII.GetString(br.ReadBytes(4));
            if (magic != HEADER_MAGIC)
                throw new Pfs0Exception("Invalid magic");

            int fileCount = br.ReadInt32();
            int strTableLenght = br.ReadInt32();
            br.ReadInt32();//padding

            long fileTableOffset = br.BaseStream.Position;
            long strTableOffset = fileTableOffset + fileCount * ENTRY_SIZE;
            long fileDataOff = strTableOffset + strTableLenght;

            for (int i = 0; i < fileCount; i++)
            {
                FileEntries.Add(new FileEntry(br, fileDataOff, strTableOffset, strTableLenght));
                File.WriteAllBytes($@"{dirPath}\{FileEntries.Last().Name}", FileEntries.Last().Data);
            }
            
        }

        public static void BuildPfs0(string indir, string outfile)
        {
            if (!Directory.Exists(indir))
                throw new Pfs0Exception("Invalid path");

            using (var fs = File.Create(outfile))
            {
                BinaryWriter bw = new BinaryWriter(fs);
                BuildPfs0(bw, indir);
            }
        }
        private static void BuildPfs0(BinaryWriter bw, string indir)
        {
            DirectoryInfo dir = new DirectoryInfo(indir);
            var files = dir.GetFiles();

            List<int> strTableOffs = new List<int>();
            int strLastOff = 0;
            for (int i = 0; i < files.Length; i++)
            {
                strTableOffs.Add(strLastOff);
                strLastOff += Encoding.UTF8.GetByteCount(files[i].Name) + 1;
            }
            strLastOff = (strLastOff + 0x1F)& ~0x1F;

            //header
            bw.Write(Encoding.ASCII.GetBytes(HEADER_MAGIC));
            bw.Write(files.Length);
            bw.Write(strLastOff);
            bw.Write(0);

            long lastFileOff = 0;

            //file table
            for (int i = 0; i < files.Length; i++)
            {
                bw.Write(lastFileOff);
                bw.Write(files[i].Length);
                bw.Write(strTableOffs[i]);
                bw.Write(0);

                lastFileOff += files[i].Length;
            }

            //str table
            foreach (var file in files)
                bw.Write(Encoding.UTF8.GetBytes(file.Name + '\0'));

            bw.Write(new byte[((bw.BaseStream.Position + 0x1F) & ~0x1F) - bw.BaseStream.Position]); //0x20 aligned according to switch-tools/build_pfs0

            //data
            foreach (var file in files)
                bw.Write(File.ReadAllBytes(file.FullName));
        }
    }
}
