//using IMDb_Scraper;
using IMDb_Scraper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace MovieViewerWPF
{
    public class ImdbHelper
    {
        string thumbnailPath = null;
        string cacheFilePath = null;
        public static MovieCollection movies = null;
        public ImdbHelper()
        {
            string appRoot = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            thumbnailPath = string.Format(@"{0}\Thumbnails", appRoot);
            cacheFilePath = string.Format(@"{0}\Movies.xml", appRoot);
            if (!Directory.Exists(thumbnailPath))
                Directory.CreateDirectory(thumbnailPath);
        }

        public Movie GetMovie(string localFileName, string matchingMovieName)
        {
            var mm = movies.Movie.Where(m => m.LocalName.ToLower() == matchingMovieName.ToLower()).FirstOrDefault();
            if (mm == null)
            {
                mm = new Movie();                
                IMDb imdb = GetImdbMovie(matchingMovieName);
                if (imdb != null && imdb.Id != null)
                {
                    mm.Name = HttpUtility.HtmlDecode(string.IsNullOrWhiteSpace(imdb.OriginalTitle) ? imdb.Title : imdb.OriginalTitle);
                    mm.LocalName = matchingMovieName;
                    mm.FullLocalPath = localFileName;
                    mm.Rating = imdb.Rating;
                    //mm.Genre = imdb.Genres.Count > 0 ? string.Format("{0}, {1}", imdb.Genres[0], imdb.Genres[1])  : string.Empty;
                    mm.Genre = imdb.Genres.Count > 0 ? (string.Format("{0}", imdb.Genres.Count > 1 ?
                    string.Format("{0}, {1}", imdb.Genres[0], imdb.Genres[1]) : imdb.Genres[0])) : string.Empty;
                    mm.ImageThumbnail = imdb.Poster;
                    mm.Id = imdb.Id;
                    mm.LocalImageThumbnail = LoadImage(mm);
                    //mm.Year = imdb.Year;
                    //mm.Duration = string.IsNullOrEmpty(imdb.Runtime) ? 0 : int.Parse(imdb.Runtime);
                    //if (mm.Id != imdb.Id)
                    //    mm.LocalImageThumbnail = null;
                    //mm.Id = imdb.Id;
                }
                else
                {
                    mm.Name = matchingMovieName;                    
                    mm.FullLocalPath = localFileName;
                    mm.LocalName = matchingMovieName;
                }
                //mm.ShortName = mm.Name.Length <= 13 ? mm.Name : $"{mm.Name.Substring(0, 10)}...";
                movies.Movie.Add(mm);
            }
            mm.ShortName = mm.Name.Length <= 22 ? mm.Name : $"{mm.Name.Substring(0, 19)}...";
            return mm;
        }

        private string LoadImage(Movie mm)
        {
            string imagePath = string.Format(@"{0}\{1}.jpg", thumbnailPath, mm.Id.ToString());
            if (File.Exists(imagePath))
            {
                //Interlocked.Increment(ref Utility.foundCounter);
                //Utility.Log("Found: " + mm.Name);
                return mm.LocalImageThumbnail = imagePath;
            }
            //Interlocked.Increment(ref Utility.NotFoundCounter);
            string url = string.IsNullOrWhiteSpace(mm.LocalImageThumbnail) ? mm.ImageThumbnail : mm.LocalImageThumbnail;
            if (!string.IsNullOrEmpty(url))
            {
                Uri urlUri = new Uri(url);
                var request = WebRequest.CreateDefault(urlUri);

                byte[] buffer = new byte[4096];

                using (var target = new FileStream(imagePath, FileMode.Create, FileAccess.Write))
                {
                    using (var response = request.GetResponse())
                    {
                        using (var stream = response.GetResponseStream())
                        {
                            int read;

                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                target.Write(buffer, 0, read);
                            }
                        }
                    }
                }
                mm.LocalImageThumbnail = imagePath;
            }
            return mm.LocalImageThumbnail;
        }

        public void UpdateCache()
        {
                using (var fs = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    new XmlSerializer(typeof(MovieCollection)).Serialize(fs, movies);
                }
            //new JsonSerializer().Serialize()
        }

        public Stopwatch sw = new Stopwatch();
        private IMDb GetImdbMovie(string matchingMovieName)
        {
            IMDb imdb = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    sw.Restart();
                    imdb = new IMDb(matchingMovieName);
                    imdb.ParseIMDbPage();
                    sw.Stop();
                    break;
                }
                catch (WebException ex)
                {
                    using (var sw = new StreamWriter(new FileStream("ErrorLog.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
                    {
                        sw.WriteLineAsync(string.Format("Error while getting imdb data for file: {0}. {1}", matchingMovieName, ex.Message));
                    }
                }
            }            
            return imdb;
        }
    }
}
