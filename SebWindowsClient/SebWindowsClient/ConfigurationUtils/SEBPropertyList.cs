using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SebWindowsClient.ConfigurationUtils
{
    public class SEBPropertyList
    {
        [XmlElement("MovieName")]
        public string Title
        { get; set; }

        [XmlElement("MovieRating")]
        public float Rating
        { get; set; }

        [XmlElement("MovieReleaseDate")]
        public DateTime ReleaseDate
        { get; set; }
    }
}
