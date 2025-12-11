using System;
using System.Net.Sockets;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatClient : MonoBehaviour
{
    public TMP_InputField ClientIP; //UI for client connect settings nad buttons
    public TMP_InputField ClientPort;
    public Button connectButton;
    public Button disconnectButton;

    public TMP_InputField Username;// UI for messaging and username input
    public TMP_InputField messageInput;
    public Button sendButton;
    public TMP_Text chatOutput;

    private TcpClient client;
    private StreamWriter writer;
    private StreamReader reader;
    private bool isConnected = false;

    void Start()
    {
        connectButton.onClick.AddListener(ConnectClicked); //used for buttons within UI
        disconnectButton.onClick.AddListener(DisconnectClicked);
        sendButton.onClick.AddListener(SendMessageClicked);

        if (string.IsNullOrEmpty(ClientPort.text)) ClientPort.text = "13000"; //defaults for name and port
        if (string.IsNullOrEmpty(Username.text)) Username.text = "Client";
    }

    void Update()
    {
        if (!isConnected || client == null) return; //checks for messages if user is connected

        Socket socket = client.Client;

        if (socket.Available > 0) 
        {
            string message = reader.ReadLine(); //displays messages
            if (message != null)
                AppendChat(message);
        }

        bool disconnected = socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0; //disconnects user if server closes and tells them
        if (disconnected)
        {
            AppendChat("[Client] Disconnected.");
            Disconnect();
        }
    }

    void ConnectClicked()
    {
        if (isConnected) return; //prevent user connecting multiple times

        string ip = string.IsNullOrWhiteSpace(ClientIP.text) ? "127.0.0.1" : ClientIP.text.Trim(); //default IP
        int port;

        if (!int.TryParse(ClientPort.text.Trim(), out port))
        {
            AppendChat("[Client] Invalid port.");
            return;
        }

        client = new TcpClient();
        client.Connect(ip, port);
        writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
        reader = new StreamReader(client.GetStream());
        isConnected = true;

        AppendChat("[Client] Connected to {ip}:{port}");
    }

    void DisconnectClicked()
    {
        Disconnect();
    }

    private void Disconnect()
    {
        if (!isConnected) return;

        isConnected = false;
        if (client != null)
        {
            client.Close();
            client = null;
        }
    }

    void SendMessageClicked()
    {
        if (!isConnected) return;

        string user = Username.text.Trim();
        if (user == "") user = "Client";

        string msg = messageInput.text.Trim();
        if (msg == "") return;

        writer.WriteLine("{user}: {msg}");
        messageInput.text = "";
    }

    private void AppendChat(string text)
    {
        chatOutput.text += text + "\n";
    }
}