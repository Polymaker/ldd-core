﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LDD.Core.Palettes
{
    public abstract class PaletteItem
    {
        [XmlAttribute("designID")]
        public int DesignID { get; set; }

        [XmlAttribute("quantity")]
        public int Quantity { get; set; }

        [XmlAttribute("ItemNos")]
        public string ElementID { get; set; }

        [XmlIgnore]
        public abstract bool HasDecorations { get; }

        public PaletteItem()
        {
            DesignID = 0;
            Quantity = 0;
            ElementID = string.Empty;
        }

        public PaletteItem(int designID, string elementID)
        {
            DesignID = designID;
            Quantity = 0;
            ElementID = elementID;
        }

        public PaletteItem(int designID, string elementID, int quantity)
        {
            DesignID = designID;
            Quantity = quantity;
            ElementID = elementID;
        }
    }
}
