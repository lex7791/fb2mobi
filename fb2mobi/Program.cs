using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

using System.Diagnostics;
using System.ComponentModel;
using System.Threading;

using CommandLine.Utility;

namespace fb2mobi
{
    class SXParser
    {
        private static string kindlegen = "kindlegen.exe";
        private static string opfxsl    = "FB2_2_opf.xsl";
        private static string bodyxsl   = "FB2_2_xhtml.xsl";

        private int ErorrLevel;

        Arguments CommandLine;

        public string basepath;
        public string rootpath;
        public string filename;
        public string bookname;

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("FB2mobi v 2.0.0 Copyright (c) 2008-2011 Rakunov Alexander 2011-01-16");
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

            SXParser sp = new SXParser(CommandLine);

            if (sp.Error())
            {
                return;
            }
            
            try
            {
                sp.saveImages();
                sp.transform(bodyxsl, "index.html");
                sp.transform(opfxsl, sp.bookname + ".opf");
            }
            catch (XmlException e)
            {
                Console.WriteLine("error occured: " + e.Message);
                return;
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = kindlegen;

            string KindleGenArguments = "-unicode -nomin";
            if (CommandLine["nc"] == "true")
                KindleGenArguments += " -c0";
            else
                KindleGenArguments += " -c2";
            KindleGenArguments += " \"" + sp.basepath + sp.bookname + ".opf\"";
            process.StartInfo.Arguments = KindleGenArguments;

            process.Start();

            string str;
            while ((str = process.StandardOutput.ReadLine()) != null)
                if (str.Length > 0)
                    Console.WriteLine(str);

            string bookname = sp.bookname + ".mobi";
            if (File.Exists(sp.basepath + bookname))
            {
                File.Move(sp.basepath + bookname, sp.rootpath + "\\" + bookname);
                Console.WriteLine("Output: " + sp.basepath + "\n");
                Console.WriteLine("Book: " + sp.rootpath + "\\" + bookname);
            }
        }

        public SXParser(Arguments args)
        {
            CommandLine = args;

            ErorrLevel = 0;

            filename = CommandLine[0];
            rootpath = "";
            basepath = "";
            bookname = "";

            string basename = "";

            if (!File.Exists(filename))
            {
                Console.WriteLine("File: \"" + filename + "\" not found\n");
                ErorrLevel = 1;
                return;
            }

            string wd = CommandLine["w"];
            if (wd.Length != 0)
                if (Directory.Exists(wd))
                    rootpath = wd;

            string of = CommandLine["o"];
            if (of.Length != 0){
                try
                {
                    basename = Path.GetFileNameWithoutExtension(of);

                    wd = Path.GetDirectoryName(of);
                    if (wd.Length != 0)
                        if (Directory.Exists(wd))
                            rootpath = wd;
                }
                catch (Exception)
                {
                    basename = "";
                }
            }

            if(rootpath.Length == 0)
                rootpath = Path.GetDirectoryName(Path.GetFullPath(filename));
            
            if(basename.Length == 0){
                basename = Path.GetFileNameWithoutExtension(filename);

                if(CommandLine["kf"] != "true"){
                    // get book name
                    XmlDocument dd = new XmlDocument();
                    dd.Load(filename);
                    XmlNode root = dd["FictionBook"]["description"], data;
                    if ((data = root["title-info"])!= null){
                        data = data["book-title"];
                        if (data != null){
                            string val = data.InnerText;
                            if (val.Length > 0)
                                basename = val;
                        }
                    }
                }
            }
            
            if(CommandLine["nt"] != "true")
                basename = transliteName(basename);
            

            try
            {
                bookname = checkFile(rootpath, basename);
                Directory.CreateDirectory(rootpath + "\\" + bookname);
            }
            catch (Exception )
            {
                rootpath = Path.GetTempPath();
                bookname = checkFile(rootpath, basename);
                Directory.CreateDirectory(rootpath + "\\" + bookname);
            }

            basepath = rootpath + "\\" + bookname + "\\";
        
        }

        public bool Error()
        {
            return ErorrLevel == 0 ? false : true;
        }

        static void print_usage()
        {
            Console.WriteLine("Usage: fb2mobi <file.fb2> {-,/,--}param[{ ,=,:}((\",')value(\",'))]");
            Console.WriteLine("\t -nc \t No compress output file. Increase speed and size :-)");
            Console.WriteLine("\t -kf \t Keep source file name, default output file name is a book name.");
            Console.WriteLine("\t -nt \t No translite output file. Save the file name unchanged");
            Console.WriteLine("\t -w \"WorkDir\" \t Set output work dir");
            Console.WriteLine("\t -o \"FileName\" \t Set output file name");
        }

        string checkFile(string dir, string file)
        {
            if (file.Length == 0)
                file = "fb2mobi";

            string name = dir + "\\" + file;
            if (!File.Exists(name) && !Directory.Exists(name))
                return file;

            int cont = 1;
            while (File.Exists(name + cont.ToString()) || Directory.Exists(name + cont.ToString()))
                ++cont;
            
            return file + cont.ToString();
        }

        string transliteName(string file)
        {
            char[] rus = new char[] {
                    'à', 'á', 'â', 'ã', 'ä', 'å', 'æ', 'ç', 'è', 'é'
                  , 'ê', 'ë', 'ì', 'í', 'î', 'ï', 'ð', 'ñ', 'ò', 'ó'
                  , 'ô', 'õ', 'ö', '÷', 'ø', 'ù', 'û', 'ý', 'þ', 'ÿ'
                  , '\\',':', '/', '?', '*', ' '};
            char[] lat = new char[] {
                    'a', 'b', 'v', 'g', 'd', 'e', 'j', 'z', 'i', 'y'
                  , 'k', 'l', 'm', 'n', 'o', 'p', 'r', 's', 't', 'u'
                  , 'f', 'h', 'c', 'h', 's', 's', 'i', 'e', 'u', 'a'
                  , '_', '_', '_', '_', '_', '_'};
            string name = "";
            for (int idx = 0; idx < file.Length; ++idx ){
                char ch = char.ToLower(file[idx]);
                if (ch == '.')
                    break;
                int i = Array.FindIndex<char>(rus, delegate(char c) { return c == ch; });
                if (i >= 0)
                    name += lat[i];
                else if(ch >= '0' && ch <= 127)
                    name += ch;
                if (name.Length > 31)
                    break;
            }
            return name;
        }

        void saveImages()
        {
            XmlDocument dd = new XmlDocument();
            dd.Load(filename);
            XmlNode bin = dd["FictionBook"]["binary"];
            while (bin != null)
            {
                FileStream fs = new FileStream(basepath + bin.Attributes["id"].InnerText, FileMode.Create);
                BinaryWriter w = new BinaryWriter(fs);
                w.Write(Convert.FromBase64String(bin.InnerText));
                w.Close();
                fs.Close();
                bin = bin.NextSibling;
            }
        }

        void transform(string xsl, string name)
        {
            XmlTextReader reader = new XmlTextReader(filename);

            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(xsl);

            XmlTextWriter writer = new XmlTextWriter(basepath + name, null);
            writer.Formatting = Formatting.Indented;
            
            xslt.Transform(reader, null, writer, null);
            
            writer.Close();
        }
    }
}
