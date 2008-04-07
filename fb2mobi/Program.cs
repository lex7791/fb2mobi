using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

namespace fb2mobi
{
    class SXParser
    {
        private static string filename;
        private static string opfxsl;
        private static string bodyxsl;
        private static string basepath;

        [STAThread]
        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]));
            if(args.Length == 0){
                Console.WriteLine("Usage: fb2mobi <file.fb2> [,<body.xsl> [,<opf.xsl>]]\n");
                return;
            }
            else
                filename = args[0];
            if(!File.Exists(filename)){
                Console.WriteLine("File: \"" + filename + "\" not found\n");
                return;
            }
            bodyxsl = "FB2_2_xhtml.xsl";
            if (args.Length > 1)
                bodyxsl = args[1];
            
            opfxsl = "FB2_2_opf.xsl";
            if (args.Length > 2)
                opfxsl = args[2];

            SXParser sp = new SXParser();

            string rootpath = Path.GetDirectoryName(Path.GetFullPath(filename));
            string basename = sp.getBookName(sp.transliteName(Path.GetFileName(filename)));
            string bookname;
            try
            {
                bookname = sp.checkFile(rootpath, basename);
                Directory.CreateDirectory(rootpath + "\\" + bookname);
            }
            catch (Exception )
            {
                rootpath = Path.GetTempPath();
                bookname = sp.checkFile(rootpath, basename);
                Directory.CreateDirectory(rootpath + "\\" + bookname);
            }
            basepath = rootpath + "\\" + bookname + "\\";

            try
            {
                sp.saveImages();
                sp.translate(opfxsl, bookname+ ".opf");
                sp.translate(bodyxsl, "index.html");
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
            process.StartInfo.FileName = "mobigen.exe";
            process.StartInfo.Arguments = "-s0 -c2 -unicode -nomin \"" + basepath + bookname + ".opf\"";
            process.Start();

            string str;
            while ((str = process.StandardOutput.ReadLine()) != null)
                if(str.Length > 0)
                    Console.WriteLine(str);

            bookname += ".mobi";
            if (File.Exists(basepath + bookname))
            {
                File.Move(basepath + bookname, rootpath + "\\" + bookname);
                Console.WriteLine("Output: " + basepath);
            }

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
                  , 'ô', 'õ', 'ö', '÷', 'ø', 'ù', 'ü', 'ú', 'ý', 'þ', 'ÿ'
                  , '\\',':', '?'};
            char[] lat = new char[] {
                    'a', 'b', 'v', 'g', 'd', 'e', 'j', 'z', 'i', 'y'
                  , 'k', 'l', 'm', 'n', 'o', 'p', 'r', 's', 't', 'u'
                  , 'f', 'h', 'c', 'h', 's', 's', 'x', 'x', 'e', 'y', 'a'
                  , '_', '_', '_'};
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

        string getBookName(string defname)
        {
            XmlDocument dd = new XmlDocument();
            dd.Load(filename);
            XmlNode root = dd["FictionBook"]["description"], data;
            if ((data = root["title-info"])!= null)
            {
                data = data["book-title"];
                if (data != null)
                {
                    string val = data.InnerText;
                    if (val.Length > 0)
                        return transliteName(val);
                }
            }
            return defname;
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

        void translate(string xsl, string name)
        {
            XmlTextReader reader = new XmlTextReader(filename);

            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(xsl);

            XmlTextWriter writer = new XmlTextWriter(basepath + name, null);
            writer.Formatting = Formatting.Indented;
            
            xslt.Transform(reader, null, writer, null);
        }
    }
}
