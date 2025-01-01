using Emgu.CV;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System;
using System.Linq;

public partial class Form1 : Form
{
    private PictureBox lieDetectorPicture;
    private Dictionary<int, Bitmap> sessionImages = new Dictionary<int, Bitmap>();
    private int currentSession = 1;

    public Form1()
    {
        InitializeComponent();
        answerBox.KeyDown += AnswerBox_KeyDown;

        lieDetectorPicture = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom
        };
        Controls.Add(lieDetectorPicture);

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
            while (true)
            {
                int request_code = BitConverter.ToInt32(receive(stream, 4));
                if (request_code == 1)
                {
                    int frame_width = BitConverter.ToInt32(receive(stream, 4));
                    int frame_height = BitConverter.ToInt32(receive(stream, 4));
                    byte[] bytes_image = receive(stream, frame_height * frame_width * 3);

                    Mat frame = new Mat(frame_height, frame_width, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
                    Marshal.Copy(bytes_image, 0, frame.DataPointer, frame_height * frame_width * 3);

                    Bitmap newBitmap = frame.ToBitmap();
                    IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;

                    // Store session image
                    Invoke(new Action(() =>
                    {
                        int sessionId = remoteEndPoint.Port; // Use client's port to distinguish sessions
                        if (sessionImages.ContainsKey(sessionId))
                        {
                            sessionImages[sessionId]?.Dispose();
                        }
                        sessionImages[sessionId] = newBitmap;

                        // Automatically display the first session if it's not displayed yet
                        if (currentSession == sessionId || sessionImages.Count == 1)
                        {
                            DisplaySession(sessionId);
                        }
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
        if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true; // Suppress default ESC behavior
            MoveToNextSession();
        }
    }

    private void MoveToNextSession()
    {
        if (sessionImages.Count == 0) return;

        var sessionKeys = sessionImages.Keys.OrderBy(k => k).ToList();
        int currentIndex = sessionKeys.IndexOf(currentSession);

        // Move to the next session, wrap around if at the last session
        currentSession = sessionKeys[(currentIndex + 1) % sessionKeys.Count];
        DisplaySession(currentSession);
    }

    private void DisplaySession(int sessionId)
    {
        if (sessionImages.TryGetValue(sessionId, out Bitmap image))
        {
            if (lieDetectorPicture.Image != null)
            {
                lieDetectorPicture.Image.Dispose(); // Dispose previous image
            }
            lieDetectorPicture.Image = image;
            Console.WriteLine($"Displaying session {sessionId}");
        }
    }
}
