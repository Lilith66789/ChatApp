using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatHost : MonoBehaviour
{
    public TMP_InputField hostIPInput; //UI for host settings and UI buttons
    public TMP_InputField hostPortInput;
    public Button startHostButton;
    public Button stopHostButton;

    public TMP_InputField usernameInput; //UI for name and chat messages
    public TMP_InputField messageInput;
    public Button sendButton;
    public TMP_Text chatOutput;

    private TcpListener listener;
    private bool isHosting = false;

    private List<TcpClient> clients = new List<TcpClient>();
    private List<StreamWriter> writers = new List<StreamWriter>();
    private List<StreamReader> readers = new List<StreamReader>();

    void Start()
    {
        startHostButton.onClick.AddListener(StartHostClicked); //button functions for use with UI
        stopHostButton.onClick.AddListener(StopHostClicked);
        sendButton.onClick.AddListener(SendHostMessage);

        if (string.IsNullOrEmpty(hostPortInput.text)) hostPortInput.text = "13000";//sets default port and username
        if (string.IsNullOrEmpty(usernameInput.text)) usernameInput.text = "Host";
    }

    void Update()
    {
        if (isHosting) AcceptClients();
        if (isHosting) ReadClientMessages();
    }

    void OnApplicationQuit()
    {
        StopHosting(); //ensures hosting stops when app is closed
    }

    void StartHostClicked()
    {
        if (isHosting) return; //prevents host hosting multiple times

        int port;
        if (!int.TryParse(hostPortInput.text, out port))
        {
            AppendChat("Invalid port.");
            return;
        }

        IPAddress ip = IPAddress.Any; //default for ip

        if (!string.IsNullOrWhiteSpace(hostIPInput.text)) //checks inputed ip and uses it
        {
            IPAddress.TryParse(hostIPInput.text, out ip);
            if (ip == null) ip = IPAddress.Any; 
        }

        listener = new TcpListener(ip, port); //starts server with ip and port
        listener.Start();
        isHosting = true;

        AppendChat($"[Host] Listening on {ip}:{port}"); //prints message with ip and port beinghosted on
    }

    void StopHostClicked()
    {
        StopHosting();
    }

    private void StopHosting()
    {
        if (!isHosting) return;
        isHosting = false;

        foreach (var c in clients) c.Close(); //disconnects all clients
        clients.Clear();
        writers.Clear();
        readers.Clear();

        listener.Stop(); 
        AppendChat("[Host] Stopped hosting.");
    }

    private void AcceptClients()
    {
        if (!listener.Pending()) return;

        TcpClient client = listener.AcceptTcpClient(); //accepts new clients
        clients.Add(client);

        writers.Add(new StreamWriter(client.GetStream()) { AutoFlush = true });
        readers.Add(new StreamReader(client.GetStream()));

        AppendChat("[Host] Client connected.");
    }

    private void ReadClientMessages()
    {
        for (int i = clients.Count - 1; i >= 0; i--)
        {
            TcpClient c = clients[i];
            Socket s = c.Client;

            bool disconnected = s.Poll(1, SelectMode.SelectRead) && s.Available == 0;
            if (disconnected)
            {
                c.Close();
                clients.RemoveAt(i);
                writers.RemoveAt(i);
                readers.RemoveAt(i);
                AppendChat("[Host] Client disconnected.");
                continue;
            }

            if (s.Available > 0)
            {
                string msg = readers[i].ReadLine();
                if (msg != null)
                {
                    AppendChat(msg); //display message to host
                    Broadcast(msg); //sends message to connected client
                }
            }
        }
    }

    private void Broadcast(string message)
    {
        for (int i = 0; i < writers.Count; i++)
            writers[i].WriteLine(message);
    }

    void SendHostMessage()
    {
        if (!isHosting) return;

        string user = usernameInput.text.Trim();
        if (user == "") user = "Host";

        string msg = messageInput.text.Trim();
        if (msg == "") return;

        string formatted = $"{user}: {msg}"; //formats message with username followed by message content
        AppendChat(formatted);
        Broadcast(formatted);

        messageInput.text = "";
    }

    private void AppendChat(string text)
    {
        chatOutput.text += text + "\n";
    }
}