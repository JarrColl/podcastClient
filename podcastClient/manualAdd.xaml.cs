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
using System.Windows.Shapes;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace podcastClient
{

    public partial class manualAdd : Window
    {
        static string strFeedsXMLPath = "xml\\feeds.xml";
        static string strFeedImagesDirPath = Environment.CurrentDirectory + "\\images\\";

        // Feed info variables
        string strFeedTitle;
        string strFeedDesc;
        string strFeedImage;
        string strImageName;
        Regex reImageName = new Regex(@"[^/\\&\?]+\.\w{3,4}(?=([\?&].*$|$))"); //The text after the last forward slash (file name)


        string strUrl;
        public manualAdd()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private readonly BackgroundWorker worker = new BackgroundWorker();

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            strUrl = txtUrl.Text;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(strUrl);
            Mouse.OverrideCursor = Cursors.Wait;
            this.Close();


        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string strUrl = (string)e.Argument;
            XmlDocument xmlNew = new XmlDocument();

            try //For when the user does not enter a valid url with an xml file
            {

                xmlNew.Load(@strUrl); // Load xml from the RSS feed

                XmlNode xmlTitle = xmlNew.SelectSingleNode("//rss/channel/title");
                XmlNode xmlDesc = xmlNew.SelectSingleNode("//rss/channel/description"); // Collect Relevant information from the external xml file
                XmlNodeList imageNodes = xmlNew.GetElementsByTagName("itunes:image");

                if (xmlTitle != null) // If there is no title then do not add the podcast
                {
                    strFeedTitle = xmlTitle.InnerText;
                    if (xmlDesc != null) // If there is no description then leave it blank
                    {
                        strFeedDesc = xmlDesc.InnerText;
                    }
                    else
                    {
                        strFeedDesc = "";
                    }

                    if (imageNodes != null && imageNodes[0].Attributes["href"] != null) // If there is no image then leave it blank
                    {
                        strFeedImage = imageNodes[0].Attributes["href"].Value;
                        strImageName = reImageName.Match(strFeedImage).ToString();

                        using (var client = new WebClient())
                        {
                            client.DownloadFileAsync(new Uri(strFeedImage), (strFeedImagesDirPath + strImageName));
                        }
                    }
                    else
                    {
                        strFeedImage = "";
                        strImageName = ""; // Handles the non existent image path when setting the image source
                    }

                    if (!File.Exists(strFeedsXMLPath)) // For safety
                    {
                        var file = File.Create(strFeedsXMLPath);
                        file.Close();
                        File.WriteAllText(strFeedsXMLPath, "<?xml version=\"1.0\"?>" + Environment.NewLine + "<feeds>\n</feeds>"); // Setting up xml file
                    }

                    XDocument xmlFeeds = XDocument.Load(strFeedsXMLPath);
                    XElement xelPodcast = new XElement("podcast", //Creating an element with all feed information
                        new XElement("title", strFeedTitle),
                        new XElement("description", strFeedDesc),
                        new XElement("url", strUrl),
                        new XElement("image", strFeedImage),
                        new XElement("imageName", strImageName));
                    xelPodcast.SetAttributeValue("title", strFeedTitle);
                    xmlFeeds.Root.Add(xelPodcast); // Adding the element to the feeds.xml file
                    xmlFeeds.Save(strFeedsXMLPath);

                    this.Dispatcher.Invoke(() => 
                    {
                        MainWindow.refreshFeeds();

                    }); // Add the podcast to the main list view

                }

            }
            catch (Exception) // If the input is not a link or the link does not contain an rss feed (pretty much the only way to do it is with try-catch)
            {
                MessageBox.Show("There was an Error, Check your URL!\nEnsure you included the https://", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }
    }
}
