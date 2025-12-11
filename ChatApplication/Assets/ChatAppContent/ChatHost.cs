using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatHost : MonoBehaviour
{
    [Header("Host Settings (TextMeshPro Input Fields)")]
    public TMP_InputField hostIPInput;   //ip for hosting chat
    public TMP_InputField hostPortInput; //port number for hosting chat
    public Button startHostButton;
    public Button stopHostButton;

    [Header("Chat UI")]
    public TMP_InputField usernameInput;
    public TMP_InputField messageInput;
    public Button sendButton;
    public TMP_Text chatOutput;

    private TcpListener listener;
    private Thread listenerThread;
    private volatile bool isHosting = false; //track current host status


    private readonly List<TcpClient> clients = new List<TcpClient>();
    private readonly List<StreamWriter> writers = new List<StreamWriter>();
    private readonly object clientsLock = new object();

    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>(); //pass messages to main thread

    void Start()
    {
        startHostButton.onClick.AddListener(StartHostClicked);
        stopHostButton.onClick.AddListener(StopHostClicked); //for use with UI buttons
        sendButton.onClick.AddListener(SendHostMessage);

        if (hostPortInput != null && string.IsNullOrEmpty(hostPortInput.text)) hostPortInput.text = "13000"; //sets default port and name 
        if (usernameInput != null && string.IsNullOrEmpty(usernameInput.text)) usernameInput.text = "Host";
    }

    void Update()
    {
        while (incomingMessages.TryDequeue(out var msg))
        {
            AppendChat(msg); //prints queued messages to chat
        }
    }

    void OnApplicationQuit()
    {
        StopHosting(); //stops hosting when the application is closed
    }

    
    public void StartHostClicked()
    {
        if (isHosting) return; //stops multiple host at the same time
        int port = 13000;
        if (!int.TryParse(hostPortInput.text, out port))
        {
            AppendChat("[Host] Invalid port.");
            return;
        }

        IPAddress ip = IPAddress.Any;
        if (!string.IsNullOrWhiteSpace(hostIPInput.text))
        {
            if (!IPAddress.TryParse(hostIPInput.text.Trim(), out ip))
            {
                AppendChat("[Host] Invalid IP; using Any.");
                ip = IPAddress.Any;
            }
        }

        StartHosting(ip, port); //starts server
    }

    public void StopHostClicked()
    {
        StopHosting(); //For use with UI button to stop hosting
    }

    public void SendHostMessage()
    {
        if (!isHosting)
        {
            AppendChat("[Host] Not hosting.");
            return;
        }

        string username = usernameInput.text.Trim();
        if (string.IsNullOrEmpty(username)) username = "Host"; //checks username 

        string message = messageInput.text.Trim(); //gets message from input box
        if (string.IsNullOrEmpty(message)) return; 

        string formatted = $"{username}: {message}"; //formats message with host name followed by message text

        AppendChat(formatted);

        Broadcast(formatted);

        messageInput.text = string.Empty;
    }


    public void StartHosting(IPAddress ip, int port)
    {
        try
        {
            listener = new TcpListener(ip, port);
            listener.Start();
            isHosting = true;
            listenerThread = new Thread(ListenerLoop) { IsBackground = true }; //starts thread which takes new connections
            listenerThread.Start();
            AppendChat($"[Host] Listening on {ip}:{port}");
        }
        catch (Exception ex)
        {
            AppendChat("[Host] Failed to start: " + ex.Message); //catch for errors and print error message
        }
    }

    private void ListenerLoop()
    {
        try
        {
            while (isHosting)
            {
                TcpClient newClient = listener.AcceptTcpClient();
                lock (clientsLock)
                {
                    clients.Add(newClient);
                    var writer = new StreamWriter(newClient.GetStream()) { AutoFlush = true };
                    writers.Add(writer);
                }
                AppendChatEnqueue("[Host] Client connected: " + newClient.Client.RemoteEndPoint); //displays message for new client connected

                Thread clientThread = new Thread(() => ClientReadLoop(newClient)) { IsBackground = true }; //threading for handling client messages
                clientThread.Start();
            }
        }
        catch (SocketException)
        {
            
        }
        catch (Exception ex)
        {
            AppendChatEnqueue("[Host] Listener error: " + ex.Message); //catch for errors and print error message
        }
    }

    private void ClientReadLoop(TcpClient client)
    {
        try
        {
            using (var reader = new StreamReader(client.GetStream()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {

                    Broadcast(line);
                    AppendChatEnqueue(line);
                }
            }
        }
        catch (IOException)
        {
            
        }
        catch (Exception ex)
        {
            AppendChatEnqueue("[Host] ClientRead error: " + ex.Message);
        }
        finally
        {
            RemoveClient(client);
            AppendChatEnqueue("[Host] Client disconnected."); //disconnects clients and displays disconnect message
        }
    }


    private void Broadcast(string message) //broadcasting messages
    {
        lock (clientsLock)
        {
            for (int i = writers.Count - 1; i >= 0; i--)
            {
                var writer = writers[i]; 
                try
                {
                    writer.WriteLine(message); //sends message with catch for removing error messages
                }
                catch
                {

                    writers.RemoveAt(i);
                    clients[i].Close();
                    clients.RemoveAt(i);
                }
            }
        }
    }

    private void RemoveClient(TcpClient client) //removes client when disconnected
    {
        lock (clientsLock) //makes sure one thread modifies clients and writers
        {
            int index = clients.IndexOf(client); //sets index as disconnected client value
            if (index >= 0)
            {
                clients[index].Close(); //closes connection of targeted client
                clients.RemoveAt(index); //removes client from list
                if (index < writers.Count) writers.RemoveAt(index);
            }
        }
    }

    public void StopHosting()
    {
        if (!isHosting) return; //prevents run if isn't hosting
        isHosting = false;
        try
        {
            listener?.Stop();
        }
        catch { }

        lock (clientsLock)
        {
            foreach (var c in clients) try { c.Close(); } catch { } //closes all client connections when hosting is stopped
            clients.Clear();
            writers.Clear(); //clears chat when hosting stops to remove messages if chat is re-hosted
        }

        AppendChatEnqueue("[Host] Stopped hosting.");
    }

   
    private void AppendChat(string text)
    {
        if (chatOutput == null) return;
        chatOutput.text += text + "\n";
    }

    private void AppendChatEnqueue(string text)
    {
        incomingMessages.Enqueue(text); //queues new chat messages
    }
}