using System.Net.Sockets;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using GameAndDot.Shared.Models;
using GameAndDot.Shared.Enums;
using System.Text.Json.Schema;
using System.Text.Json;
using System.Collections.Generic; // Добавляем для использования List

namespace GameAndDot.Gui
{
    public partial class Form1 : Form
    {
        private readonly StreamWriter? _writer;
        private readonly StreamReader? _reader;
        private readonly TcpClient _client;
        private string? _userName;
        private string _userColor = "#FF0000";
        private Dictionary<string, string> playerColors = new Dictionary<string, string>();
        private void GenerateRandomColor()
        {
            // Используй хеш имени + текущее время для seed
            int seed = (_userName?.GetHashCode() ?? 0) + Environment.TickCount;
            Random localRandom = new Random(seed);
            _userColor = $"#{localRandom.Next(0x1000000):X6}";

            Console.WriteLine($"Игрок {_userName} получил цвет: {_userColor}");
        }

        // Храним список точек для отрисовки
        private List<Point> points = new List<Point>();
        // Bitmap для рисования
        private Bitmap drawingBitmap;
        // Graphics для рисования на Bitmap
        private Graphics bitmapGraphics;

        const string host = "127.0.0.1";
        const int port = 8888;

        public Form1()
        {
            InitializeComponent();

            // Инициализируем Bitmap и Graphics для рисования
            InitializeDrawingSurface();

            _client = new TcpClient();

            try
            {
                _client.Connect(host, port); //подключение клиента
                _reader = new StreamReader(_client.GetStream());
                _writer = new StreamWriter(_client.GetStream());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            listBox1.DrawMode = DrawMode.OwnerDrawFixed;
            listBox1.DrawItem += new DrawItemEventHandler(listBox1_DrawItem);
            listBox1.ItemHeight = 40;
            listBox1.IntegralHeight = false;
        }

        private void InitializeDrawingSurface()
        {
            drawingBitmap = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            bitmapGraphics = Graphics.FromImage(drawingBitmap);

            bitmapGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            pictureBox1.Image = drawingBitmap;
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
            label5.Text = _userColor;

            GenerateRandomColor();
            Console.WriteLine($"Игрок {_userName} получил цвет: {_userColor}");
            playerColors[_userName] = _userColor;
            // запускаем новый поток для получения данных
            Task.Run(() => ReceiveMessageAsync());

            var message = new EventMessege()
            {
                Type = EventType.PlayerConected,
                Username = _userName,
                Color = _userColor,
            };
            string json = JsonSerializer.Serialize(message);

            await SendMessageAsync(json);
        }

        // отправка сообщений
        async Task SendMessageAsync(string message)
        {
            if (_writer != null)
            {
                await _writer.WriteLineAsync(message);
                await _writer.FlushAsync();
            }
        }

        // получение сообщений
        async Task ReceiveMessageAsync()
        {
            while (true)
            {
                try
                {
                    // считываем ответ в виде строки
                    string? jsonRequest = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(jsonRequest)) continue;

                    var messageRequest = JsonSerializer.Deserialize<EventMessege>(jsonRequest);
                    if (messageRequest == null) continue;

                    switch (messageRequest.Type)
                    {
                        case EventType.PlayerConected:
                            if (messageRequest.Players != null)
                            {
                                Invoke(() =>
                                {
                                    listBox1.Items.Clear();
                                    foreach (var name in messageRequest.Players)
                                    {
                                        listBox1.Items.Add(name); 
                                    }
                                });
                            }
                            break;
                        case EventType.PointedPlaced:
                            Console.WriteLine($"Получил от {messageRequest.Username}: ({messageRequest.X},{messageRequest.Y}), цвет: {messageRequest.Color}");
                            if(!string.IsNullOrEmpty(messageRequest.Username) && !string.IsNullOrEmpty(messageRequest.Color))
                            {
                                playerColors[messageRequest.Username] = messageRequest.Color;
                                Invoke(() => listBox1.Refresh());
                            }
                            if (messageRequest.Username != _userName)
                            {
                                Invoke(() =>
                                {
                                    DrawPoint(messageRequest.X, messageRequest.Y, messageRequest.Color);
                                });
                            }
                            break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            // Добавляем точку в список
            points.Add(new Point(e.X, e.Y));

            // Рисуем точку на Bitmap
            DrawPoint(e.X, e.Y);

            // Обновляем PictureBox
            pictureBox1.Refresh();
            // Отправляем на сервер
            SendPointToServer(e.X, e.Y);
        }

        private void DrawPoint(int x, int y, string colorHex = null)
        {
            if (bitmapGraphics == null) return;

            int pointSize = 10;
            Color pointColor;

            // Если цвет не указан - используем СВОЙ цвет
            if (string.IsNullOrEmpty(colorHex))
            {
                colorHex = _userColor;  // ← Используем свой цвет, а не красный!
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
                Type = EventType.PointedPlaced, // Нужно добавить этот тип в enum
                Username = _userName,
                X = x,
                Y = y,
                Color = _userColor
            };

            string json = JsonSerializer.Serialize(message);
            // Отладка
            Console.WriteLine($"Отправляю: {_userName}, ({x},{y}), цвет: {_userColor}");
            await SendMessageAsync(json);
        }

        // Обработчик события Paint для PictureBox
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Дополнительная отрисовка, если нужна
            // Например, можно отрисовать все точки из списка
            // Это полезно, если вы хотите перерисовывать точки при изменении размера формы
        }

        // Обработчик изменения размера PictureBox
        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            // При изменении размера создаем новый Bitmap
            if (drawingBitmap != null)
            {
                drawingBitmap.Dispose();
                bitmapGraphics.Dispose();
            }
            InitializeDrawingSurface();

            // Перерисовываем все точки
            RedrawAllPoints();
        }

        private void RedrawAllPoints()
        {
            // Очищаем Bitmap
            bitmapGraphics.Clear(pictureBox1.BackColor);

            // Перерисовываем все точки
            foreach (Point point in points)
            {
                DrawPoint(point.X, point.Y, _userColor);
            }

            // Обновляем PictureBox
            pictureBox1.Refresh();
        }

        // Метод для очистки всех точек
        public void ClearPoints()
        {
            points.Clear();
            if (bitmapGraphics != null)
            {
                bitmapGraphics.Clear(pictureBox1.BackColor);
                pictureBox1.Refresh();
            }
        }

        // Не забудьте освободить ресурсы при закрытии формы
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (bitmapGraphics != null)
                bitmapGraphics.Dispose();
            if (drawingBitmap != null)
                drawingBitmap.Dispose();
            if (_reader != null)
                _reader.Dispose();
            if (_writer != null)
                _writer.Dispose();
            if (_client != null)
                _client.Close();
        }

        // Остальные методы без изменений
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

            string playerName = listBox1.Items[e.Index].ToString();

            // Ищем цвет в словаре или берём чёрный
            string colorHex = playerColors.ContainsKey(playerName)
                ? playerColors[playerName]
                : "#000000";

            // Преобразуем в цвет
            Color color;
            try
            {
                color = ColorTranslator.FromHtml(colorHex);
            }
            catch
            {
                color = Color.Black;
            }

            // Рисуем
            using (Brush brush = new SolidBrush(color))
            {
                e.Graphics.DrawString(" " + playerName, e.Font, brush, e.Bounds);
            }
            e.DrawFocusRectangle();
        }
    }
}