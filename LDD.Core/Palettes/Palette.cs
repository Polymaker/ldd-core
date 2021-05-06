﻿using LDD.Core.Data;
using LDD.Core.Files;
using LDD.Common.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LDD.Core.Palettes
{
    [XmlRoot("PAXML")]
    public partial class Palette
    {
        public const string EXTENSION = "PAXML";

        public List<PaletteItem> Items { get; private set; }

        public IEnumerable<Brick> Bricks => Items.OfType<Brick>();

        public IEnumerable<Assembly> Assemblies => Items.OfType<Assembly>();

        public VersionInfo FileVersion { get; set; }

        public string Name { get; set; }

        public Palette()
        {
            Items = new List<PaletteItem>();
            FileVersion = new VersionInfo(1, 0);
        }

        public void Save(string filename)
        {
            using (var fs = File.Open(filename, FileMode.Create))
                Save(fs);
        }

        public void Save(Stream stream)
        {
            var doc = new XDocument(
                new XElement(EXTENSION,
                    new XAttribute("versionMajor", FileVersion.Major),
                    new XAttribute("versionMinor", FileVersion.Minor)
                    )
                )
            {
                Declaration = new XDeclaration("1.0", "UTF-8", "no")
            };
            var bagElem = doc.Root.AddElement("Bag");
            foreach (var item in Items)
            {
                if (item is Brick brick)
                    bagElem.Add(XmlHelper.DefaultSerialize(brick));
                else if (item is Assembly assembly)
                    bagElem.Add(XmlHelper.DefaultSerialize(assembly));
            }
            doc.Save(stream);
        }

        public static Palette Load(string filename)
        {
            using (var fs = File.OpenRead(filename))
                return Load(fs);
        }

        public static Palette FromLifEntry(LifFile.FileEntry entry)
        {
            var result = Load(entry.GetStream());
            if (result != null)
                result.Name = Path.GetFileNameWithoutExtension(entry.Name);
            return result;
        }

        public static Palette Load(Stream stream)
        {
            var doc = XDocument.Load(stream);
            doc.Root.TryGetIntAttribute("versionMajor", out int major);
            doc.Root.TryGetIntAttribute("versionMinor", out int minor);

            var palette = new Palette()
            {
                FileVersion = new VersionInfo(major, minor)
            };

            if (stream is FileStream fs)
                palette.Name = Path.GetFileNameWithoutExtension(fs.Name);

            foreach (var itemElem in doc.Root.Element("Bag").Elements())
            {
                if (itemElem.Name.LocalName == "Brick")
                    palette.Items.Add(XmlHelper.DefaultDeserialize<Brick>(itemElem));
                else if (itemElem.Name.LocalName == "Assembly")
                    palette.Items.Add(XmlHelper.DefaultDeserialize<Assembly>(itemElem));
            }

            return palette;
        }
    }
}
