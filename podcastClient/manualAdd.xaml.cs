﻿using System;
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
            strUrl = txtUrl.Text; //TODO: VALIDATE INPUT

            XmlDocument xmlNew = new XmlDocument();
            xmlNew.Load(@strUrl); // Load xml from the RSS feed

            XmlNode xmlTitle = xmlNew.SelectSingleNode("//rss/channel/title");
            XmlNode xmlDesc = xmlNew.SelectSingleNode("//rss/channel/description"); // Collect Relevant information from the external xml file
            XmlNodeList nodes = xmlNew.GetElementsByTagName("itunes:image");


            strFeedTitle = xmlTitle.InnerText;
            strFeedDesc = xmlDesc.InnerText;
            strFeedImage = nodes[0].Attributes["href"].Value;
            strImageName = reImageName.Match(strFeedImage).ToString();
            // add the feed to the new xml file
            XDocument xmlFeeds = XDocument.Load("feeds.xml");
            XElement podcast = new XElement("podcast",
                new XElement("title", strFeedTitle),
                new XElement("description", strFeedDesc),
                new XElement("url", strUrl),
                new XElement("image", strFeedImage),
                new XElement("imageName", strImageName));
            xmlFeeds.Root.Add(podcast);
            xmlFeeds.Save("feeds.xml");


            using (var client = new WebClient())
            {
                client.DownloadFileAsync(new Uri(strFeedImage), (Environment.CurrentDirectory + "\\..\\..\\feedImages\\" + strImageName));
            }



            this.Close();
        }
    }
}
