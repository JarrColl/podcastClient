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

namespace podcastClient
{
    /// <summary>
    /// Interaction logic for manualAdd.xaml
    /// </summary>
    public partial class manualAdd : Window
    {
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

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            strUrl = txtUrl.Text;

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
                            client.DownloadFileAsync(new Uri(strFeedImage), (Environment.CurrentDirectory + "\\..\\..\\feedImages\\" + strImageName));
                        }
                    }
                    else
                    {
                        strFeedImage = ""; 
                        strImageName = ""; // Handles the non existent image path when setting the image source
                    }

                    if (!File.Exists("feeds.xml")) // For safety
                    {
                        var file = File.Create("feeds.xml");
                        file.Close();
                        File.WriteAllText("feeds.xml", "<?xml version=\"1.0\"?>" + Environment.NewLine + "<feeds>\n</feeds>"); // Setting up xml file
                    }


                    XDocument xmlFeeds = XDocument.Load("feeds.xml");
                    XElement podcast = new XElement("podcast", //Creating an element with all feed information
                        new XElement("title", strFeedTitle),
                        new XElement("description", strFeedDesc),
                        new XElement("url", strUrl),
                        new XElement("image", strFeedImage),
                        new XElement("imageName", strImageName));
                    podcast.SetAttributeValue("title", strFeedTitle);
                    xmlFeeds.Root.Add(podcast); // Adding the element to the feeds.xml file
                    xmlFeeds.Save("feeds.xml");


                    MainWindow.refreshFeeds(); // Add the podcast to the main list view
                    this.Close();
                }

            }
            catch (Exception)
            {
                MessageBox.Show("Not a valid URL!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        
        }
    }
}
