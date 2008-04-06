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
        private static string basename;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
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
                basename = sp.generateName(filename);

                Console.WriteLine("Output: " + basename + "\n");
                Directory.CreateDirectory(basename);

                sp.saveImages();

                string bookname = sp.getBookName() + ".opf";
                sp.translate(opfxsl, bookname);
                sp.translate(bodyxsl, "index.html");

                System.Diagnostics.Process process = new System.Diagnostics.Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.FileName = "mobigen.exe";
                process.StartInfo.Arguments = "-s0 -c2 -unicode -nomin " + basename + "\\" + bookname;
                process.Start();

                string str;
                while ((str = process.StandardOutput.ReadLine()) != null)
                {
                    Console.WriteLine(str);
                }
            }
            catch (XmlException e)
            {
                Console.WriteLine("error occured: " + e.Message);
            }

        }

        string generateName(string file)
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
            }
            if (name.Length == 0 || File.Exists(name) || Directory.Exists(name))
            {
                int cont = 1;
                while (File.Exists(name + cont.ToString()) || Directory.Exists(name + cont.ToString()))
                    ++cont;
                name += cont.ToString();
            }
            return name;
        }

        void saveImages()
        {
            XmlDocument dd = new XmlDocument();
            dd.Load(filename);
            XmlNode bin = dd["FictionBook"]["binary"];
            while(bin != null)
            {
                FileStream fs = new FileStream(basename + "\\" + bin.Attributes["id"].InnerText, FileMode.Create);
                BinaryWriter w = new BinaryWriter(fs);
                w.Write(Convert.FromBase64String(bin.InnerText));
                w.Close();
                fs.Close();
                bin = bin.NextSibling;
            }
        }

        string getBookName()
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
                        return generateName(val);
                }
            }
            return basename;
        }

        void translate(string xsl, string name)
        {
            XmlTextReader reader = new XmlTextReader(filename);

            XslCompiledTransform xslt = new XslCompiledTransform();
            xslt.Load(xsl);

            XmlTextWriter writer = new XmlTextWriter(basename + "\\" + name, null);
            writer.Formatting = Formatting.Indented;
            
            xslt.Transform(reader, null, writer, null);
        }
    }
}
