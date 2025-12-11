using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatClient : MonoBehaviour
{
    [Header("Client Settings (TextMeshPro Input Fields)")]
    public TMP_InputField clientIPInput;   // server IP to connect to
    public TMP_InputField clientPortInput; // server port
    public TMP_InputField usernameInput;
    public Button connectButton;
    public Button disconnectButton;

    [Header("Chat UI")]
    public TMP_InputField messageInput;
    public Button sendButton;
    public TMP_Text chatOutput;

    private TcpClient client;
    private StreamWriter writer;
    private Thread receiveThread;
    private volatile bool isConnected = false;

    private ConcurrentQueue<string> incomingMessages = new ConcurrentQueue<string>();

    void Start()
    {
        connectButton.onClick.AddListener(ConnectClicked);
        disconnectButton.onClick.AddListener(DisconnectClicked);
        sendButton.onClick.AddListener(SendMessageClicked);

        if (clientPortInput != null && string.IsNullOrEmpty(clientPortInput.text)) clientPortInput.text = "13000";
        if (usernameInput != null && string.IsNullOrEmpty(usernameInput.text)) usernameInput.text = "Client";
    }

    void Update()
    {
        while (incomingMessages.TryDequeue(out var msg))
        {
            chatOutput.text += msg + "\n";
        }
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    public void ConnectClicked()
    {
        if (isConnected) return;

        string ip = clientIPInput.text.Trim();
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
        if (!int.TryParse(clientPortInput.text.Trim(), out int port))
        {
            AppendChat("[Client] Invalid port.");
            return;
        }

        try
        {
            client = new TcpClient();
            client.Connect(ip, port);
            writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            isConnected = true;
            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();
            AppendChat("[Client] Connected to " + ip + ":" + port);
        }
        catch (Exception ex)
        {
            AppendChat("[Client] Connect failed: " + ex.Message);
        }
    }

    public void DisconnectClicked()
    {
        Disconnect();
    }

    public void SendMessageClicked()
    {
        if (!isConnected)
        {
            AppendChat("[Client] Not connected.");
            return;
        }

        string username = usernameInput.text.Trim();
        if (string.IsNullOrEmpty(username)) username = "Player";
        string message = messageInput.text.Trim();
        if (string.IsNullOrEmpty(message)) return;

        string formatted = $"{username}: {message}";

        try
        {
            writer.WriteLine(formatted);
            messageInput.text = string.Empty;
        }
        catch (Exception ex)
        {
            AppendChat("[Client] Send failed: " + ex.Message);
        }
    }

    private void ReceiveLoop()
    {
        try
        {
            using (var reader = new StreamReader(client.GetStream()))
            {
                string line;
                while (isConnected && (line = reader.ReadLine()) != null)
                {
                    incomingMessages.Enqueue(line);
                }
            }
        }
        catch (IOException)
        {
            // disconnected
        }
        catch (Exception ex)
        {
            incomingMessages.Enqueue("[Client] Receive error: " + ex.Message);
        }
        finally
        {
            incomingMessages.Enqueue("[Client] Disconnected from server.");
            Disconnect();
        }
    }

    private void Disconnect()
    {
        if (!isConnected) return;
        isConnected = false;
        try { client?.Close(); } catch { }
        try { receiveThread?.Abort(); } catch { }
        client = null;
        writer = null;
    }

    private void AppendChat(string text)
    {
        if (chatOutput == null) return;
        chatOutput.text += text + "\n";
    }
}