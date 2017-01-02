using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace MovieViewerWPF
{
    public partial class MainWindow : Window
    {
        private string cacheFilePath = null;
        private string appRoot = null;
        MovieCollection movies = null;
        ImdbHelper imdb = null;
        string dirName = null;
        private bool stopRefresh = false;
        ObservableCollection<Movie> data = new ObservableCollection<Movie>();
        private BackgroundWorker worker = null;
        private bool completed = false;

        public MainWindow()
        {
            InitializeComponent();
            dirName = textBox.Text;
            imdb = new ImdbHelper();

            appRoot = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            cacheFilePath = string.Format(@"{0}\Movies.xml", appRoot);
            ImdbHelper.movies = ReadCache();
            ic.ItemsSource = data;//.OrderByDescending(d => d.Rating));
        }
        
        private void button_Click(object sender, RoutedEventArgs e)
        {
            dirName = textBox.Text;
            statusLebel.Text = $"Scanning.";
            completed = false;
            stopRefresh = false;

            Action workAction = delegate
            {
                worker = new BackgroundWorker();
                worker.DoWork += delegate
                {
                    SearchMovies();
                };
                worker.RunWorkerCompleted += delegate
                {
                    ShowData(null, null);
                };
                worker.RunWorkerAsync();
            };
            button.Dispatcher.BeginInvoke(DispatcherPriority.Background, workAction);
        }

        private void ShowData(object sender, EventArgs e)
        {
            if (!stopRefresh)
            {
                statusLebel.Text = $"Scanning. {data.Count} items";
            }
            if (completed && !stopRefresh)
            {
                stopRefresh = true;
                statusLebel.Text = $"Scan complete. {data.Count} items";
            }
        }

        private void SearchMovies()
        {
            Dispatcher.Invoke(() => { data.Clear(); });
            var dataDict = new Dictionary<string, List<FileInfo>>();
            List<string> exclusionList = new List<string>();
            int sz = 500;
            var extList = new HashSet<string>();
            extList.Add("avi");
            extList.Add("mkv");
            extList.Add("mp4");
            extList.Add("vob");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //foreach (var file1 in SafeFileEnumerator.EnumerateFiles(dirName, SearchOption.AllDirectories, extList, exclusionList, sz))
                Parallel.ForEach(SafeFileEnumerator.EnumerateFiles(dirName, SearchOption.AllDirectories, extList, exclusionList, sz), new ParallelOptions { MaxDegreeOfParallelism = 4}, file1 =>
            {
                string matchingMovieName = GetMatch(System.IO.Path.GetFileNameWithoutExtension(file1));
                var movie = imdb.GetMovie(file1, matchingMovieName);
                movie.FullLocalPath = file1;
                Dispatcher.Invoke(() => { data.Add(movie); });
            }
            );
            using (var sw1 = new StreamWriter(new FileStream("ErrorLog.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
            {
                sw1.WriteLineAsync($"Total time taken: {imdb.sw.Elapsed}");
                sw1.WriteLineAsync($"Total time taken in download data: {IMDb_Scraper.IMDb.sw.Elapsed}");
                sw1.WriteLineAsync($"Total time taken in regex: {IMDb_Scraper.IMDb.sw1.Elapsed}");
            }

            Dispatcher.Invoke(() => { timeTakenLebel.Text = $"Time: {sw.Elapsed.TotalSeconds}s"; });
            completed = true;
            imdb.UpdateCache();
        }

        private string GetMatch(string movieName)
        {
            movieName = movieName.Replace(".", " ");
            int yearIndex = -1;
            Match m = Regex.Match(movieName, @"^(.+)\s(19|20)\d{2}");
            if (m.Success)
                yearIndex = m.Value.Length - 4;
            int minIndex = int.MaxValue;
            bool found = false;
            int[] indexes = { yearIndex, movieName.IndexOf('['), movieName.IndexOf('('),
                                movieName.IndexOf('-'), movieName.IndexOf("720p"), movieName.IndexOf("1080p"), movieName.IndexOf("DVDRip") };
            foreach (var item in indexes)
            {
                if (item >= 0 && item <= minIndex)
                {
                    found = true;
                    minIndex = item;
                }
            }
            if (found)
                movieName = movieName.Substring(0, minIndex);
            return movieName.Trim();
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string file = ((MovieViewerWPF.Movie)(((System.Windows.FrameworkElement)sender).DataContext)).FullLocalPath;
            Process.Start(file);
        }

        private void FrameworkElement_OnInitialized(object sender, EventArgs e)
        {
            var button = (Button)sender;
            string file = ((MovieViewerWPF.Movie)(((System.Windows.FrameworkElement)sender).DataContext)).LocalImageThumbnail;
            if (System.IO.Path.GetExtension(file) != ".jpg")
            {
                button.Height = 20;
                button.Width = 20;
                button.Template = null;
                button.Content = file;
            }
            else
            {
                button.Height = 130;
                button.Width = 130;
            }
        }

        private MovieCollection ReadCache()
        {
            movies = new MovieCollection() { Movie = new List<Movie>() };
            try
            {

                if (File.Exists(cacheFilePath))
                {
                    StreamReader reader = new StreamReader(cacheFilePath);
                    XmlSerializer serializer = new XmlSerializer(typeof(MovieCollection));
                    movies = (MovieCollection)serializer.Deserialize(reader);
                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }
            return movies;
        }

        private void Grid_MouseEnter(object sender, MouseEventArgs e)
        {
            Grid b = sender as Grid;
            //b.BorderThickness = new Thickness(3);
            Button btn = (Button)b.FindName("btnEye");
            btn.Visibility = Visibility.Visible;
        }

        private void Grid_MouseLeave(object sender, MouseEventArgs e)
        {
            Grid b = sender as Grid;
            //b.BorderThickness = new Thickness(1);
            Button btn = (Button)b.FindName("btnEye");
            btn.Visibility = Visibility.Hidden;
        }

        private void chkCache_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkCache = (CheckBox)sender;
            if (!(bool)checkCache.IsChecked)
                ImdbHelper.movies.Movie.Clear();
        }
    }
}
