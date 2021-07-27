using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using static System.Console;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace print_demo_info_immediate
{
    class SettingsHandler
    {
        public bool VerifyPaths()
        {
            if (!File.Exists(Program.ListDemoPath))
            {
                WriteLine("Listdemo path invalid! Aborting!!");
                throw new FileNotFoundException();
            }
            return true;
        }

        public void ReadSettings()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load("config.xml");

            Program.ListDemoPath = xml.DocumentElement.SelectSingleNode("/config/listdemo").InnerText ?? "";
            Program.GameExe = xml.DocumentElement.SelectSingleNode("/config/gameexe").InnerText ?? "";
            VerifyPaths();
            WriteLine("Successfully loaded settings.");
        }

        public void FirstTimeSettings()
        {
            WriteLine("Config not found! Starting first time setup...");
            WriteLine("Please enter without surrounding quotes the following info:");

            WriteLine("Listdemo / Demo Parser path: ");
            Program.ListDemoPath = ReadLine();

            WriteLine("Game EXE name: ");
            Program.GameExe = ReadLine();

            VerifyPaths();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = ("    ");
            settings.CloseOutput = true;
            using (XmlWriter xml = XmlWriter.Create("config.xml", settings))
            {
                xml.WriteStartElement("config");
                xml.WriteElementString("listdemo", Program.ListDemoPath);
                xml.WriteElementString("gameexe", Program.GameExe);
                xml.WriteEndElement();
                xml.Flush();
            }
        }
    }
}
