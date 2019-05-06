using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NxUtils;

namespace Sp2Patcher
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 5)
            {
                Console.WriteLine($"Invalid Arg Nb ({args.Length})");
                ShowInfo();
                return;
            }

            string indir = args[0];
            string subsdkPath = args[1];
            string outdir = args[2];
            string lstFile = args[3];
            string configFile = args[4];

            Console.WriteLine($"Current dir : {Environment.CurrentDirectory}");

            if (!Directory.Exists(indir) || !File.Exists(subsdkPath) || !Directory.Exists(outdir) || !File.Exists(lstFile) || !File.Exists(configFile))
            {
                Console.WriteLine($"Invalid Args :");
                foreach (var arg in args)
                    Console.WriteLine(arg);

                ShowInfo();
                return;
            }


            ExeFS exefs = new ExeFS(indir);
            Nso custom = new Nso(subsdkPath);

            //allocate 0x100 bytes at the end of .bss for the module_object
            AllocModObj(custom);
            AddSdk(exefs, custom);

            ApplyPatches(exefs, lstFile, configFile);

            Console.WriteLine("Exporting...");
            exefs.ExportAsDir(outdir);
            Console.WriteLine("Done!");
        }

        static void ApplyPatches(ExeFS exefs, string lstFile, string configfile)
        {
            var patches = PatchConfig.GetHooks(configfile);
            var symbols = ListingFile.GetSymbols(lstFile);

            foreach (var patch in patches)
            {
                Console.WriteLine($"{patch}");
                patch.Apply(exefs, symbols);
            }
            return;

            /* we don't need this anymore

            Console.WriteLine("Patching NPDM...");
            //patch ndpm to be able to access sd card
            using (var ms = new MemoryStream(exefs.Npdm))
            {
                BinaryReader br = new BinaryReader(ms);
                BinaryWriter bw = new BinaryWriter(ms);
                br.BaseStream.Position = 0x70;
                var aci0Off = br.ReadUInt32(); //ACI0 offset
                br.BaseStream.Position = 0x78;
                var acidOff = br.ReadUInt32(); //ACID offset

                br.BaseStream.Position = aci0Off+0x20; //FS Access Header offset
                var fsAccessHeaderOff = aci0Off + br.ReadUInt32();
                br.BaseStream.Position = acidOff + 0x220; //FS Access Control offset
                var fsAccessControlOff = acidOff + br.ReadUInt32();

                bw.BaseStream.Position = fsAccessHeaderOff + 4;
                bw.Write(0x4000000000a92131); //full fs access
                bw.BaseStream.Position = fsAccessControlOff + 4;
                bw.Write(0x4000000000a92131); //full fs access
            }
            */

        }


        static void AllocModObj(Nso nso)
        {
            nso.RwdataSegment.BssOrAlign += 0x100;
            ModHeader.CreateModHeader(nso, nso.GetModAddr(), nso.ModSection.DynamicAddr, nso.ModSection.EhStartAddr, nso.ModSection.EhEndAddr, nso.ModSection.BssEndAddr);

        }
        static void AddSdk(ExeFS exefs, Nso custom)
        {
            Nso sdk = exefs.GetNso(NsoTypeEnum.sdk);
            exefs.SetNso(sdk, NsoTypeEnum.subsdk0);
            exefs.SetNso(custom, NsoTypeEnum.sdk);
        }



        static void ShowInfo()
        {
            Console.WriteLine("Usage : Sp2Patcher.exe [indir] [subsk] [outdir] [.lst file] [config file]");
        }
    }
}
