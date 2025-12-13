using System.Net.Sockets;
using GameAndDot.Shared.Models;
using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Protocol;
using System.Text.Json;
using System.Collections.Generic;

namespace GameAndDot.Gui
{
    public partial class Form1 : Form
    {
        private readonly TcpClient _client;
        private NetworkStream? _stream;

        private string? _userName;
        private string _userColor = "#FF0000";

        private readonly Dictionary<string, string> playerColors = new();
        private readonly List<Point> points = new(); 

        private Bitmap drawingBitmap;
        private Graphics bitmapGraphics;

        private readonly List<byte> _packetBuffer = new();

        const string host = "127.0.0.1";
        const int port = 8888;

        public Form1()
        {
            InitializeComponent();

            InitializeDrawingSurface();

            _client = new TcpClient();

            try
            {
                _client.Connect(host, port);
                _stream = _client.GetStream();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.DrawItem += new DrawItemEventHandler(listBox1_DrawItem);
            listBox1.ItemHeight = 30;
            listBox1.IntegralHeight = false;
        }

        private void InitializeDrawingSurface()
        {
            drawingBitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            bitmapGraphics = Graphics.FromImage(drawingBitmap);
            bitmapGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            pictureBox1.Image = drawingBitmap;
        }

        private void GenerateRandomColor()
        {
            int seed = (_userName?.GetHashCode() ?? 0) + Environment.TickCount;
            Random localRandom = new Random(seed);
            _userColor = $"#{localRandom.Next(0x1000000):X6}";
            Console.WriteLine($"Игрок {_userName} получил цвет: {_userColor}");
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            label6.Visible = false;
            textBox1.Visible = false;
            button3.Visible = false;

            label1.Visible = true;
            label2.Visible = true;
            label4.Visible = true;
            label5.Visible = true;
            listBox1.Visible = true;
            label3.Visible = true;

            _userName = textBox1.Text;
            label3.Text = _userName;

            GenerateRandomColor();
            Console.WriteLine($"Игрок {_userName} получил цвет: {_userColor}");
            playerColors[_userName] = _userColor;
            label5.Text = _userColor;

            Task.Run(ReceiveMessageAsync);

            var message = new EventMessege()
            {
                Type = EventType.PlayerConected,
                Username = _userName,
                Color = _userColor,
            };

            await SendMessageAsync(message);
        }

        private async Task SendMessageAsync(EventMessege message)
        {
            if (_stream == null)
                return;

            byte[] packetBytes = GameProtocol.SerializeMessage(message);
            await _stream.WriteAsync(packetBytes, 0, packetBytes.Length);
            await _stream.FlushAsync();
        }

        private async Task ReceiveMessageAsync()
        {
            if (_stream == null)
                return;

            var buffer = new byte[4096];

            while (true)
            {
                int bytesRead;
                try
                {
                    bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch
                {
                    break;
                }

                if (bytesRead == 0)
                    break; 

                _packetBuffer.AddRange(buffer.AsSpan(0, bytesRead).ToArray());

                while (true)
                {
                    int endIndex = GameProtocol.FindPacketEndIndex(_packetBuffer);
                    if (endIndex == -1)
                        break; 

                    int packetLength = endIndex + 2; 
                    byte[] packetBytes = _packetBuffer.Take(packetLength).ToArray();
                    _packetBuffer.RemoveRange(0, packetLength);

                    EventMessege? messageRequest = null;
                    try
                    {
                        messageRequest = GameProtocol.DeserializeMessage(packetBytes);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Ошибка парсинга XPacket: " + ex.Message);
                        continue;
                    }

                    if (messageRequest == null)
                        continue;

                    HandleServerMessage(messageRequest);
                }
            }
        }

        private void HandleServerMessage(EventMessege messageRequest)
        {
            switch (messageRequest.Type)
            {
                case EventType.PlayerConected:
                    HandlePlayerConnected(messageRequest);
                    break;

                case EventType.PointedPlaced:
                    HandlePointPlaced(messageRequest);
                    break;

                case EventType.PlayerDisconected:
                    HandlePlayerDisconnected(messageRequest);
                    break;
            }
        }

        private void HandlePlayerConnected(EventMessege messageRequest)
        {
            Invoke(() =>
            {
                if (messageRequest.Players != null && messageRequest.Players.Count > 0)
                {
                    listBox1.Items.Clear();
                    foreach (var name in messageRequest.Players)
                    {
                        listBox1.Items.Add(name);
                    }
                }

                if (messageRequest.PlayerColors != null && messageRequest.PlayerColors.Count > 0)
                {
                    foreach (var kv in messageRequest.PlayerColors)
                    {

                        playerColors[kv.Key] = kv.Value;
                    }
                }

                listBox1.Refresh();
            });
        }

        private void HandlePointPlaced(EventMessege messageRequest)
        {
            Console.WriteLine($"Получил от {messageRequest.Username}: ({messageRequest.X},{messageRequest.Y}), цвет: {messageRequest.Color}");

            if (!string.IsNullOrEmpty(messageRequest.Username) && !string.IsNullOrEmpty(messageRequest.Color))
            {
                playerColors[messageRequest.Username] = messageRequest.Color;
                Invoke(() => listBox1.Refresh());
            }

            if (messageRequest.Username != _userName)
            {
                Invoke(() =>
                {
                    points.Add(new Point(messageRequest.X, messageRequest.Y));
                    DrawPoint(messageRequest.X, messageRequest.Y, messageRequest.Color);
                });
            }
        }

        private void HandlePlayerDisconnected(EventMessege messageRequest)
        {
            Console.WriteLine($"Игрок {messageRequest.Username} отключился");

            Invoke(() =>
            {
                if (messageRequest.Username == null)
                    return;

                listBox1.Items.Remove(messageRequest.Username);

                if (playerColors.ContainsKey(messageRequest.Username))
                {
                    playerColors.Remove(messageRequest.Username);
                }

                listBox1.Refresh();
            });
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            points.Add(new Point(e.X, e.Y));

            DrawPoint(e.X, e.Y);

            pictureBox1.Refresh();

            SendPointToServer(e.X, e.Y);
        }

        private void DrawPoint(int x, int y, string? colorHex = null)
        {
            if (bitmapGraphics == null) return;

            int pointSize = 10;
            Color pointColor;

            if (string.IsNullOrEmpty(colorHex))
            {
                colorHex = _userColor;
            }

            try
            {
                pointColor = ColorTranslator.FromHtml(colorHex);
            }
            catch
            {
                pointColor = Color.Red;
            }

            using (Brush brush = new SolidBrush(pointColor))
            {
                bitmapGraphics.FillEllipse(brush,
                    x - pointSize / 2,
                    y - pointSize / 2,
                    pointSize,
                    pointSize);
            }

            pictureBox1.Refresh();
        }

        private async void SendPointToServer(int x, int y)
        {
            var message = new EventMessege()
            {
                Type = EventType.PointedPlaced,
                Username = _userName,
                X = x,
                Y = y,
                Color = _userColor
            };

            Console.WriteLine($"Отправляю: {_userName}, ({x},{y}), цвет: {_userColor}");
            await SendMessageAsync(message);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            if (drawingBitmap != null)
            {
                drawingBitmap.Dispose();
                bitmapGraphics.Dispose();
            }
            InitializeDrawingSurface();
            RedrawAllPoints();
        }

        private void RedrawAllPoints()
        {
            bitmapGraphics.Clear(pictureBox1.BackColor);

            foreach (Point point in points)
            {
                DrawPoint(point.X, point.Y, _userColor);
            }

            pictureBox1.Refresh();
        }

        public void ClearPoints()
        {
            points.Clear();
            if (bitmapGraphics != null)
            {
                bitmapGraphics.Clear(pictureBox1.BackColor);
                pictureBox1.Refresh();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bitmapGraphics != null)
                bitmapGraphics.Dispose();
            if (drawingBitmap != null)
                drawingBitmap.Dispose();
            if (_stream != null)
                _stream.Dispose();
            if (_client != null)
                _client.Close();
        }

        // Остальные обработчики оставляем пустыми
        private void Form1_Load(object sender, EventArgs e) { }
        private void label1_Click(object sender, EventArgs e) { }
        private void label2_Click(object sender, EventArgs e) { }
        private void label3_Click(object sender, EventArgs e) { }
        private void label4_Click(object sender, EventArgs e) { }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void label5_Click(object sender, EventArgs e) { }
        private void textBox1_TextChanged_1(object sender, EventArgs e) { }
        private void label6_Click(object sender, EventArgs e) { }

        private void listBox1_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            string playerName = listBox1.Items[e.Index].ToString() ?? "";

            string colorHex = playerColors.ContainsKey(playerName)
                ? playerColors[playerName]
                : "#000000";

            Color color;
            try
            {
                color = ColorTranslator.FromHtml(colorHex);
            }
            catch
            {
                color = Color.Black;
            }

            e.DrawBackground();
            using (Brush brush = new SolidBrush(color))
            {
                e.Graphics.DrawString(" " + playerName, e.Font, brush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }
    }
}
