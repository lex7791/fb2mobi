using System;
using System.IO;

using CommandLine.Utility;

namespace fb2mobi
{
    class FB2mobiMain
    {
        private static string kindlegen = "kindlegen.exe";
        private static string opfxsl    = "FB2_2_opf.xsl";
        private static string bodyxsl   = "FB2_2_xhtml.xsl";

        static void print_usage()
        {
            Console.WriteLine("Usage: fb2mobi <file.fb2> [<output.mobi>] [{-,/,--}param]");
            Console.WriteLine("  -nc \t No compress output file. Increase speed and size :-)");
            Console.WriteLine("  -cl \t Clean output dir after convert.");
            Worker.print_usage();
        }

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("FB2mobi v 2.0.0b Copyright (c) 2008-2011 Rakunov Alexander 2011-02-13");
            Console.WriteLine("Project home: http://code.google.com/p/fb2mobi/\n");

            string PathToExecute = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            if(PathToExecute.Trim().Length > 0)
                Directory.SetCurrentDirectory(PathToExecute);
            
            if(!File.Exists(kindlegen)){
                Console.WriteLine("File: \"" + kindlegen + "\" not found\n");
                return;
            }

            if(args.Length == 0){
                print_usage();
                return;
            }
            Arguments CommandLine = new Arguments(args);

            if (CommandLine["?"] == "true" || CommandLine["help"] == "true" || CommandLine["h"] == "true")
            {
                print_usage();
                return;
            }

            string filename = CommandLine[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine("File: \"" + filename + "\" not found\n");
                print_usage();
                return;
            }


            // PREPARE DATA


            Worker sp = new Worker(CommandLine);

            if (sp.error())
            {
                print_usage();
                return;
            }
            
            try
            {
                sp.saveImages();
                sp.transform(bodyxsl, "index.html");
                sp.transform(opfxsl, sp.getBookName(".opf"));
            }
            catch (Exception e)
            {
                Console.WriteLine("error occured: " + e.Message);
                return;
            }


            // RUN KINDLEGEN


            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = kindlegen;

            string KindleGenArguments = "-unicode -nomin";
            KindleGenArguments += CommandLine["nc"] == "true" ? " -c0" : " -c2";
            KindleGenArguments += " \"" + sp.getWorkDir() + sp.getBookName(".opf") + "\"";

            process.StartInfo.Arguments = KindleGenArguments;

            process.Start();

            string str;
            while ((str = process.StandardOutput.ReadLine()) != null)
                if (str.Length > 0)
                    Console.WriteLine(str);

            process.Close();

            // CLEAN AND PUBLISH
            Console.WriteLine("");

            string bookname = sp.getBookName(".mobi");
            if (File.Exists(sp.getWorkDir() + bookname))
            {
                File.Move(sp.getWorkDir() + bookname, sp.getOutputDir() + bookname);

                if (CommandLine["cl"] == "true")
                {
                    try
                    {
                        Directory.Delete(sp.getWorkDir(), true);
                    }
                    catch (Exception) { }
                }
                else
                    Console.WriteLine("Output: " + sp.getWorkDir());

                Console.WriteLine("Book: " + sp.getOutputDir() + bookname);

            }
            else
            {
                Console.WriteLine("The output file is missing.");
                try
                {
                    Directory.Delete(sp.getWorkDir(), true);
                }
                catch (Exception) { }
            }
        }
    }
}
