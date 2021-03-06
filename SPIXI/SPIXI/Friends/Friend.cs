﻿using IXICore;
using IXICore.Meta;
using IXICore.Network;
using IXICore.Utils;
using SPIXI.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SPIXI
{
    public enum FriendMessageType
    {
        standard,
        requestAdd,
        requestFunds,
        sentFunds,
        fileHeader
    }


    public class FriendMessage
    {
        private byte[] _id;
        public string message;
        public long timestamp; // timestamp as specified by the sender
        public bool localSender;
        public bool read;
        public bool confirmed;
        public FriendMessageType type;
        public string transferId; // UID of file transfer if applicable
        public bool completed; // for file transfer, indicating whether the transfer completed
        public string filePath; // for file transfer
        public ulong fileSize; // for file transfer

        public byte[] senderAddress;
        public string senderNick = "";

        public long receivedTimestamp; // timestamp of when the message was received; used for storage purposes

        public FriendMessage(byte[] id, string msg, long time, bool local_sender, FriendMessageType t, byte[] sender_address = null, string sender_nick = "")
        {
            _id = id;
            message = msg;
            timestamp = time;
            localSender = local_sender;
            read = false;
            type = t;
            confirmed = false;
            senderAddress = sender_address;
            senderNick = sender_nick;
            transferId = "";
            completed = false;
            filePath = "";
            fileSize = 0;
            receivedTimestamp = Clock.getTimestamp();
        }

        public FriendMessage(string msg, long time, bool local_sender, FriendMessageType t, byte[] sender_address = null, string sender_nick = "")
        {
            message = msg;
            timestamp = time;
            localSender = local_sender;
            read = false;
            type = t;
            confirmed = false;
            senderAddress = sender_address;
            senderNick = sender_nick;
            transferId = "";
            completed = false;
            filePath = "";
            fileSize = 0;
            receivedTimestamp = Clock.getTimestamp();
        }

        public FriendMessage(byte[] bytes)
        {
            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int id_len = reader.ReadInt32();
                    if (id_len > 0)
                    {
                        _id = reader.ReadBytes(id_len);
                    }
                    type = (FriendMessageType)reader.ReadInt32();
                    message = reader.ReadString();
                    timestamp = reader.ReadInt64();
                    localSender = reader.ReadBoolean();
                    read = reader.ReadBoolean();
                    confirmed = reader.ReadBoolean();

                    int sender_address_len = reader.ReadInt32();
                    if (sender_address_len > 0)
                    {
                        senderAddress = reader.ReadBytes(sender_address_len);
                    }

                    senderNick = reader.ReadString();

                    transferId = reader.ReadString();

                    completed = reader.ReadBoolean();

                    filePath = reader.ReadString();
                    fileSize = reader.ReadUInt64();

                    receivedTimestamp = reader.ReadInt64();
                }
            }

        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(id.Length);
                    writer.Write(id);
                    writer.Write((int)type);
                    writer.Write(message);
                    writer.Write(timestamp);
                    writer.Write(localSender);
                    writer.Write(read);
                    writer.Write(confirmed);

                    if (senderAddress != null)
                    {
                        writer.Write(senderAddress.Length);
                        writer.Write(senderAddress);
                    }
                    else
                    {
                        writer.Write((int)0);
                    }

                    writer.Write(senderNick);

                    writer.Write(transferId);
                    writer.Write(completed);

                    writer.Write(filePath);
                    writer.Write(fileSize);

                    writer.Write(receivedTimestamp);
                }
                return m.ToArray();
            }
        }


        public byte[] id
        {
            get
            {
                if (_id == null)
                {
                    _id = Guid.NewGuid().ToByteArray(); // Generate a new unique id
                }
                return _id;
            }
            set
            {
                _id = value;
            }
        }
    }

    // Helper message class used for communicating with the UI
    public class FriendMessageHelper
    {
        public string walletAddress;
        public string nickname;
        public long timestamp;
        public string avatar;
        public string onlineString;
        public string excerpt;
        public int unreadCount;

        public FriendMessageHelper(string wa, string nick, long time, string av, string online, string ex, int unread)
        {
            walletAddress = wa;
            nickname = nick;
            timestamp = time;
            avatar = av;
            onlineString = online;
            excerpt = ex;
            unreadCount = unread;
        }

    }

    public class BotContact
    {
        public string nick = "";
        public byte[] publicKey;

        public BotContact()
        {

        }

        public BotContact(string nick, byte[] public_key)
        {
            this.nick = nick;
            publicKey = public_key;
        }

        public BotContact(byte[] contact_bytes)
        {
            using (MemoryStream m = new MemoryStream(contact_bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    nick = reader.ReadString();

                    int pk_length = reader.ReadInt32();
                    if (pk_length > 0)
                    {
                        publicKey = reader.ReadBytes(pk_length);
                    }
                }
            }
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(nick);

                    if(publicKey == null)
                    {
                        writer.Write((int)0);
                    }else
                    {
                        writer.Write(publicKey.Length);
                        writer.Write(publicKey);
                    }
                }
                return m.ToArray();
            }
        }
    }

    public class Friend
    {
        public byte[] walletAddress;
        public byte[] publicKey;

        private string _nick = "";

        public byte[] chachaKey = null; // TODO TODO don't keep keys in plaintext in memory
        public byte[] aesKey = null; // TODO TODO don't keep keys in plaintext in memory
        public long keyGeneratedTime = 0;

        public string relayIP = null;
        public byte[] relayWallet = null;

        public bool online = false;

        public List<FriendMessage> messages = new List<FriendMessage>();

        public Dictionary<byte[], BotContact> contacts = new Dictionary<byte[], BotContact>(new ByteArrayComparer()); // used by bot friends

        public SingleChatPage chat_page = null;

        public bool approved = true;

        public bool bot = false;

        private int _handshakeStatus = 0;

        public bool handshakePushed = false;

        public byte[] lastReceivedMessageId = null; // Used primarily for bot purposes

        public long lastReceivedHandshakeMessageTimestamp = 0;

        public Friend(byte[] wallet, byte[] public_key, string nick, byte[] aes_key, byte[] chacha_key, long key_generated_time, bool approve = true)
        {
            walletAddress = wallet;
            publicKey = public_key;
            nickname = nick;
            approved = approve;

            chachaKey = chacha_key;
            aesKey = aes_key;
            keyGeneratedTime = key_generated_time;
        }

        public Friend(byte[] bytes)
        {

            using (MemoryStream m = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    int wal_length = reader.ReadInt32();
                    walletAddress = reader.ReadBytes(wal_length);

                    int pkey_length = reader.ReadInt32();
                    if (pkey_length > 0)
                    {
                        publicKey = reader.ReadBytes(pkey_length);
                    }

                    _nick = reader.ReadString(); // use internal variable, to avoid writing to file

                    int aes_len = reader.ReadInt32();
                    if (aes_len > 0)
                    {
                        aesKey = reader.ReadBytes(aes_len);
                    }

                    int cc_len = reader.ReadInt32();
                    if (cc_len > 0)
                    {
                        chachaKey = reader.ReadBytes(cc_len);
                    }

                    keyGeneratedTime = reader.ReadInt64();

                    approved = reader.ReadBoolean();

                    _handshakeStatus = reader.ReadInt32(); // use internal variable, to avoid writing to file

                    bot = reader.ReadBoolean();
                    handshakePushed = reader.ReadBoolean();

                    int num_contacts = reader.ReadInt32();
                    for (int i = 0; i < num_contacts; i++)
                    {
                        int contact_len = reader.ReadInt32();

                        BotContact contact = new BotContact(reader.ReadBytes(contact_len));
                        contacts.Add(new Address(contact.publicKey).address, contact);
                    }

                    int rcv_msg_id_len = reader.ReadInt32();
                    if (rcv_msg_id_len > 0)
                    {
                        lastReceivedMessageId = reader.ReadBytes(rcv_msg_id_len);
                    }

                    lastReceivedHandshakeMessageTimestamp = reader.ReadInt64();
                }
            }
        }

        public byte[] getBytes()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {

                    writer.Write(walletAddress.Length);
                    writer.Write(walletAddress);
                    if (publicKey != null)
                    {
                        writer.Write(publicKey.Length);
                        writer.Write(publicKey);
                    }
                    else
                    {
                        writer.Write(0);
                    }
                    writer.Write(nickname);

                    // encryption keys
                    if (aesKey != null)
                    {
                        writer.Write(aesKey.Length);
                        writer.Write(aesKey);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    if (chachaKey != null)
                    {
                        writer.Write(chachaKey.Length);
                        writer.Write(chachaKey);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    writer.Write(keyGeneratedTime);

                    writer.Write(approved);

                    writer.Write(handshakeStatus);

                    writer.Write(bot);

                    writer.Write(handshakePushed);

                    int num_contacts = contacts.Count();
                    writer.Write(num_contacts);

                    foreach (var contact in contacts)
                    {
                        byte[] contact_bytes = contact.Value.getBytes();

                        writer.Write(contact_bytes.Length);
                        writer.Write(contact_bytes);
                    }

                    if (lastReceivedMessageId != null)
                    {
                        writer.Write(lastReceivedMessageId.Length);
                        writer.Write(lastReceivedMessageId);
                    }
                    else
                    {
                        writer.Write(0);
                    }

                    writer.Write(lastReceivedHandshakeMessageTimestamp);

                }
                return m.ToArray();
            }
        }

        // Get the number of unread messages
        // TODO: optimize this
        public int getUnreadMessageCount()
        {
            int unreadCount = 0;
            lock(messages)
            {
                for (int i = messages.Count - 1; i >= 0; i--)
                {
                    if (messages[i].read == true || messages[i].localSender == true)
                    {
                        break;
                    }
                    unreadCount++;
                }
            }
            return unreadCount;
        }

        // Flushes the temporary message history
        public bool flushHistory()
        {
            messages.Clear();
            return true;
        }

        // Deletes the history file and flushes the temporary history
        public bool deleteHistory()
        {

            if (Node.localStorage.deleteMessages(walletAddress) == false)
                return false;

            if (flushHistory() == false)
                return false;

            return true;
        }

        // Check if the last message is unread. Returns true if it is unread.
        public bool checkLastUnread()
        {
            if (messages.Count < 1)
                return false;
            FriendMessage last_message = messages[messages.Count - 1];
            if (last_message.read == false && !last_message.localSender)
                return true;

            return false;
        }

        public int getMessageCount()
        {
            return messages.Count;
        }

        // Set last message as read
        public void setLastRead()
        {
            if (messages.Count < 1)
                return;
            FriendMessage last_message = messages[messages.Count - 1];
            if (!last_message.localSender)
            {
                last_message.read = true;
            }
        }


        // Generates a random chacha key and a random aes key
        // Returns the two keys encrypted using the supplied public key
        // Returns false if not enough time has passed to generate the keys
        public bool generateKeys()
        {
            // TODO TODO TODO keys should be re-generated periodically
            try
            {
                if (aesKey == null)
                {
                    aesKey = CryptoManager.lib.getSecureRandomBytes(32);
                    return true;
                }

                if (chachaKey == null)
                {
                    chachaKey = CryptoManager.lib.getSecureRandomBytes(32);
                    return true;
                }
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during generate keys: {0}", e.Message));
            }

            return false;
        }

        public bool sendKeys(int selected_key)
        {
            try
            {
                using (MemoryStream m = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(m))
                    {
                        if (aesKey != null && selected_key != 2)
                        {
                            writer.Write(aesKey.Length);
                            writer.Write(aesKey);
                            Logging.info("Sending aes key");
                        }else
                        {
                            writer.Write(0);
                        }

                        if (chachaKey != null && selected_key != 1)
                        {
                            writer.Write(chachaKey.Length);
                            writer.Write(chachaKey);
                            Logging.info("Sending chacha key");
                        }
                        else
                        {
                            writer.Write(0);
                        }

                        Logging.info("Preparing key message");

                        SpixiMessage spixi_message = new SpixiMessage(SpixiMessageCode.keys, m.ToArray());

                        // Send the key to the recipient
                        StreamMessage sm = new StreamMessage();
                        sm.type = StreamMessageCode.info;
                        sm.recipient = walletAddress;
                        sm.sender = Node.walletStorage.getPrimaryAddress();
                        sm.transaction = new byte[1];
                        sm.sigdata = new byte[1];
                        sm.data = spixi_message.getBytes();
                        sm.encryptionType = StreamMessageEncryptionCode.rsa;
                        sm.id = new byte[] { 2 };

                        sm.sign(IxianHandler.getWalletStorage().getPrimaryPrivateKey());

                        StreamProcessor.sendMessage(this, sm);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during send keys: {0}", e.Message));
            }

            return false;
        }

        // Handles receiving and decryption of keys
        public bool receiveKeys(byte[] data)
        {
            try
            {
                Logging.info("Received keys");
                byte[] decrypted = data;

                using (MemoryStream m = new MemoryStream(data))
                {
                    using (BinaryReader reader = new BinaryReader(m))
                    {
                        // Read and assign the aes password
                        int aes_length = reader.ReadInt32();
                        byte[] aes = null;
                        if (aes_length > 0)
                        {
                            aes = reader.ReadBytes(aes_length);
                        }

                        // Read the chacha key
                        int cc_length = reader.ReadInt32();
                        byte[] chacha = null;
                        if (cc_length > 0)
                        {
                            chacha = reader.ReadBytes(cc_length);
                        }

                        if (aesKey == null)
                        {
                            aesKey = aes;
                        }

                        if (chachaKey == null)
                        {
                            chachaKey = chacha;
                        }

                        // Everything succeeded
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.error(String.Format("Exception during receive keys: {0}", e.Message));
            }

            return false;
        }

        // Retrieve the friend's connected S2 node address. Returns null if not found
        public string searchForRelay()
        {
            relayIP = null;
            relayWallet = null;

            string hostname = FriendList.getRelayHostname(walletAddress);

            if (hostname != null)
            {
                // Store the last relay ip and wallet for this friend
                relayIP = hostname;
            }
            // Finally, return the ip address of the node
            return relayIP;
        }

        public bool setMessageRead(byte[] id)
        {
            FriendMessage msg = messages.Find(x => x.id.SequenceEqual(id));
            if(msg == null)
            {
                Logging.error("Error trying to set read indicator, message does not exist");
                return false;
            }

            if (msg.localSender)
            {
                if (!msg.read)
                {
                    msg.read = true;
                    Node.localStorage.writeMessages(walletAddress, messages);
                }

                if(chat_page != null)
                {
                    chat_page.updateMessage(msg);
                }
            }

            return true;
        }

        public bool setMessageReceived(byte[] id)
        {
            FriendMessage msg = messages.Find(x => x.id.SequenceEqual(id));
            if (msg == null)
            {
                Logging.error("Error trying to set received indicator, message does not exist");
                return false;
            }

            if (msg.localSender)
            {
                if (!msg.confirmed)
                {
                    msg.confirmed = true;
                    Node.localStorage.writeMessages(walletAddress, messages);
                }

                if (chat_page != null)
                {
                    chat_page.updateMessage(msg);
                }
            }

            return true;
        }

        public int handshakeStatus
        {
            get
            {
                return _handshakeStatus;
            }
            set
            {
                if (_handshakeStatus != value)
                {
                    _handshakeStatus = value;
                    handshakePushed = false;
                    FriendList.saveToStorage();
                }
            }
        }

        public string nickname
        {
            get
            {
                return _nick;
            }
            set
            {
                if (_nick != value)
                {
                    _nick = value;
                    FriendList.saveToStorage();
                }
            }
        }
    }
}
