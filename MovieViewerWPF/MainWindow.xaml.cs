using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MovieViewerWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ManualResetEvent completedEvent = new ManualResetEvent(false);
        public MainWindow()
        {
            InitializeComponent();
            var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(ShowData);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
            dispatcherTimer.Start();
        }

        private DispatcherTimer dispatcherTimer = null;
        private BackgroundWorker worker = null;
        private void button_Click(object sender, RoutedEventArgs e)
        {
            statusLebel.Text = "Scanning...";
            //FindDuplicates();
            //ShowData(null, null);

            Action workAction = delegate
            {
                worker = new BackgroundWorker();
                worker.DoWork += delegate
                {
                    FindDuplicates();
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
                if (view == RenderView.File)
                    RenderInFileView();
                else
                    RenderInDirView();
            }
            if (completed && !stopRefresh)
            {
                stopRefresh = true;
                statusLebel.Text = "Scan complete";
            }

        }

        private bool stopRefresh = false;
        private string GetFileHash(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return Encoding.Default.GetString(md5.ComputeHash(stream));
                }
            }
        }
        Dictionary<string, List<FileInfo>> dupDataDict = new Dictionary<string, List<FileInfo>>();
        RenderView view = RenderView.File;
        Dictionary<string, List<MovieInfo>> data = new Dictionary<string, List<MovieInfo>>();//<MovieInfo>();
        //List<MovieInfo> data = new List<MovieInfo>();//<MovieInfo>();
        private void FindDuplicates()
        {
            //sizeLebel.Text = "";

            dupDataDict.Clear();
            var dataDict = new Dictionary<string, List<FileInfo>>();
            //ic.Items.Clear();
            List<string> exclusionList = new List<string>();
            int sz = 500;

            var extList = new HashSet<string>();
            extList.Add("avi");
            extList.Add("mkv");
            extList.Add("mp4");
            //extList.Add("jpg");
            extList.Add("vob");
            // extList.Add("txt");


            //Parallel.ForEach(SafeFileEnumerator.EnumerateFiles(@"D:\", SearchOption.AllDirectories, extList, exclusionList, sz), file1 =>
            foreach (var file1 in SafeFileEnumerator.EnumerateFiles(@"D:\DP", SearchOption.AllDirectories, extList, exclusionList, sz))
            {
                string matchingMovieName = GetMatch(System.IO.Path.GetFileNameWithoutExtension(file1));
                //data.Add(file1, new MovieInfo() {Title = matchingMovieName});
                data.Add("Watched", new List<MovieInfo>() { new MovieInfo() { Title = matchingMovieName }});

                //var hash = GetFileHash(file1);
                //if (dataDict.ContainsKey(hash))
                //{
                //    if (!dupDataDict.ContainsKey(hash))
                //        dupDataDict.Add(hash, dataDict[hash]);
                //    dupDataDict[hash].Add(new FileInfo(file1));
                //}
                //else
                //{
                //    dataDict.Add(hash, new List<FileInfo> { new FileInfo(file1) });
                //}
            }
            data.Add("New", new List<MovieInfo>() { new MovieInfo() { Title = "Hyderabad" }, new MovieInfo() { Title = "Delhi" } });
            completed = true;
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
                                movieName.IndexOf('-'), movieName.IndexOf("720p"), movieName.IndexOf("1080p") };
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

        private bool completed = false;

        private void RenderInFileView()
        {
            var items = new List<TreeViewItem>();
            var mm = new MovieInfo {Title = "Hyderabad days"};
            //data["Watched"].Add(mm);

        //data.Add(new MovieInfo() { Title = "Title1" });
            //data.Add(new MovieInfo() { Title = "Title2" });
            //data.Add(new MovieInfo() { Title = "Title3" });
            //data.Add(new MovieInfo() { Title = "Title4" });
            //data.Add(new MovieInfo() { Title = "Title5" });
            ic.ItemsSource = data.ToList();//Values.ToList();//OrderByDescending(x => x);//.SelectMany(x=> x.);//.ToList();//OrderByDescending(x => x); //.OrderByDescending(x => x);
            //ic.ItemsSource = dupDataDict.Values.OrderByDescending(x => x.Sum(z => z.Length));


        }

        private void RenderInDirView()
        {
            foreach (var data in dupDataDict.Values)//.SelectMany(x => x.Select(z => z.DirectoryName)).Distinct())
            {
                TreeViewItem treeItem = new TreeViewItem();
                treeItem.IsExpanded = true;
                treeItem.FontWeight = FontWeights.Bold;
                treeItem.FontSize = 14;
                treeItem.Header = data;
                ic.Items.Add(treeItem);
            }
        }

        List<string> deleteList = new List<string>();
        Queue<string> selectedDirList = new Queue<string>();

        private long sizeBytes = 0;

        enum RenderView
        {
            File,
            Folder
        };

        private void Button1_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var file in deleteList)
            {
                File.Delete(file);
            }
            sizeLebel.Text = $"Files deleted. {sizeLebel.Text} saved";
        }

        static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return "0.0 bytes"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, SizeSuffixes[mag]);
        }

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string file = ((System.Windows.FrameworkElement)sender).DataContext.ToString();
            Process ps = new Process();
            var psi = new ProcessStartInfo();
            psi.FileName = @"C:\Windows\System32\rundll32.exe";
            psi.Arguments = $@"""C:\Program Files (x86)\Windows Photo Viewer\PhotoViewer.dll"", ImageView_Fullscreen {file}";
            ps.StartInfo = psi;
            ps.Start();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            string file = ((System.Windows.FrameworkElement)sender).DataContext.ToString();
            if (((Button)sender).Opacity == 1.0)
            {
                ((Button)sender).Opacity = 0.5;
                deleteList.Add(file);
                sizeBytes += new FileInfo(file).Length;
            }
            else
            {
                ((Button)sender).Opacity = 1.0;
                deleteList.Remove(((FrameworkElement)sender).DataContext.ToString());
                sizeBytes -= new FileInfo(file).Length;
            }
            sizeLebel.Text = $"{SizeSuffix(sizeBytes)}";
        }

        private void FrameworkElement_OnInitialized(object sender, EventArgs e)
        {
            var button = (Button)sender;
            string file = ((System.Windows.FrameworkElement)sender).DataContext.ToString();
            if (System.IO.Path.GetExtension(file) != ".jpg")
            {
                button.Height = 20;
                button.Width = 20;
                button.Template = null;
                button.Content = file;
            }
            else
            {
                button.Height = 100;
                button.Width = 100;
            }
        }
    }

    public class MovieInfo
    {
        public string Title { get; set; }
    }
}
