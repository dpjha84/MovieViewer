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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        ObservableCollection<Movie> dataCopy = new ObservableCollection<Movie>();
        private BackgroundWorker worker = null;
        private bool completed = false;
        private bool? showWatched = null;

        public MainWindow()
        {
            InitializeComponent();
            dirName = textBox.Text;
            imdb = new ImdbHelper();

            appRoot = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            cacheFilePath = string.Format(@"{0}\Movies.xml", appRoot);
            ImdbHelper.movies = ReadCache();
            ic.ItemsSource = data;
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
                Parallel.ForEach(SafeFileEnumerator.EnumerateFiles(dirName, SearchOption.AllDirectories, extList, exclusionList, sz), new ParallelOptions { MaxDegreeOfParallelism = -1}, file1 =>
            {
                string matchingMovieName = GetMatch(System.IO.Path.GetFileNameWithoutExtension(file1));
                var movie = imdb.GetMovie(file1, matchingMovieName);
                movie.FullLocalPath = file1;
                Dispatcher.Invoke(() =>
                {
                    data.Add(movie);
                    data.Sort(d => d.Rating);
                });
            }
            );
            dataCopy = Clone(data);
            Dispatcher.Invoke(() => { timeTakenLebel.Text = $"Time: {sw.Elapsed.TotalSeconds}s"; });
            completed = true;
            imdb.UpdateCache();
        }

        public ObservableCollection<Movie> Clone(object obj)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new MemoryStream();
            using (stream)
            {
                formatter.Serialize(stream, obj);
                stream.Seek(0, SeekOrigin.Begin);
                return formatter.Deserialize(stream) as ObservableCollection<Movie>;
            }
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
                                movieName.IndexOf('-'), movieName.IndexOf("720p"), movieName.IndexOf("1080p"), movieName.IndexOf("DVDRip", StringComparison.OrdinalIgnoreCase),
                             movieName.IndexOf("DVDSCR", StringComparison.OrdinalIgnoreCase)};
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
            var movie = ((MovieViewerWPF.Movie) (((System.Windows.FrameworkElement) sender).DataContext));
            if (movie == null) return;
            string file = movie.LocalImageThumbnail;
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

        private void chkCache_Unchecked(object sender, RoutedEventArgs e)
        {
            var checkCache = (CheckBox)sender;
            if (!(bool)checkCache.IsChecked)
                ImdbHelper.movies.Movie.Clear();
        }

        private void BtnEye_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var movie = ((Movie) (((FrameworkElement) sender).DataContext));
            movie.Watched = !movie.Watched;
            dataCopy = data.Clone();
            imdb.UpdateCache();
        }

        private void BtnWatched_OnClick(object sender, RoutedEventArgs e)
        {
            if (showWatched == null)
                showWatched = true;
            else
                showWatched = !showWatched;

            data.Clear();
            foreach (var movie in dataCopy)
            {
                var temp = dataCopy.FirstOrDefault(a => a.Id == movie.Id && a.Watched == (bool) showWatched);
                if (temp != null)
                    data.Add(temp);
            }
        }
    }

    public static class Extensions
    {
        public static void Sort<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> sortedList = source.OrderByDescending(keySelector).ToList();
            source.Clear();
            foreach (var sortedItem in sortedList)
            {
                source.Add(sortedItem);
            }
        }

        public static T Clone<T>(this T source)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(ms, source);
            ms.Position = 0;
            object obj = bf.Deserialize(ms);
            ms.Close();
            return (T)obj;
        }

        private void txtSearch_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //txtSearch.Text = "";
        }

        private void txtSearch_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            //txtSearch.Text = "Search Movies";
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            data = Clone(dataCopy);
            ic.ItemsSource = data;
            var text = ((TextBox)sender).Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            foreach (var i in dataCopy)
            {
                if (!i.Name.ToLower().Contains(text.ToLower()))
                {
                    data.Remove(data.FirstOrDefault(d => d.Id == i.Id));
                }
            }
        }
    }

    public class TextInputToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Always test MultiValueConverter inputs for non-null
            // (to avoid crash bugs for views in the designer)
            if (values[0] is bool && values[1] is bool)
            {
                bool hasText = !(bool)values[0];
                bool hasFocus = (bool)values[1];

                if (hasFocus || hasText)
                    return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public static class Extensions
    {
        public static ObservableCollection<T> Clone<T>(this ObservableCollection<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()) as ObservableCollection<T>;
        }

        public static void Sort<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> sortedList = source.OrderBy(keySelector).ToList();
            source.Clear();
            foreach (var sortedItem in sortedList)
            {
                source.Add(sortedItem);
            }
        }

        public static void ReverseSort<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector)
        {
            List<TSource> sortedList = source.OrderByDescending(keySelector).ToList();
            source.Clear();
            foreach (var sortedItem in sortedList)
            {
                source.Add(sortedItem);
            }
        }
    }
}
