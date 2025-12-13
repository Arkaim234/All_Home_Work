using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GameAndDot.Shared;
using GameAndDot.Shared.Enums;
using GameAndDot.Shared.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using System.Linq;

namespace GameAndDot.Maui
{
    public partial class MainPage : ContentPage
    {
        private readonly ObservableCollection<KeyValuePair<string, string>> _players = new();

        private readonly PointsDrawable _drawable = new();

        private string _userName = "";
        private string _userColor = "#000000";

        private Socket? _socket;
        private CancellationTokenSource? _cts;

        public MainPage()
        {
            InitializeComponent();

            PlayersList.ItemsSource = _players;

            SetupPlayersItemTemplate();

            DrawArea.Drawable = _drawable;
            DrawArea.StartInteraction += OnDrawStart;
            DrawArea.DragInteraction += OnDrawDrag;
        }

        private async void OnLoginClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameEntry.Text))
                return;

            _userName = NameEntry.Text.Trim();
            _userColor = GetRandomColorHex();

            UserNameLabel.Text = _userName;
            ColorLabel.Text = _userColor;

            var color = Color.FromHex(_userColor);
            UserNameLabel.TextColor = color;
            ColorLabel.TextColor = color;

            if (!_players.Any(p => p.Key == _userName))
                _players.Add(new KeyValuePair<string, string>(_userName, _userColor));

            LoginPanel.IsVisible = false;

            await ConnectToServerAsync();

            var connectMessage = new EventMessege
            {
                Type = EventType.PlayerConected,
                Username = _userName,
                Color = _userColor
            };

            await SendEventAsync(connectMessage);
        }

        private string GetRandomColorHex()
        {
            var rnd = new Random();
            int r = rnd.Next(40, 230);
            int g = rnd.Next(40, 230);
            int b = rnd.Next(40, 230);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private async Task ConnectToServerAsync()
        {
            if (_socket != null)
                return;

            var config = SettingsManager.GetInstance();

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await _socket.ConnectAsync(config.HostAddress, config.PortNumber);

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        }

        private async Task SendEventAsync(EventMessege message)
        {
            if (_socket == null)
                return;

            var json = JsonSerializer.Serialize(message);
            var data = Encoding.UTF8.GetBytes(json + "\n");
            var segment = new ArraySegment<byte>(data, 0, data.Length);

            await _socket.SendAsync(segment, SocketFlags.None);
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            if (_socket == null)
                return;

            var buffer = new byte[4096];
            var pending = new StringBuilder();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await _socket.ReceiveAsync(buffer, SocketFlags.None);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    string part = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    pending.Append(part);

                    while (true)
                    {
                        string full = pending.ToString();
                        int newlineIndex = full.IndexOf('\n');
                        if (newlineIndex == -1)
                            break;

                        string jsonText = full.Substring(0, newlineIndex);
                        pending.Clear();
                        pending.Append(full.Substring(newlineIndex + 1));

                        if (string.IsNullOrWhiteSpace(jsonText))
                            continue;

                        EventMessege? msg = null;
                        try
                        {
                            msg = JsonSerializer.Deserialize<EventMessege>(jsonText);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Ошибка парсинга JSON: " + ex.Message);
                            continue;
                        }

                        if (msg == null)
                            continue;

                        HandleServerEvent(msg);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка в ReceiveLoop: " + ex.Message);
            }
        }

        private void HandleServerEvent(EventMessege msg)
        {
            Dispatcher.Dispatch(() =>
            {
                switch (msg.Type)
                {
                    case EventType.PlayerConected:
                        HandlePlayerConnected(msg);
                        break;

                    case EventType.PointedPlaced:
                        HandlePointPlaced(msg);
                        break;

                    case EventType.PlayerDisconected:
                        HandlePlayerDisconnected(msg);
                        break;
                }
            });
        }

        private void HandlePlayerConnected(EventMessege msg)
        {
            _players.Clear();

            if (msg.PlayerColors != null && msg.PlayerColors.Count > 0)
            {
                foreach (var kv in msg.PlayerColors)
                {
                    _players.Add(new KeyValuePair<string, string>(kv.Key, kv.Value));
                }
            }
            else
            {
                foreach (var name in msg.Players)
                {
                    _players.Add(new KeyValuePair<string, string>(name, "#000000"));
                }
            }

            _drawable.Points.Clear();
            foreach (var p in msg.Points)
            {
                _drawable.Points.Add(new ColoredPoint
                {
                    X = p.X,
                    Y = p.Y,
                    Color = Color.FromHex(p.Color)
                });
            }

            DrawArea.Invalidate();
        }

        private void HandlePointPlaced(EventMessege msg)
        {
            _drawable.Points.Add(new ColoredPoint
            {
                X = msg.X,
                Y = msg.Y,
                Color = Color.FromHex(msg.Color)
            });

            DrawArea.Invalidate();
        }

        private void HandlePlayerDisconnected(EventMessege msg)
        {
            if (string.IsNullOrEmpty(msg.Username))
                return;

            var toRemove = _players.FirstOrDefault(p => p.Key == msg.Username);
            if (!toRemove.Equals(default(KeyValuePair<string, string>)))
                _players.Remove(toRemove);
        }

        private void OnDrawStart(object sender, TouchEventArgs e)
        {
            if (!e.Touches.Any())
                return;

            AddPoint(e.Touches.First());
        }

        private void OnDrawDrag(object sender, TouchEventArgs e)
        {
            if (!e.Touches.Any())
                return;

            AddPoint(e.Touches.First());
        }

        private async void AddPoint(PointF p)
        {
            if (string.IsNullOrEmpty(_userName))
                return;

            var color = Color.FromHex(_userColor);

            _drawable.Points.Add(new ColoredPoint
            {
                X = p.X,
                Y = p.Y,
                Color = color
            });
            DrawArea.Invalidate();

            var msg = new EventMessege
            {
                Type = EventType.PointedPlaced,
                Username = _userName,
                X = (int)p.X,
                Y = (int)p.Y,
                Color = _userColor
            };

            await SendEventAsync(msg);
        }

        private void SetupPlayersItemTemplate()
        {
            PlayersList.ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label
                {
                    FontFamily = "Times New Roman",
                    FontSize = 22,
                    Padding = new Thickness(6)
                };

                label.SetBinding(Label.TextProperty, "Key");

                label.BindingContextChanged += (s, e) =>
                {
                    if (label.BindingContext is KeyValuePair<string, string> kv)
                    {
                        label.TextColor = Color.FromHex(kv.Value);
                    }
                    else
                    {
                        label.TextColor = Colors.Black;
                    }
                };

                return label;
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                _cts?.Cancel();
                _socket?.Close();
            }
            catch
            {
            }
        }
    }

    public class ColoredPoint
    {
        public float X { get; set; }
        public float Y { get; set; }
        public Color Color { get; set; } = Colors.Black;
    }

    public class PointsDrawable : IDrawable
    {
        public List<ColoredPoint> Points { get; } = new();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            foreach (var pt in Points)
            {
                canvas.FillColor = pt.Color;
                canvas.FillCircle(pt.X, pt.Y, 4);
            }
        }
    }
}
