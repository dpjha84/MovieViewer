using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MovieViewerWPF
{
    [Serializable()]
    public class Movie
    {
        [System.Xml.Serialization.XmlElement("Id")]
        public string Id { get; set; }

        [System.Xml.Serialization.XmlElement("LocalName")]
        public string LocalName { get; set; }

        [System.Xml.Serialization.XmlElement("Name")]
        public string Name { get; set; }

        [System.Xml.Serialization.XmlElement("Rating")]
        public string Rating { get; set; }

        [System.Xml.Serialization.XmlElement("Genre")]
        public string Genre { get; set; }

        [System.Xml.Serialization.XmlElement("Size")]
        public string Size { get; set; }

        [System.Xml.Serialization.XmlElement("FullLocalPath")]
        public string FullLocalPath { get; set; }

        [System.Xml.Serialization.XmlElement("ImageThumbnail")]
        public string ImageThumbnail { get; set; }

        [System.Xml.Serialization.XmlElement("LocalImageThumbnail")]
        public string LocalImageThumbnail { get; set; }

        [System.Xml.Serialization.XmlElement("Year")]
        public string Year { get; set; }

        [System.Xml.Serialization.XmlElement("Duration")]
        public int Duration { get; set; }
    }


    [Serializable()]
    [System.Xml.Serialization.XmlRoot("MovieCollection")]
    public class MovieCollection
    {
        [XmlArray("Movies")]
        [XmlArrayItem("Movie", typeof(Movie))]
        public List<Movie> Movie { get; set; }
    }
}
