#region imports
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.IO;
using System.Net;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Collections.Specialized;
using System.Collections;
#endregion

#region junk
namespace podcastClient
{

    public partial class MainWindow : Window
    {
        static string strDownloadsXMLPath = "xml\\downloads.xml";
        static string strFeedsXMLPath = "xml\\feeds.xml";
        static string strFeedImagesDirPath = Environment.CurrentDirectory+"\\images\\";
        static string strDownloadsDirPath = "downloads\\";

        static private ItemsChangeObservableCollection<Episode> episodes = new ItemsChangeObservableCollection<Episode>(); // Used for updating the downloads list
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
            lvPodDownloads.ItemsSource = episodes;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lvPodFeeds1 = lvPodFeeds;
            refreshFeeds();
            refreshDownloads();
        }

        private void MainWindow1_Closing(object sender, CancelEventArgs e)
        {
            foreach (Episode ep in episodes)
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

                ListViewItem item = (ListViewItem)lvPodFeeds.SelectedItem;
                string[] arrInfo = (string[])item.Tag;
                // arrInfo =     Title, Desc, Url, Image Name 
                XElement deleteFeed = xmlFeeds.Descendants("podcast").Where(x => (string)x.Attribute("title") == arrInfo[0]).FirstOrDefault();
                deleteFeed.Remove();
                xmlFeeds.Save(strFeedsXMLPath);
                lvPodFeeds.Items.RemoveAt(lvPodFeeds.Items.IndexOf(item));
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
                ListViewItem item = new ListViewItem();
                item.Content = strTitle;

                item.Tag = listFeedInfo; // Associates the feeds information with the listview item so it can be easily accessed later
                lvPodFeeds1.Items.Add(item);

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

            foreach(XElement episode in xmlEpisodes)
            {
                string strTitle = episode.Attribute("title").Value;
                string strDesc = episode.Element("description").Value;
                string strImgName = episode.Element("imgName").Value;
                string strFileName = episode.Element("fileName").Value;
                Episode ep = new Episode() { Title = strTitle, Description = strDesc, FileName = strFileName, ImgName = strImgName, Progress = "Done" };

                if (File.Exists(strDownloadsDirPath + strFileName))
                {
                    episodes.Add(ep);
                }
                else
                {
                    deleteDownload(ep);
                }

            }
            
        }

        #endregion

        #region lvPodFeeds


        Regex reFileName = new Regex(@"[^/\\&\?]+\.\w{3,4}(?=([\?&].*$|$))");
        private void lvPodFeeds_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            
            if (e.ChangedButton == MouseButton.Left) // Left button was double clicked
            {
                lvPodEpisodes.Items.Clear();
                if (lvPodFeeds.SelectedItems.Count == 1) // Check if an item is selected just to be safe
                {
                    string strTitle;
                    string strDesc;
                    string strUrl;

                    ListViewItem selected = (ListViewItem)lvPodFeeds.SelectedItem;
                    string[] arrInfo = (string[])selected.Tag;

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

                        string[] listEpisodesInfo = { strTitle, strDesc, strUrl, reFileName.Match(strUrl).ToString(), arrInfo[3]};
                        ListViewItem item = new ListViewItem();
                        item.Content = strTitle;
                        item.Tag = listEpisodesInfo; // Associates the feeds information with the listview item so it can be easily accessed later
                        lvPodEpisodes.Items.Add(item);
                    }

                }
                
            }
            
        }
        #endregion

        #region selection changed
        private void lvPodEpisodes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPodEpisodes.SelectedItems.Count == 1) // Check if an item is selected just to be safe
            {
                ListViewItem item = (ListViewItem)lvPodEpisodes.SelectedItem;
                string[] epInfo = (string[])item.Tag;
                // epInfo =  strTitle, strDesc, strUrl, File Name, Image Name

                txtTitle.Text = epInfo[0];
                txtDesc.Text = epInfo[1];


                try
                {
                    imgFeedImage.Source = new BitmapImage(new Uri((strFeedImagesDirPath + epInfo[4])));
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
                ListViewItem item = (ListViewItem)lvPodFeeds.SelectedItem;
                string[] arrInfo = (string[])item.Tag;
                txtTitle.Text = arrInfo[0];
                txtDesc.Text = arrInfo[1];

                try
                {
                    imgFeedImage.Source = new BitmapImage(new Uri((strFeedImagesDirPath + arrInfo[3])));
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
                        imgFeedImage.Source = new BitmapImage(new Uri((strFeedImagesDirPath + ep.ImgName)));
                    }
                    catch (Exception) // If it fails to set the image (Eg. It's non-existent) It will leave it blank
                    {
                        imgFeedImage.Source = null;
                    }
                }
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
                    ListViewItem selected = (ListViewItem)lvPodEpisodes.SelectedItem;
                    string[] epInfo = (string[])selected.Tag;

                    Uri downloadUrl = new Uri(epInfo[2]);
                    // epInfo =  strTitle, strDesc, strUrl, File Name, Image Name


                    using (WebClient client = new WebClient())
                    {
                        var newEpisode = new Episode() { Title = epInfo[0], Progress = "0%", FileName = epInfo[3], Description = epInfo[1], ImgName = epInfo[4], feedClient = client};
                        episodes.Add(newEpisode);
                        int index = episodes.IndexOf(newEpisode);

                        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((sender, e) => ProgressChanged(sender, e, newEpisode));
                        client.DownloadFileCompleted += new AsyncCompletedEventHandler((sender, e) => Completed(sender, e, newEpisode));

                        client.DownloadFileAsync(downloadUrl, (strDownloadsDirPath + epInfo[3]));
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
                episodes.Remove(ep);
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

        static private void deleteDownload(Episode ep)
        {
            if (ep.Progress == "Done")
            {
                if (File.Exists(strDownloadsDirPath +  ep.FileName))
                    File.Delete(strDownloadsDirPath +  ep.FileName);
                if (ep != null)
                {
                    episodes.Remove(ep);
                }
            }
            else
            {
                ep.feedClient.CancelAsync();
            }


            XDocument xmlDownloads = XDocument.Load(strDownloadsXMLPath);
            XElement deleteFeed = xmlDownloads.Descendants("episode").Where(x => (string)x.Attribute("title") == ep.Title).FirstOrDefault();
            if (deleteFeed != null)
            {
                deleteFeed.Remove();
                xmlDownloads.Save(strDownloadsXMLPath);
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

        }
    }

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

    public class ItemsChangeObservableCollection<T> :
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
//TODO: add an about page with the version creator etc
