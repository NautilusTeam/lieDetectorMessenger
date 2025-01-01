using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Collections.Specialized.BitVector32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.IO;

namespace lieDetectorMessenger
{
    public partial class Form1 : Form
    {
        private PictureBox pictureBox;

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

            IPEndPoint remoteEndPoint = client.Client.RemoteEndPoint as IPEndPoint;
            Console.WriteLine("새 클라이언트가 접속했습니다. ");
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

                        // Display image
                        Invoke(new Action(() =>
                        {
                            lieDetectorPicture.Image = frame.ToBitmap();
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
                // 클라이언트 처리를 위한 스레드 시작
                Thread clientThread = new Thread(new ParameterizedThreadStart(run_connection));
                clientThread.Start(client);
            }
        }

        public Form1()
        {
            InitializeComponent();
            answerBox.KeyDown += AnswerBox_KeyDown;
            Task runTask = Task.Run(() => run_server());
        }

        private void AnswerBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter 키를 감지
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Enter 키의 기본 동작(삽입 줄바꿈)을 막음
                answerBox.Text = "";
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true; // Enter 키의 기본 동작(삽입 줄바꿈)을 막음
                answerBox.Text = "ESC";
            }
        }
    }
}