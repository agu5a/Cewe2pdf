using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Cewe2pdf {

    class Program {
        public static readonly string version = "v0.4.1-alpha1";
        public static string mcfxPath = "";
        public static string pdfPath = "";

        private const string CONFIG_PATH = "config.txt";

        static void Main(string[] args) {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, assembly, searchPath) => {
                if (libraryName == "libwebp_x64.dll" || libraryName == "libwebp_x86.dll")
                    return NativeLibrary.Load("libwebp", assembly, searchPath);
                if (libraryName == "kernel32.dll")
                    return NativeLibrary.Load("libc", assembly, searchPath);
                return IntPtr.Zero;
            });

#if DEBUG || _DEBUG
            Log.level = Log.Level.Info;
            Log.Message("Cewe2pdf " + version + " [Debug]");
#else
            Log.level = Log.Level.Error;
            Log.Message("Cewe2pdf " + version + " [Release]");
#endif

            List<string> cmdoptions;

            if (!CmdArgParser.parse(args, out cmdoptions)) return;

            // check for valid input file
            if (String.IsNullOrWhiteSpace(mcfxPath)) { Log.Error("No input.mcfx file specified."); return; }
            if (!System.IO.File.Exists(mcfxPath)) { Log.Error("'" + mcfxPath + "' does not exist.'"); return; }

            // allow only input file as argument
            if (String.IsNullOrWhiteSpace(pdfPath)) pdfPath = mcfxPath.Replace(".mcfx", "-converted.pdf");

            // set config settings
            Config.setMissingFromOptions(cmdoptions.ToArray());
            Config.setMissingFromFile(CONFIG_PATH);
            Config.setMissingToDefaults();

            Log.Info("Using " + Config.print());

            if (String.IsNullOrWhiteSpace(Config.ProgramPath) || !System.IO.Directory.Exists(Config.ProgramPath + "//Resources")) {
                Log.Error("Cewe Installation directory not found. Please specify installation folder in config.txt next to Cewe2pdf.exe. Check (https://github.com/stfnk/Cewe2pdf#troubleshooting) for more information.");
                return;
            }

            // measure runtime to calculate remaining time
            System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();

            // initialize with given files
            Mcfx mcfx = new Mcfx(mcfxPath);
            McfParser parser = new McfParser(mcfx.getMcfFile());
            PdfWriter writer = new PdfWriter(pdfPath, mcfx);

            if (Config.ToPage > 0)
                Log.Message("Converting " + Config.ToPage.ToString() + " pages.");

            Log.Message("Starting conversion. This may take several minutes.");

            // keep track to calculate progress to report to user
            int count = 0;
            int pageCount = Config.ToPage > 0 ? Config.ToPage : parser.pageCount();

            // iterate through all pages
            while (true) {
                Page next = parser.nextPage();
                if (next == null) break; // reached end of book
                Log.Message("[" + (Math.Min(count / (float)pageCount * 100, 100.0f)).ToString("F1") + "%]\tprocessing Page " + Math.Min(count + 1, pageCount) + "/" + pageCount + "...", false);
                long lastTime = timer.ElapsedMilliseconds;
                writer.writePage(next);
                float pageTime = (timer.ElapsedMilliseconds - lastTime) / 1000.0f;
                count++;
                if (count == Config.ToPage) break;
                Log.Message("\tremaining: ~" + MathF.Ceiling(timer.ElapsedMilliseconds / count * (pageCount - count) / 1000.0f / 60.0f).ToString() + " minutes.");
            }

            // close files
            Log.Message("Writing '" + pdfPath + "'.");
            writer.close();
            Log.Message("Conversion finished after " + timer.ElapsedMilliseconds / 1000.0f + " seconds.");
            Log.writeLogFile();
        }
    }
}
