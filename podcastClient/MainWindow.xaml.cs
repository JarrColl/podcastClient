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


namespace podcastClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        public MainWindow()
        {
            InitializeComponent();
            
        }

        private void btnManualAdd_Click(object sender, RoutedEventArgs e)
        {
            manualAdd winMA = new manualAdd();
            winMA.Show();
        }

        private void btnDownloads_Click(object sender, RoutedEventArgs e)
        {
            refreshFeeds(lvPodFeeds); //Remove later
        }

        public static void refreshFeeds(ListView lv) //Adds the feeds from the xml file to the list view
        {
            XmlDocument xmlFeeds = new XmlDocument();
            xmlFeeds.Load("feeds.xml"); // Load xml from the subbed feeds xml

            XmlNodeList nodesFeeds = xmlFeeds.SelectNodes("//feeds/podcast");


            foreach(XmlNode feed in nodesFeeds)
            {
                string strTitle = feed.SelectSingleNode("title").InnerText;
                string strDesc = feed.SelectSingleNode("description").InnerText;
                string strUrl = feed.SelectSingleNode("url").InnerText;
                string strImageName = feed.SelectSingleNode("imageName").InnerText;

                string[] listFeedInfo = { strTitle, strDesc, strUrl, strImageName };
                ListViewItem item = new ListViewItem();
                item.Content = strTitle;

                item.Tag = listFeedInfo; // Associates the feeds information with the listview item so it can be easily accessed later
                lv.Items.Add(item);

            }

        }
        #region Subs listview
        private void lvPodFeeds_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPodFeeds.SelectedItems.Count == 1) // Check if an item is selected just to be safe
            {
                ListViewItem item = (ListViewItem)lvPodFeeds.SelectedItem;
                string[] arrInfo = (string[])item.Tag;
                lblTitle.Content = arrInfo[0];
                txtDesc.Text = arrInfo[1];

                imgFeedImage.Source = new BitmapImage(new Uri((Environment.CurrentDirectory + "\\..\\..\\feedImages\\" + arrInfo[3])));
            }
        }

        private void lvPodFeeds_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string strTitle;
            string strDesc;
            string strUrl;

            ListViewItem selected = (ListViewItem)lvPodFeeds.SelectedItem;
            string[] arrInfo = (string[])selected.Tag;
            //XmlReader xmlFeed = XmlReader.Create(arrInfo[2]);
            string[] epInfo = new string[4];



            XmlDocument xmlFeed = new XmlDocument();
            XmlDocument xmlSubbedFeeds = new XmlDocument();

            xmlFeed.Load(arrInfo[2]); //Load xml of rss feed
            xmlSubbedFeeds.Load("feeds.xml");

            // {Title, Desc, Url, ImageName}
            XmlNodeList xmlEpisodes = xmlFeed.SelectNodes("//rss/channel/item");

            foreach (XmlNode episode in xmlEpisodes)
            {
                strTitle = episode.SelectSingleNode("title").InnerText;
                strDesc = episode.SelectSingleNode("description").InnerText;
                strUrl = episode.SelectSingleNode("enclosure").Attributes["url"].Value;

                xmlSubbedFeeds.SelectNodes("//feeds/podcast");



                string[] listEpisodesInfo = { strTitle, strDesc, strUrl };
                ListViewItem item = new ListViewItem();
                item.Content = strTitle;
                item.Tag = listEpisodesInfo; // Associates the feeds information with the listview item so it can be easily accessed later
                lvPodEpisodes.Items.Add(item);
            }



        }
        #endregion

        #region Downloads Listview
        private void lvPodDownloads_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        #endregion

    }



}
//TODO: add an about page with the version creator etc
//      Fix podcast/download names getting cut off