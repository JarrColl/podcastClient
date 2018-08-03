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
#endregion

#region junk
namespace podcastClient
{

    public partial class MainWindow : Window
    {

        public static ListView lvPodFeeds1 { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MyWindow_Loaded;
        }

        private void MyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lvPodFeeds1 = lvPodFeeds;
            refreshFeeds();
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
                XDocument xmlFeeds = XDocument.Load("feeds.xml");

                ListViewItem item = (ListViewItem)lvPodFeeds.SelectedItem;
                string[] arrInfo = (string[])item.Tag;
                // arrInfo =     Title, Desc, Url, Image Name 
                XElement deleteFeed = xmlFeeds.Descendants("podcast").Where(x => (string)x.Attribute("title") == arrInfo[0]).FirstOrDefault();
                deleteFeed.Remove();
                xmlFeeds.Save("feeds.xml");
                lvPodFeeds.Items.RemoveAt(lvPodFeeds.Items.IndexOf(item));
            }
        }
        #endregion

        #region refreshing
        public static void refreshFeeds() //Adds the feeds from the xml file to the list view
        {
            XmlDocument xmlFeeds = new XmlDocument();
            xmlFeeds.Load("feeds.xml"); // Load xml from the subbed feeds xml

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

        //public static void refreshEpisodes(ListView lv, bool booCheckOnline) //Adds the feeds from the xml file to the list view
        //{
        //    XDocument xmlFeeds = XDocument.Load("episodes.xml");

        //    //XmlNodeList nodesFeeds = xmlFeeds.SelectNodes("//feeds/podcast");


        //    foreach (XmlNode feed in nodesFeeds)
        //    {
        //        string strTitle = feed.SelectSingleNode("title").InnerText;
        //        string strDesc = feed.SelectSingleNode("description").InnerText;
        //        string strUrl = feed.SelectSingleNode("url").InnerText;
        //        string strImageName = feed.SelectSingleNode("imageName").InnerText;

        //        string[] listFeedInfo = { strTitle, strDesc, strUrl, strImageName };
        //        ListViewItem item = new ListViewItem();
        //        item.Content = strTitle;

        //        item.Tag = listFeedInfo; // Associates the feeds information with the listview item so it can be easily accessed later
        //        lv.Items.Add(item);

        //    }

        //}
        #endregion

        #region Subs listview
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
                    imgFeedImage.Source = new BitmapImage(new Uri((Environment.CurrentDirectory + "\\..\\..\\feedImages\\" + arrInfo[3])));
                }
                catch(Exception) // If it fails to set the image (Eg. It's non-existent) It will leave it blank
                {
                    imgFeedImage.Source = null;
                }

            }
        }

        private void lvPodFeeds_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) // Left button was double clicked
            {

                if (lvPodFeeds.SelectedItems.Count == 1) // Check if an item is selected just to be safe
                {
                    string strTitle;
                    string strDesc;
                    string strUrl;

                    ListViewItem selected = (ListViewItem)lvPodFeeds.SelectedItem;
                    string[] arrInfo = (string[])selected.Tag;
                    //XmlReader xmlFeed = XmlReader.Create(arrInfo[2]);
                    string[] epInfo = new string[4];


                    //Creating the xml documents to read
                    XmlDocument xmlFeed = new XmlDocument();

                    // Check if episodes.xml exists and create it if it doesn't
                    if (!File.Exists("episodes.xml"))
                    {
                        var file = File.Create("episodes.xml");
                        file.Close();
                        File.WriteAllText("episodes.xml", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<feeds>\n</feeds>");
                    }
                    XDocument xmlEpisodes = XDocument.Load("episodes.xml");


                    // arrInfo = Title, Desc, Url, ImageName (For Phils reference)

                    xmlFeed.Load(arrInfo[2]); //Load xml of rss feed with the requested episodes

                    XmlNodeList xmlEpisodesNodes = xmlFeed.SelectNodes("//rss/channel/item");

                    XElement deleteFeed = xmlEpisodes.Descendants().Where(x => (string)x.Attribute("name") == arrInfo[0]).FirstOrDefault();
                    if (deleteFeed != null)
                        deleteFeed.Remove();


                    XElement podcast = new XElement("podcast");
                    podcast.SetAttributeValue("name", arrInfo[0]);
                    xmlEpisodes.Root.Add(podcast); //ADD CHECKING TO SEE IF FEED/EPISODE IS ALREADY THERE


                    foreach (XmlNode ep in xmlEpisodesNodes)
                    {
                        strTitle = ep.SelectSingleNode("title").InnerText;
                        strDesc = ep.SelectSingleNode("description").InnerText;
                        strUrl = ep.SelectSingleNode("enclosure").Attributes["url"].Value;

                        // The episode for the xml file
                        XElement episode = new XElement("episode",
                            new XElement("description", strDesc),
                            new XElement("url", strUrl));


                        episode.SetAttributeValue("title", strTitle);

                        XElement currentFeed = xmlEpisodes.Descendants().Where(x => (string)x.Attribute("name") == arrInfo[0]).FirstOrDefault();
                        currentFeed.Add(episode);


                        string[] listEpisodesInfo = { strTitle, strDesc, strUrl, arrInfo[3] };
                        ListViewItem item = new ListViewItem();
                        item.Content = strTitle;
                        item.Tag = listEpisodesInfo; // Associates the feeds information with the listview item so it can be easily accessed later
                        lvPodEpisodes.Items.Add(item);
                    }
                    xmlEpisodes.Save("episodes.xml");

                }
                
            }
            
        }
        #endregion

        #region Downloading
        private void lvPodDownloads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPodEpisodes.SelectedItems.Count == 1) // Check if an item is selected just to be safe
            {
                ListViewItem item = (ListViewItem)lvPodEpisodes.SelectedItem;
                string[] epInfo = (string[])item.Tag;
                txtTitle.Text = epInfo[0];
                txtDesc.Text = epInfo[1];

                try
                {
                    imgFeedImage.Source = new BitmapImage(new Uri((Environment.CurrentDirectory + "\\..\\..\\feedImages\\" + epInfo[3])));
                }
                catch (Exception) // If it fails to set the image (Eg. It's non-existent) It will leave it blank
                {
                    imgFeedImage.Source = null;
                }
            }
        }
        private void lvPodEpisodes_MouseDoubleClick(object sender, MouseButtonEventArgs e) // Downloading the episode in here
        {
            if (e.ChangedButton == MouseButton.Left) // Left button was double clicked
            {
                ListViewItem selected = (ListViewItem)lvPodEpisodes.SelectedItem;
                string[] epInfo = (string[])selected.Tag;

                Uri downloadUrl = new Uri(epInfo[2]);



                List<Episode> downloading = new List<Episode>();
                downloading.Add(new Episode() { Title = epInfo[0], Progress = "0%" });
                lvPodDownloads.Items.Add((new Episode() { Title = epInfo[0], Progress = "0%" }));
                using (WebClient client = new WebClient())
                {
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(downloadProgress/*(downloading, 1)*/);
                }
            }
        }

        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //e.ProgressPercentage;
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            MessageBox.Show("Download completed!");
        }

        private void downloadProgress(/*List<Episode> downloading, int index, */object sender, DownloadProgressChangedEventArgs e)
        {
            
        }
        #endregion


    }

    public class Episode
    {
        public string Title { get; set; }
        public string Progress { get; set; }
    }

}
//TODO: add an about page with the version creator etc
