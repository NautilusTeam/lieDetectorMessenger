// Form1.cs
using Emgu.CV;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using WMPLib;
using System.IO;

public partial class Form1 : MetroFramework.Forms.MetroForm
{
    private int currentSession = 0;
    private Dictionary<int, TcpClient> clientSessions = new Dictionary<int, TcpClient>();
    WindowsMediaPlayer wmp = new WindowsMediaPlayer();
    string enterSoundUrl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", "enter.mp3");

    public Form1()
    {
        InitializeComponent();
        answerBox.KeyDown += AnswerBox_KeyDown;
        Task runTask = Task.Run(() => run_server());
    }

    void run_connection(object obj)
    {
        TcpClient client = obj as TcpClient;
        NetworkStream stream = client.GetStream();

        byte[] receive(NetworkStream streama, int length)
        {
            byte[] buffer = new byte[length];
            int bytesRead = 0;
            while (bytesRead < length)
            {
                bytesRead += streama.Read(buffer, bytesRead, length - bytesRead);
                if (bytesRead == 0)
                {
                    break;
                }
            }
            return buffer;
        }

        try
        {
            IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            int sessionId = remoteEndPoint.Port;
            lock (clientSessions)
            {
                if (clientSessions.Count == 0)
                {
                    currentSession = sessionId;
                }
                clientSessions.Add(sessionId, client);
            }

            while (true)
            {
                int request_code = BitConverter.ToInt32(receive(stream, 4));
                if (request_code == 1)
                {
                    int frame_width = BitConverter.ToInt32(receive(stream, 4));
                    int frame_height = BitConverter.ToInt32(receive(stream, 4));
                    byte[] bytes_image = receive(stream, frame_height * frame_width * 3);

                    if (currentSession != sessionId)
                    {
                        // Discard the packet if not the current session
                        continue;
                    }

                    Mat frame = new Mat(frame_height, frame_width, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
                    Marshal.Copy(bytes_image, 0, frame.DataPointer, frame_height * frame_width * 3);

                    Bitmap newBitmap = frame.ToBitmap();

                    Invoke(new Action(() =>
                    {
                        if (lieDetectorPicture.Image != null)
                        {
                            lieDetectorPicture.Image.Dispose();
                        }
                        lieDetectorPicture.Image = newBitmap;
                        Console.WriteLine($"Displaying session {sessionId}");
                    }));
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("클라이언트와 접속을 해제합니다.");
            Console.WriteLine(e.Message);
        }
        finally
        {
            IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            int sessionId = remoteEndPoint.Port;
            lock (clientSessions)
            {
                clientSessions.Remove(sessionId);
                if (clientSessions.Count == 0)
                {
                    lieDetectorPicture.Image?.Dispose();
                    lieDetectorPicture.Image = null;
                    currentSession = 0;
                    Console.WriteLine("No more sessions available.");
                }
                if (currentSession == sessionId)
                {
                    MoveToNextSession();
                }
            }
            client.Close();
        }
    }

    void run_server()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 3302);
        listener.Start();
        Console.WriteLine("서버가 시작되었습니다. " + listener.Server.LocalEndPoint.ToString());

        while (true)
        {
            TcpClient client = listener.AcceptTcpClient();
            Thread clientThread = new Thread(new ParameterizedThreadStart(run_connection));
            clientThread.IsBackground = true;
            clientThread.Start(client);
        }
    }

    private void AnswerBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            SendAnswerToClientAndDisconnect();
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            MoveToNextSession();
        }
    }

    private void SendAnswerToClientAndDisconnect()
    {
        if (clientSessions.TryGetValue(currentSession, out TcpClient client))
        {
            NetworkStream stream = client.GetStream();
            string answer = answerBox.Text;
            answerBox.Clear();

            string sendStr = $"L/{answer}";
            byte[] sendBytes = Encoding.UTF8.GetBytes(sendStr);
            stream.Write(sendBytes, 0, sendBytes.Length);
            MoveToNextSession();
            wmp.URL = enterSoundUrl;
            wmp.controls.play();
        }
    }

    private void MoveToNextSession()
    {
        if (clientSessions.Count == 0)
        {
            lieDetectorPicture.Image?.Dispose();
            lieDetectorPicture.Image = null;
            currentSession = 0;
            Console.WriteLine("No more sessions available.");
            return;
        }

        var sessionKeys = clientSessions.Keys.OrderBy(k => k).ToList();
        int currentIndex = sessionKeys.IndexOf(currentSession);

        currentSession = sessionKeys[(currentIndex + 1) % sessionKeys.Count];
        Console.WriteLine($"Switched to session {currentSession}");
    }
}
