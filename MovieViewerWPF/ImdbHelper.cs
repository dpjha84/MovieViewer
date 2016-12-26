//using IMDb_Scraper;
using IMDb_Scraper;
using System;
using System.Collections.Generic;
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
                    //mm.Genre = imdb.Genres.Count > 0 ? (string.Format("{0}", imdb.Genres.Count > 1 ?
                    //string.Format("{0}, {1}", imdb.Genres[0], imdb.Genres[1]) : imdb.Genres[0])) : string.Empty;
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
                movies.Movie.Add(mm);
            }
            //IMDb_Scraper.IMDb imdb = new IMDb()
            
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

        private void objImage_DownloadCompleted(object sender, EventArgs e)
        {
            string imagePath = string.Format(@"{0}\{1}.jpg", thumbnailPath, Guid.NewGuid().ToString());
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            Guid photoID = System.Guid.NewGuid();
            String photolocation = photoID.ToString() + ".jpg";  //file name 

            encoder.Frames.Add(BitmapFrame.Create((BitmapImage)sender));

            using (var filestream = new FileStream(photolocation, FileMode.Create))
                encoder.Save(filestream);
        }

        List<IMDb> ImdbMovies = new List<IMDb>();

        public async Task AwaitMovies()
        {

            foreach (var imdb in ImdbMovies)
            {
                var mm = movies.Movie.Where(m => m.LocalName == imdb.MovieInfoTask.MovieName).FirstOrDefault();
                await imdb.AwaitMovies(imdb.MovieInfoTask);
                if (imdb != null && imdb.Id != null)
                {
                    mm.Name = HttpUtility.HtmlDecode(string.IsNullOrWhiteSpace(imdb.OriginalTitle) ? imdb.Title : imdb.OriginalTitle);
                    //mm.LocalName = matchingMovieName;
                    //.FullLocalPath = localFileName;
                    mm.Rating = imdb.Rating;
                    //mm.Genre = imdb.Genres.Count > 0 ? string.Format("{0}, {1}", imdb.Genres[0], imdb.Genres[1])  : string.Empty;
                    //mm.Genre = imdb.Genres.Count > 0 ? (string.Format("{0}", imdb.Genres.Count > 1 ?
                    //string.Format("{0}, {1}", imdb.Genres[0], imdb.Genres[1]) : imdb.Genres[0])) : string.Empty;
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
                    //mm.Name = matchingMovieName;
                    //mm.FullLocalPath = localFileName;
                    //mm.LocalName = matchingMovieName;
                }
            }
        }


        private IMDb GetImdbMovie(string matchingMovieName)
        {
            IMDb imdb = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    imdb = new IMDb(matchingMovieName);
                    ImdbMovies.Add(imdb);
                    break;
                }
                catch (WebException ex)
                {
                    if (i == 2)
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
