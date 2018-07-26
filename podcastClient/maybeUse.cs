//while (xmlFeed.Read())
//{
//    if((xmlFeed.NodeType == XmlNodeType.Element) && (xmlFeed.Name == "item"))
//    {
//        if (xmlFeed.IsStartElement())

//        {

//            //return only when you have START tag

//            switch (xmlFeed.Name.ToString())

//            {
//                case "item":
//                    epInfo = new string[4];
//                    break;

//                case "title":
//                    epInfo[0] = xmlFeed.ReadString();
//                    break;

//                case "description":
//                    epInfo[1] = xmlFeed.ReadString();
//                    break;

//                case "enclosure":
//                    epInfo[2] = xmlFeed.ReadString();
//                    break;

//                case "itunes:duration":
//                    epInfo[3] = xmlFeed.ReadString();
//                    break;
//            }
//        }
//    }

//}
//MessageBox.Show(epInfo[0]);

