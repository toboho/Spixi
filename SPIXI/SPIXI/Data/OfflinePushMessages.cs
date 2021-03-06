﻿using IXICore;
using IXICore.Meta;
using Newtonsoft.Json;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web;

namespace SPIXI
{
    public class OfflinePushMessages
    {
        private static long lastUpdate = 0;
        private static int cooldownPeriod = 60; // cooldown period in seconds


        public static bool sendPushMessage(StreamMessage msg, bool push)
        {
            string receiver = Base58Check.Base58CheckEncoding.EncodePlain(msg.recipient);
            string sender = Base58Check.Base58CheckEncoding.EncodePlain(msg.sender);
            string data = HttpUtility.UrlEncode(Convert.ToBase64String(msg.getBytes()));

            string pub_key = "";

            if (msg.id.Length == 1 && msg.id[0] == 1)
            {
                pub_key = HttpUtility.UrlEncode(Convert.ToBase64String(Node.walletStorage.getPrimaryPublicKey()));
            }


            Friend f = FriendList.getFriend(msg.recipient);
            if (f == null)
                return true; // return true to skip sending this message and remove it from the queue

            string URI = String.Format("{0}/push.php", Config.pushServiceUrl);
            string parameters = String.Format("tag={0}&data={1}&pk={2}&push={3}&fa={4}", receiver, data, pub_key, push, sender);

            using (WebClient client = new WebClient())
            {
                try
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    string htmlCode = client.UploadString(URI, parameters);
                    if (htmlCode.Equals("OK"))
                    {
                        if (msg.id.Length == 1 && msg.id[0] >= f.handshakeStatus)
                        {
                            f.handshakePushed = true;

                            FriendList.saveToStorage();
                        }
                        return true;
                    }
                }catch(Exception e)
                {
                    Logging.error("Exception occured in sendPushMessage: " + e);
                }
            }
            return false;
        }

        public static bool fetchPushMessages(bool force = false)
        {
            if(force == false && lastUpdate + cooldownPeriod > Clock.getTimestamp())
            {
                return false;
            }

            lastUpdate = Clock.getTimestamp();

            try
            {
                string URI = String.Format("{0}/fetch.php", Config.pushServiceUrl);
                string unique_uri = String.Format("{0}/uniqueid.php", Config.pushServiceUrl);

                string receiver = Base58Check.Base58CheckEncoding.EncodePlain(Node.walletStorage.getPrimaryAddress());
        
                WebClient uclient = new WebClient();
                byte[] checksum = Convert.FromBase64String(uclient.DownloadString(unique_uri));

                byte[] sig = CryptoManager.lib.getSignature(checksum, Node.walletStorage.getPrimaryPrivateKey());

                using (WebClient client = new WebClient())
                {
                    string url = String.Format("{0}?tag={1}&sig={2}", URI, receiver, HttpUtility.UrlEncode(Convert.ToBase64String(sig)));
                    string htmlCode = client.DownloadString(url);
                    Logging.info("fetchPushMessages: {0}", htmlCode);

                    if (htmlCode == "FALSE")
                        return false;

                    lastUpdate = 0; // If data was available, fetch it again without cooldown

                    List<string[]> jsonResponse = JsonConvert.DeserializeObject<List<string[]>>(htmlCode);

                    foreach (string[] str in jsonResponse)
                    {
                        try
                        {
                            byte[] data = Convert.FromBase64String(str[0]);
                            if (str[1] != "")
                            {
                                byte[] pk = Convert.FromBase64String(str[1]);
                                Friend f = FriendList.getFriend(new Address(pk).address);
                                if (f != null)
                                {
                                    f.publicKey = pk;
                                }
                                FriendList.saveToStorage();
                            }
                            StreamProcessor.receiveData(data, null);
                        }
                        catch(Exception e)
                        {
                            Logging.error(string.Format("Exception occured in fetchPushMessages while parsing the json response. {0}", e));
                        }
                    }

                }
            }
            catch (Exception e)
            {
                Logging.error(string.Format("Exception occured in fetchPushMessages. {0}", e));
                return false;
            }

            return true;
        }
    }
}
