#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Collections;
#endregion

#region mainWindow
namespace podcastClient
{

    public partial class MainWindow : Window
    {
        static string strDownloadsXMLPath = "xml\\downloads.xml";
        static string strFeedsXMLPath = "xml\\feeds.xml";
        static string strFeedImagesDirPath = Environment.CurrentDirectory+"\\images\\";
        static string strDownloadsDirPath = "downloads\\";

        static private ItemsChangeObservableCollection<Episode> obCollEpisodes = new ItemsChangeObservableCollection<Episode>(); // Used for updating the downloads list
        public static ListView lvPodFeeds1 { get; set; }


        public MainWindow()
        {
            if(!Directory.Exists("images"))
            {
                Directory.CreateDirectory("images");
            }
            if (!Directory.Exists("xml"))
            {
                Directory.CreateDirectory("xml");
            }
            if (!Directory.Exists("downloads"))
            {
                Directory.CreateDirectory("downloads");
            }


            InitializeComponent();
            Loaded += MyWindow_Loaded;
            lvPodDownloads.ItemsSource = obCollEpisodes;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lvPodFeeds1 = lvPodFeeds;
            refreshFeeds();
            refreshDownloads();
        }

        private void MainWindow1_Closing(object sender, CancelEventArgs e)
        {
            foreach (Episode ep in obCollEpisodes)
            {
                if (ep.Progress != "Done")
                    ep.feedClient.CancelAsync();
            }
        }
        #endregion

        #region buttons
        private void btnManualAdd_Click(object sender, RoutedEventArgs e)
        {
            manualAdd winMA = new manualAdd();
            winMA.Show();
        }

        private void btnDel_Click(object sender, RoutedEventArgs e)
        {
            if (lvPodFeeds.SelectedItems.Count == 1)
            {
                XDocument xmlFeeds = XDocument.Load(strFeedsXMLPath);
                ListViewItem lviItem = (ListViewItem)lvPodFeeds.SelectedItem;
                string[] arrInfo = (string[])lviItem.Tag;

                // arrInfo =     Title, Desc, Url, Image Name 
                XElement xelDeleteFeed = xmlFeeds.Descendants("podcast").Where(x => (string)x.Attribute("title") == arrInfo[0]).FirstOrDefault();

                if (xelDeleteFeed != null)
                {
                    xelDeleteFeed.Remove(); // Remove from the xml file
                    xmlFeeds.Save(strFeedsXMLPath);
                }
                lvPodFeeds.Items.RemoveAt(lvPodFeeds.Items.IndexOf(lviItem)); // Remove from the listview

                if(File.Exists(strFeedImagesDirPath + arrInfo[3]))
                {
                    File.Delete(strFeedImagesDirPath + arrInfo[3]); // Delete the feeds cover image
                }
            }
        }

        private void btnDelEp_Click(object sender, RoutedEventArgs e)
        {
            if(lvPodDownloads.SelectedItems.Count == 1)
            {
                deleteDownload((Episode)lvPodDownloads.SelectedItem);
            }
        }
        #endregion

        #region refreshing
        public static void refreshFeeds() //Adds the feeds from the xml file to the list view
        {
            lvPodFeeds1.Items.Clear();
            if (!File.Exists(strFeedsXMLPath)) // For safety
            {
                var file = File.Create(strFeedsXMLPath);
                file.Close();
                File.WriteAllText(strFeedsXMLPath, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<feeds>\n</feeds>"); // Setting up xml file
            }

            XmlDocument xmlFeeds = new XmlDocument();
            xmlFeeds.Load(strFeedsXMLPath); // Load xml from the subbed feeds xml

            XmlNodeList nodesFeeds = xmlFeeds.SelectNodes("//feeds/podcast");


            foreach(XmlNode feed in nodesFeeds)
            {
                string strTitle = feed.Attributes["title"].InnerText;
                string strDesc = feed.SelectSingleNode("description").InnerText;
                string strUrl = feed.SelectSingleNode("url").InnerText;
                string strImageName = feed.SelectSingleNode("imageName").InnerText;

                string[] listFeedInfo = { strTitle, strDesc, strUrl, strImageName };
                ListViewItem lviItem = new ListViewItem();
                lviItem.Content = strTitle;

                lviItem.Tag = listFeedInfo; // Associates the feeds information with the listview item so it can be easily accessed later
                lvPodFeeds1.Items.Add(lviItem);

            }

        }

        public static void refreshDownloads()
        {
            if (!File.Exists(strDownloadsXMLPath))
            {
                var file = File.Create(strDownloadsXMLPath);
                file.Close();
                File.WriteAllText(strDownloadsXMLPath, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<feeds>\n</feeds>");
            }

            XDocument xmlDownloads = XDocument.Load(strDownloadsXMLPath);

            IEnumerable<XElement> xmlEpisodes = xmlDownloads.Descendants("episode");

            foreach(XElement xelEpisode in xmlEpisodes)
            {
                string strTitle = xelEpisode.Attribute("title").Value;
                string strDesc = xelEpisode.Element("description").Value;
                string strImgName = xelEpisode.Element("imgName").Value;
                string strFileName = xelEpisode.Element("fileName").Value;
                Episode ep = new Episode() { Title = strTitle, Description = strDesc, FileName = strFileName, ImgName = strImgName, Progress = "Done" };

                if (File.Exists(strDownloadsDirPath + strFileName))
                {
                    obCollEpisodes.Add(ep);
                }
                else
                {
                    deleteDownload(ep);
                }

            }
            
        }

        #endregion

        #region fill episodes list

        Regex reFileName = new Regex(@"[^/\\&\?]+\.\w{3,4}(?=([\?&].*$|$))");
        private readonly BackgroundWorker worker = new BackgroundWorker();

        private void lvPodFeeds_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
            if (e.ChangedButton == MouseButton.Left) // Left button was double clicked
            {

                if (lvPodFeeds.SelectedItems.Count == 1) // Check if an item is selected just to be safe
                {

                    if (!worker.IsBusy)
                    {
                        ListViewItem lviSelected = (ListViewItem)lvPodFeeds.SelectedItem;
                        string[] arrInfo = (string[])lviSelected.Tag;

                        worker.DoWork += worker_DoWork;
                        worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                        worker.RunWorkerAsync(arrInfo);
                        Mouse.OverrideCursor = Cursors.Wait;
                    }

                }
                
            }
            
        }



        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string strTitle;
            string strDesc;
            string strUrl;
            string[] arrInfo = (string[])e.Argument;
            this.Dispatcher.Invoke(() => { lvPodEpisodes.Items.Clear(); });

            //Creating the xml documents to read
            XmlDocument xmlFeed = new XmlDocument();

            // arrInfo = Title, Desc, Url, ImageName (For Phils reference)

            xmlFeed.Load(arrInfo[2]); //Load xml of rss feed with the requested episodes

            XmlNodeList xmlEpisodesNodes = xmlFeed.SelectNodes("//rss/channel/item");

            foreach (XmlNode ep in xmlEpisodesNodes)
            {
                strTitle = ep.SelectSingleNode("title").InnerText;
                strDesc = ep.SelectSingleNode("description").InnerText;
                strUrl = ep.SelectSingleNode("enclosure").Attributes["url"].Value;

                string[] listEpisodesInfo = { strTitle, strDesc, strUrl, reFileName.Match(strUrl).ToString(), arrInfo[3] };

                this.Dispatcher.Invoke(() =>
                {
                    ListViewItem bgItem = new ListViewItem();
                    bgItem.Content = strTitle;
                    bgItem.Tag = listEpisodesInfo; // Associates the feeds information with the listview item so it can be easily accessed later
                    lvPodEpisodes.Items.Add(bgItem);
                }
                );

            }

        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }


        #endregion

        #region selection changed
        private void lvPodEpisodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPodEpisodes.SelectedItems.Count == 1) // Check if an item is selected just to be safe
            {
                ListViewItem lviItem = (ListViewItem)lvPodEpisodes.SelectedItem;
                string[] arrEpInfo = (string[])lviItem.Tag;
                // arrEpInfo =  strTitle, strDesc, strUrl, File Name, Image Name (phils reference)

                txtTitle.Text = arrEpInfo[0];
                txtDesc.Text = arrEpInfo[1];


                try
                {
                    imgFeedImage.Source = LoadImage(strFeedImagesDirPath + arrEpInfo[4]);
                }
                catch (Exception) // If it fails to set the image (Eg. It's non-existent) It will leave it blank
                {
                    imgFeedImage.Source = null;
                }
            }
        }

        private void lvPodFeeds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPodFeeds.SelectedItems.Count == 1) // Check if an item is selected just to be safe
            {
                ListViewItem lviItem = (ListViewItem)lvPodFeeds.SelectedItem;
                string[] arrInfo = (string[])lviItem.Tag;
                txtTitle.Text = arrInfo[0];
                txtDesc.Text = arrInfo[1];

                try
                {
                    imgFeedImage.Source = LoadImage(strFeedImagesDirPath + arrInfo[3]);
                }
                catch (Exception) // If it fails to set the image (Eg. It's non-existent) It will leave it blank
                {
                    imgFeedImage.Source = null;
                }

            }
        }

        private void lvPodDownloads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPodEpisodes.SelectedItems.Count == 1) // Check if an item is selected just to be safe
            {
                
                Episode ep = (Episode)lvPodDownloads.SelectedItem;

                if (ep != null)
                {
                    txtTitle.Text = ep.Title;
                    txtDesc.Text = ep.Description;

                    try
                    {
                        imgFeedImage.Source = LoadImage(strFeedImagesDirPath + ep.ImgName);
                    }
                    catch (Exception) // If it fails to set the image (Eg. It's non-existent) It will leave it blank
                    {
                        imgFeedImage.Source = null;
                    }
                }
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                lvPodDownloads.SelectedItems.Clear();
                lvPodFeeds.SelectedItems.Clear();
                lvPodEpisodes.SelectedItems.Clear();

                txtTitle.Text = "";
                txtDesc.Text = "";
                imgFeedImage.Source = null;
            }

            if (tabSubscriptions.IsSelected)
            {
                btnDel.IsEnabled = true;
                btnDelEp.IsEnabled = false;
            }
            if (tabEpisodes.IsSelected)
            {
                btnDel.IsEnabled = false;
                btnDelEp.IsEnabled = false;
            }
            if (tabDownloads.IsSelected)
            {
                btnDel.IsEnabled = false;
                btnDelEp.IsEnabled = true;
            }

        }
        #endregion

        #region Downloading

        private void lvPodEpisodes_MouseDoubleClick(object senderTwo, MouseButtonEventArgs c) // Downloading the episode in here
        {
            if (lvPodEpisodes.SelectedItems.Count == 1)
            {

                if (c.ChangedButton == MouseButton.Left) // Left button was double clicked
                {
                    ListViewItem lviSelected = (ListViewItem)lvPodEpisodes.SelectedItem;
                    string[] arrEpInfo = (string[])lviSelected.Tag;

                    Uri uriDownloadUrl = new Uri(arrEpInfo[2]);
                    // arrEpInfo =  strTitle, strDesc, strUrl, File Name, Image Name


                    using (WebClient client = new WebClient())
                    {
                        var newEpisode = new Episode() { Title = arrEpInfo[0], Progress = "0%", FileName = arrEpInfo[3], Description = arrEpInfo[1], ImgName = arrEpInfo[4], feedClient = client};
                        if (!obCollEpisodes.Any(p => p.Title == newEpisode.Title && p.Description == newEpisode.Description && p.FileName == newEpisode.FileName)) // Prevents duplicate downloads
                        {
                            obCollEpisodes.Add(newEpisode);

                            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => ProgressChanged(sender, e, newEpisode));
                            client.DownloadFileCompleted += new AsyncCompletedEventHandler((sender, e) => Completed(sender, e, newEpisode));

                            client.DownloadFileAsync(uriDownloadUrl, (strDownloadsDirPath + arrEpInfo[3]));
                        }
                    }


                }
            }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e, Episode ep)
        {

            ep.Progress = $"{e.ProgressPercentage}%";
            //e.ProgressPercentage;
        }

        private void Completed(object sender, AsyncCompletedEventArgs e, Episode ep)
        {
            if (e.Cancelled)
            {
                // delete the partially-downloaded file
                File.Delete(strDownloadsDirPath + ep.FileName);
                obCollEpisodes.Remove(ep);
                return;
            }
            ep.Progress = "Done";

            if (!File.Exists(strDownloadsXMLPath)) //Safety
            {
                var file = File.Create(strDownloadsXMLPath);
                file.Close();
                File.WriteAllText(strDownloadsXMLPath, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<feeds>\n</feeds>");
            }
            XDocument xmlEpisodes = XDocument.Load(strDownloadsXMLPath);

            XElement xmlEpisode = new XElement("episode",
                new XElement("description", ep.Description),
                new XElement("imgName", ep.ImgName),
                new XElement("fileName", ep.FileName));

            xmlEpisode.SetAttributeValue("title", ep.Title);

            xmlEpisodes.Root.Add(xmlEpisode);
            xmlEpisodes.Save(strDownloadsXMLPath);
        }


        #endregion

        #region playing episodes
        private void lvPodDownloads_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvPodDownloads.SelectedItems.Count == 1)
            {
                if (e.ChangedButton == MouseButton.Left) // Left button was double clicked
                {
                    Episode ep = (Episode)lvPodDownloads.SelectedItem;
                    if(File.Exists(strDownloadsDirPath + ep.FileName))
                    {
                        System.Diagnostics.Process.Start(strDownloadsDirPath + ep.FileName);
                    }
                }
            }
        }

        #endregion

        #region misc Functions
        static private void deleteDownload(Episode ep)
        {
            if (ep.Progress == "Done")
            {
                if (File.Exists(strDownloadsDirPath + ep.FileName))
                    File.Delete(strDownloadsDirPath + ep.FileName);
                if (ep != null)
                {
                    obCollEpisodes.Remove(ep);
                }
            }
            else
            {
                ep.feedClient.CancelAsync();
            }


            XDocument xmlDownloads = XDocument.Load(strDownloadsXMLPath);
            XElement xelDeleteFeed = xmlDownloads.Descendants("episode").Where(x => (string)x.Attribute("title") == ep.Title).FirstOrDefault();
            if (xelDeleteFeed != null)
            {
                xelDeleteFeed.Remove();
                xmlDownloads.Save(strDownloadsXMLPath);
            }

        }

        private BitmapImage LoadImage(string strImageFile) // Using this stops the program from locking up images so I can delete them when removing a feed.
        {
            BitmapImage bmpRetVal = null;
            if (strImageFile != null)
            {
                BitmapImage bmpImage = new BitmapImage();
                using (FileStream stream = File.OpenRead(strImageFile))
                {
                    bmpImage.BeginInit();
                    bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImage.StreamSource = stream;
                    bmpImage.EndInit();
                }
                bmpRetVal = bmpImage;
            }
            return bmpRetVal;
        }
    }
    #endregion

    public class Episode : INotifyPropertyChanged
    {
        public string _Title;
        public string Title
        {
            get => _Title;
            set
            {
                _Title = value;
                OnPropertyChanged();
            }
        }

        public string _Progress;
        public string Progress
        {
            get => _Progress;
            set
            {
                _Progress = value;
                OnPropertyChanged();
            }
        }

        public string Description { get; set; }
        public string ImgName { get; set; }
        public string FileName { get; set; }

        public WebClient feedClient { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

    public class ItemsChangeObservableCollection<T> :               // This class tells the listview to update when an Episode property is changed (eg download progress) because normal ObservableCollections do not.
       ObservableCollection<T> where T : INotifyPropertyChanged
    {
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                RegisterPropertyChanged(e.NewItems);
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                UnRegisterPropertyChanged(e.OldItems);
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                UnRegisterPropertyChanged(e.OldItems);
                RegisterPropertyChanged(e.NewItems);
            }

            base.OnCollectionChanged(e);
        }

        protected override void ClearItems()
        {
            UnRegisterPropertyChanged(this);
            base.ClearItems();
        }

        private void RegisterPropertyChanged(IList items)
        {
            foreach (INotifyPropertyChanged item in items)
            {
                if (item != null)
                {
                    item.PropertyChanged += new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
        }

        private void UnRegisterPropertyChanged(IList items)
        {
            foreach (INotifyPropertyChanged item in items)
            {
                if (item != null)
                {
                    item.PropertyChanged -= new PropertyChangedEventHandler(item_PropertyChanged);
                }
            }
        }

        private void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

}
