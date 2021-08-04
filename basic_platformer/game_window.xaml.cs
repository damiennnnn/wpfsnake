using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
namespace snake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public System.Windows.Point player_pos = new System.Windows.Point(192, 192);
        public Key last_pressed_key = Key.W;
        public List<System.Windows.Point> previous_pos = new List<System.Windows.Point>();
        public System.Windows.Point point_pos = new System.Windows.Point(8, 8);
        public Random rand = new Random();
        public int score = 0;
        public bool game_over = false;
        public bool paused = false;
        public void output(string debug_output)
        {
            string time = DateTime.Now.ToString("[hh:mm:ss]");
            debug_output = string.Concat(time, " ", debug_output, System.Environment.NewLine);
            this.Dispatcher.Invoke(() =>
            {
                foreach (Window window in Application.Current.Windows)
                {
                    if (window.GetType() == typeof(console_window))
                    {
                        (window as console_window).console.AppendText(debug_output);
                        (window as console_window).console.ScrollToEnd();
                    }
                }
            });
        }
        Timer input = new Timer();
        public MainWindow()
        {
            InitializeComponent();
            var console = new console_window();
            console.Show();
            this.KeyDown += key_input;
            this.Closed += close_event;
            bool multiple_of_8 = false;
            while (!multiple_of_8)
            {
                int rand_x = rand.Next(0, 384);
                int rand_y = rand.Next(0, 384);
                multiple_of_8 = ((rand_x % 8) == 0) && ((rand_y % 8) == 0);
                if (multiple_of_8)
                {
                    point_pos.X = rand_x; point_pos.Y = rand_y;
                }
            }
            input.Interval = 40;
            input.Elapsed += input_elapsed;
            Timer render = new Timer();
            render.Interval = 16.666;
            render.Elapsed += render_event;
            input.Start();
            render.Start();
        }

        private void input_elapsed(object sender, ElapsedEventArgs e)
        {
            if (game_over) return;

            if (previous_pos.Count > (score+5))
            {
                previous_pos.RemoveAt(0);
            }
            previous_pos.Add(player_pos);
            switch (last_pressed_key)
            {
                case Key.D: player_pos.X += 8; break;
                case Key.A: player_pos.X -= 8; break;
                case Key.W: player_pos.Y -= 8; break;
                case Key.S: player_pos.Y += 8; break;
            }
            if (player_pos.X >= 384) player_pos.X = 0;
            if (player_pos.Y >= 384) player_pos.Y = 0;
            if (player_pos.X < 0) player_pos.X = 376;
            if (player_pos.Y < 0) player_pos.Y = 376;
            bool multiple_of_8 = false;
            if (player_pos == point_pos)
            {
                score++;
                while (!multiple_of_8)
                {
                    int rand_x = rand.Next(0, 384);
                    int rand_y = rand.Next(0, 384);
                    multiple_of_8 = ((rand_x % 8) == 0) && ((rand_y % 8) == 0);
                    if (multiple_of_8)
                    {
                        point_pos.X = rand_x; point_pos.Y = rand_y;
                    }
                }
            }
            if (previous_pos.Contains(player_pos))
            {
                game_over = true;
                MessageBox.Show(string.Concat("Game Over", Environment.NewLine, "Score: ", score.ToString()));
                close_event(null, null);
            }
            this.Dispatcher.Invoke(() => { Application.Current.MainWindow.Title = string.Concat("Score: " + score.ToString()); });
            output(player_pos.ToString());
        }
        private void close_event(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke( () => { Application.Current.Shutdown(); });
        }
        private void key_input(object sender, KeyEventArgs e)
        {
            output(string.Concat("key ", e.Key.ToString(), " pressed"));
            if (!paused)
            {
                if (e.Key.Equals(Key.W) || e.Key.Equals(Key.S) || e.Key.Equals(Key.A) || e.Key.Equals(Key.D))
                    last_pressed_key = e.Key;
            }
            if (e.Key.Equals(Key.OemPlus))
                score++;
            if (e.Key.Equals(Key.Space))
            {
                paused = !paused;
                if (paused)
                {
                    input.Stop();
                    this.Dispatcher.Invoke(() => { Application.Current.MainWindow.Title = "Paused"; });
                }
                else
                {
                    input.Start();
                    this.Dispatcher.Invoke(() => { Application.Current.MainWindow.Title = string.Concat("Score: " + score.ToString()); });
                }
            }
        }

        private void render_event(object sender, ElapsedEventArgs e)
        {
            if (game_over) return;
            BitmapImage frame = new BitmapImage();
            MemoryStream stream = new MemoryStream();
            Bitmap buffer_copy = new Bitmap(1, 1);
            this.Dispatcher.Invoke(() =>
           {
               buffer_copy = logic();
           });
            buffer_copy.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            frame.BeginInit();
            frame.StreamSource = stream;
            frame.EndInit();
            frame.Freeze();
            this.Dispatcher.Invoke(() =>
            {
                canvas.Source = frame;
            });
            frame = null;
            buffer_copy.Dispose();
        }

        public void fill_pixel(int x, int y, System.Drawing.Color colour, Bitmap buffer)
        {
            for (int y2 = 0; y2 < 8; y2++) // tiles = 8x8
            {
                for (int x2 = 0; x2 < 8; x2++)
                {
                    buffer.SetPixel(x + x2, y + y2, colour);
                }
            }
        }
        public Bitmap logic()
        {
            Bitmap buffer = new Bitmap(384, 384, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            foreach (System.Windows.Point pos in previous_pos.ToArray())
            {
                fill_pixel((int)pos.X, (int)pos.Y, System.Drawing.Color.FromArgb(255,195,0,0), buffer);
            }
            fill_pixel((int)point_pos.X, (int)point_pos.Y, System.Drawing.Color.Gray, buffer);
            fill_pixel((int)player_pos.X, (int)player_pos.Y, System.Drawing.Color.Red, buffer);
            if (paused)
            {
                for (int y = 8; y < 40; y += 8)
                {
                    fill_pixel(8, y, System.Drawing.Color.White, buffer);
                    fill_pixel(24, y, System.Drawing.Color.White, buffer);
                }
            }

            return buffer;
        }


    
    }
}