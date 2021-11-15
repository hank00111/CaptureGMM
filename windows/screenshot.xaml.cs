﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace CaptureGMM.windows
{

    public enum e_截圖類型
    {
        選取截圖 = 0,
        全螢幕截圖_png = 1
    }

    public class Sendjson
    {
        public string command { set; get; }
        public string x1 { set; get; }
        public string y1 { set; get; }
        public string x2 { set; get; }
        public string y2 { set; get; }
        public string x3 { set; get; }
        public string y3 { set; get; }
        
    }

    /// <summary>
    /// screenshot.xaml 的互動邏輯
    /// </summary>
    public partial class screenshot : Window
    {


        //private System.Windows.Forms.Timer t_拖曳中;
        private TcpClient client;
        private DispatcherTimer _timer;
        private DispatcherTimer send_timer;


        private bool link_st = false;
        private bool send_m_st = false;
        private 拖曳模式 int_拖曳模式 = 拖曳模式.none;
        private double[] d_xywh = { 0, 0, 0, 0 };
        private System.Drawing.Point d_滑鼠xy = new System.Drawing.Point();
        private Boolean bool_初始 = true;
        private Rectangle re_目前拖曳物件;

        private System.Drawing.Bitmap bimg = null;


        private int int_x1 = 0;
        private int int_y1 = 0;
        private int int_x2 = 0;
        private int int_y2 = 0;
        private int int_螢幕起始坐標_x = 0;
        private int int_螢幕起始坐標_y = 0;
        private double d_解析度比例_x = 1;
        private double d_解析度比例_y = 1;
        private double d_螢幕_w = 0;
        private double d_螢幕_h = 0;

 
        //按空白鍵 拖曳
        private 拖曳模式 int_空白鍵記錄 = 拖曳模式.中心;
        private Boolean bool_空白鍵記錄 = false;

        private MainWindow M;
        private WindowState ws_全螢幕前的狀態;
        private double d_記錄視窗位子 = 0;


        SolidColorBrush color_滑鼠移入 = new SolidColorBrush { Color = Color.FromArgb(255, 100, 250, 255) };
        SolidColorBrush color_原始 = new SolidColorBrush { Color = Color.FromArgb(150, 100, 250, 255) };


        /// <summary>
        /// 
        /// </summary>
        /// <param name="m"></param>
        public screenshot(MainWindow m, e_截圖類型 e_type)
        {

            this.M = m;

            //取得螢幕總起始坐標、總大小
            int_螢幕起始坐標_x = 0;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {//列出所有螢幕資訊     
                if (screen.Bounds.X < int_螢幕起始坐標_x)
                    int_螢幕起始坐標_x = screen.Bounds.X;
                if (screen.Bounds.Y < int_螢幕起始坐標_y)
                    int_螢幕起始坐標_y = screen.Bounds.Y;

                int yy = screen.Bounds.Y + screen.Bounds.Height;
                if (yy > d_螢幕_h)
                    d_螢幕_h = yy;

                int xx = screen.Bounds.X + screen.Bounds.Width;
                if (xx > d_螢幕_w)
                    d_螢幕_w = xx;
                System.Console.WriteLine(screen);
            }

            d_螢幕_w -= int_螢幕起始坐標_x;
            d_螢幕_h -= int_螢幕起始坐標_y;

          

            if (M.ShowInTaskbar && M.WindowState != WindowState.Minimized)
            {

                //截圖前記錄視窗狀態
                ws_全螢幕前的狀態 = M.WindowState;
                d_記錄視窗位子 = M.Top;


                //避免全螢幕導致視窗無法隱藏
                M.fun_鎖定視窗(true);
                M.WindowStyle = System.Windows.WindowStyle.None;//無邊框
                M.WindowState = WindowState.Normal;
                M.Top = -5000;
            }


            this.Closed += (object sender, EventArgs e) => {
                if (M.ShowInTaskbar && M.WindowState != WindowState.Minimized)
                {
                    M.Top = d_記錄視窗位子;
                    M.WindowState = ws_全螢幕前的狀態;
                    M.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                    M.fun_鎖定視窗(false);
                }
            };



            bimg = CaptureScreen();//全螢幕截圖

            InitializeComponent();
            this.Top = -5000;
            this.Show();

            st_按鈕群.Visibility = Visibility.Hidden;

            func_初始化(e_type);


        }




        /// <summary>
        /// 
        /// </summary>
        private void func_初始化(e_截圖類型 e_type)
        {
            if (e_type == e_截圖類型.全螢幕截圖_png)
            {

                String s_儲存路徑 = M.func_取得儲存檔名("png");

                //轉換成 rgb24 ，才不會有破圖現象
                var bmpOut = new System.Drawing.Bitmap(bimg.Width, bimg.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                var g = System.Drawing.Graphics.FromImage(bmpOut);
                g.DrawImage(bimg,
                    new System.Drawing.Rectangle(0, 0, bimg.Width, bimg.Height),
                    new System.Drawing.Rectangle(0, 0, bimg.Width, bimg.Height),
                    System.Drawing.GraphicsUnit.Pixel
                );
                bmpOut.Save(s_儲存路徑);

                //自動存入剪貼簿


                func_關閉程式();
                return;
            }


            st_size與放大鏡.Visibility = Visibility.Hidden;//隱藏szie資訊

            //初始化顏色
            b_上.Fill = color_原始;
            b_下.Fill = color_原始;
            b_右.Fill = color_原始;
            b_左.Fill = color_原始;
            b_右上.Stroke = color_原始;
            b_右下.Stroke = color_原始;
            b_左上.Stroke = color_原始;
            b_左下.Stroke = color_原始;



            //取得解析度偏差
            PresentationSource source = PresentationSource.FromVisual(M);
            d_解析度比例_x = source.CompositionTarget.TransformToDevice.M11;
            d_解析度比例_y = source.CompositionTarget.TransformToDevice.M22;



            //this.Background = new ImageBrush(ToBitmapSource(bimg));//設定背景

            this.Focus();

            //程式最大化
            this.Width = d_螢幕_w / d_解析度比例_x;
            this.Height = d_螢幕_h / d_解析度比例_y;
            //var Work = System.Windows.Forms.Screen.GetBounds(new System.Drawing.Point((int)this.Left, (int)this.Top));

            //MessageBox.Show(d_螢幕_h + "\n" + System.Windows.Forms.Cursor.Clip.Size.Height + "");


            this.Left = int_螢幕起始坐標_x;
            this.Top = int_螢幕起始坐標_y;

            //啟動計時器
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(10);
            _timer.Tick += new EventHandler(T_Tick);
            _timer.Start();

            //t_拖曳中 = new System.Windows.Forms.Timer();
            //t_拖曳中.Interval = 10;
            //t_拖曳中.Tick += T_Tick;
            //t_拖曳中.Start();
            T_Tick(null, null);//立刻執行，避免使用者開啟截圖後立即點擊

            func_重新();

            Rectangle[] ar = { b_中心拖曳, b_左上, b_上, b_右上, b_右, b_右下, b_下, b_左下, b_左 };

            //滑鼠按下
            for (int i = 0; i < ar.Length; i++)
            {
                ar[i].MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                    re_目前拖曳物件 = (Rectangle)sender;
                    st_按鈕群.Visibility = Visibility.Hidden;
                    d_滑鼠xy = func_取得滑鼠();
                    _timer.Start();

                    //t_拖曳中.Start();
                };
            }


            //滑鼠移入後改變顏色
            for (int i = 0; i < ar.Length; i++)
            {
                ar[i].MouseEnter += (sender, e) => {
                    if (sender == b_上 || sender == b_右 || sender == b_下 || sender == b_左)
                    {
                        if (_timer.IsEnabled == false)
                            ((Rectangle)sender).Fill = color_滑鼠移入;

                        /*if (t_拖曳中.Enabled == false)
                            ((Rectangle)sender).Fill = color_滑鼠移入;*/
                    }
                };
                ar[i].MouseLeave += (sender, e) => {
                    if (sender == b_上 || sender == b_右 || sender == b_下 || sender == b_左)
                    {
                        if (_timer.IsEnabled == false)
                        {
                            ((Rectangle)sender).Fill = color_原始;
                        }

                        /*if (t_拖曳中.Enabled == false)
                        {
                            ((Rectangle)sender).Fill = color_原始;
                        }*/
                    }
                };
                ar[i].MouseDown += (sender, e) => {
                    if (sender == b_上 || sender == b_右 || sender == b_下 || sender == b_左)
                    {

                        ((Rectangle)sender).Fill = color_滑鼠移入;
                    }
                };

                ar[i].MouseEnter += (sender, e) => {
                    if (sender == b_左上 || sender == b_左下 || sender == b_右上 || sender == b_右下)
                    {

                        if (_timer.IsEnabled == false)
                            ((Rectangle)sender).Stroke = color_滑鼠移入;

                        /*if (t_拖曳中.Enabled == false)
                            ((Rectangle)sender).Stroke = color_滑鼠移入;*/
                    }
                };
                ar[i].MouseLeave += (sender, e) => {
                    if (sender == b_左上 || sender == b_左下 || sender == b_右上 || sender == b_右下)
                    {
                        if (_timer.IsEnabled == false)
                            ((Rectangle)sender).Stroke = color_原始;

                        /*if (t_拖曳中.Enabled == false)
                            ((Rectangle)sender).Stroke = color_原始;*/
                    }
                };
                ar[i].MouseDown += (sender, e) => {
                    if (sender == b_左上 || sender == b_左下 || sender == b_右上 || sender == b_右下)
                    {

                        ((Rectangle)sender).Stroke = color_滑鼠移入;
                    }
                };
            }

            b_中心拖曳.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_中心拖曳.Margin.Left, b_中心拖曳.Margin.Top, b_中心拖曳.ActualWidth, b_中心拖曳.ActualHeight };
                int_拖曳模式 = 拖曳模式.中心;

            };

            b_上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_上.Margin.Left, b_上.Margin.Top, b_上.ActualWidth, b_上.ActualHeight };
                int_拖曳模式 = 拖曳模式.上;
            };
            b_下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_下.Margin.Left, b_下.Margin.Top, b_下.ActualWidth, b_下.ActualHeight };
                int_拖曳模式 = 拖曳模式.下;
            };

            b_右.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右.Margin.Left, b_右.Margin.Top, b_右.ActualWidth, b_右.ActualHeight };
                int_拖曳模式 = 拖曳模式.右;
            };
            b_左.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左.Margin.Left, b_左.Margin.Top, b_左.ActualWidth, b_左.ActualHeight };
                int_拖曳模式 = 拖曳模式.左;
            };

            b_左上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左上.Margin.Left, b_左上.Margin.Top, b_左上.ActualWidth, b_左上.ActualHeight };
                int_拖曳模式 = 拖曳模式.左上;
            };
            b_右上.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右上.Margin.Left, b_右上.Margin.Top, b_右上.ActualWidth, b_右上.ActualHeight };
                int_拖曳模式 = 拖曳模式.右上;
            };
            b_右下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_右下.Margin.Left, b_右下.Margin.Top, b_右下.ActualWidth, b_右下.ActualHeight };
                int_拖曳模式 = 拖曳模式.右下;
            };
            b_左下.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {
                d_xywh = new double[] { b_左下.Margin.Left, b_左下.Margin.Top, b_左下.ActualWidth, b_左下.ActualHeight };
                int_拖曳模式 = 拖曳模式.左下;
            };



            this.MouseLeftButtonUp += (object sender, MouseButtonEventArgs e) => {

                if (rg_遮罩.Rect.Width == 0 || rg_遮罩.Rect.Height == 0)
                {
                    func_重新();
                    return;
                }

                _timer.IsEnabled = false;
                //t_拖曳中.Enabled = false;
                rect_游標.Margin = new Thickness(-100, -100, 0, 0);


                fun_顯示按鈕();

            };


            //第一次拖曳
            this.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) => {

                if (bool_初始)
                {
                    b_左上.Margin = new Thickness(func_取得滑鼠().X - 10, func_取得滑鼠().Y - 10, 0, 0);

                    d_xywh = new double[] { b_左上.Margin.Left, b_左上.Margin.Top, b_左上.ActualWidth, b_左上.ActualHeight };
                    d_滑鼠xy = func_取得滑鼠();
                    _timer.Start();
                    //t_拖曳中.Start();
                    int_拖曳模式 = 拖曳模式.右下;
                    DoEvents();
                    bool_初始 = false;
                }
            };







            this.KeyDown += (sender, e) => {


            };
            this.KeyUp += (object sender, KeyEventArgs e) => {


                //按空白鍵 拖曳
                if (e.Key == Key.Space)
                {
                    bool_空白鍵記錄 = false;

                    Rectangle r = null;
                    if (int_空白鍵記錄 == 拖曳模式.左上)
                    {
                        r = b_左上;
                    }
                    else if (int_空白鍵記錄 == 拖曳模式.右上)
                    {
                        r = b_右上;
                    }
                    else if (int_空白鍵記錄 == 拖曳模式.右下)
                    {
                        r = b_右下;
                    }
                    else if (int_空白鍵記錄 == 拖曳模式.左下)
                    {
                        r = b_左下;
                    }
                    else if (int_空白鍵記錄 == 拖曳模式.上)
                    {
                        r = b_上;
                    }
                    else if (int_空白鍵記錄 == 拖曳模式.下)
                    {
                        r = b_下;
                    }
                    else if (int_空白鍵記錄 == 拖曳模式.右)
                    {
                        r = b_右;
                    }
                    else if (int_空白鍵記錄 == 拖曳模式.左)
                    {
                        r = b_左;
                    }
                    else
                    {

                        return;
                    }

                    d_xywh = new double[] { r.Margin.Left, r.Margin.Top, r.ActualWidth, r.ActualHeight };
                    d_滑鼠xy = func_取得滑鼠();

                    int_拖曳模式 = int_空白鍵記錄;
                    this.Title = "**";
                }

            };


            //右鍵 結束
            this.MouseRightButtonUp += (object sender, MouseButtonEventArgs e) => {
                if (int_拖曳模式 == 拖曳模式.none)
                {

                    func_關閉程式();
                }
                else
                {
                    send_timer.IsEnabled = false;
                    func_重新();
                }
            };

            button_關閉.Click += (sender, e) => {
                //send_timer.IsEnabled = false;
                func_關閉程式();
            };

            button_重新.Click += (sender, e) => {
                func_重新();
            };

            //button_確認_png.Click += (sender, e) => {
                //fun_確認儲存("png");
            //};
            button_確認_jpg.Click += (sender, e) => {

                send_timer = new DispatcherTimer();
                send_timer.Interval = TimeSpan.FromMilliseconds(10);
                send_timer.Tick += new EventHandler(func_send);
                send_timer.Start();
                //func_send();
                send_m_st = true;
                //fun_確認儲存("jpg");
               
            };


        }


        /// <summary>
        /// 在全域的鍵盤偵測進行呼叫
        /// </summary>
        /// <param name="e"></param>
        public void func_key_down(System.Windows.Forms.KeyEventArgs e)
        {

            bool bool_ctrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            var k = e.KeyCode;
            if (k == System.Windows.Forms.Keys.Escape)
            { //按esc結束
                func_關閉程式();
                return;
            }

            if (k == System.Windows.Forms.Keys.C && bool_ctrl)
            {//複製
                e.Handled = true;//遮蔽系統原生的快速鍵
                fun_確認儲存("copy");
                return;
            }
            if (k == System.Windows.Forms.Keys.X && bool_ctrl)
            {//複製
                e.Handled = true;//遮蔽系統原生的快速鍵
                fun_確認儲存("copy");
                return;
            }

            if (k == System.Windows.Forms.Keys.D && bool_ctrl)
            {//取消選取
                if (int_拖曳模式 != 拖曳模式.none)
                {
                    func_重新();
                    return;
                }
            }

            if (k == System.Windows.Forms.Keys.S && bool_ctrl)
            {//直接儲存
                if (int_拖曳模式 != 拖曳模式.none)
                {
                    fun_確認儲存("png");
                    return;
                }
            }

            if (k == System.Windows.Forms.Keys.A && bool_ctrl)
            {//全選
                func_全選();
                return;
            }


            if (k == System.Windows.Forms.Keys.Enter)
            {//直接儲存
                if (int_拖曳模式 != 拖曳模式.none)
                {
                    fun_確認儲存("png");
                    return;
                }
                return;
            }

            if (int_拖曳模式 == 拖曳模式.none)
                return;

            //按空白鍵 拖曳
            if (bool_空白鍵記錄 == false)
                if (k == System.Windows.Forms.Keys.Space)
                {
                    bool_空白鍵記錄 = true;
                    d_xywh = new double[] { b_中心拖曳.Margin.Left, b_中心拖曳.Margin.Top, b_中心拖曳.ActualWidth, b_中心拖曳.ActualHeight };
                    d_滑鼠xy = func_取得滑鼠();
                    int_空白鍵記錄 = int_拖曳模式;
                    int_拖曳模式 = 拖曳模式.中心;
                }
        }


        /// <summary>
        /// 
        /// </summary>
        private void fun_顯示按鈕()
        {

            System.Console.WriteLine(int_拖曳模式);

            //st_size與放大鏡.Visibility = Visibility.Hidden;//隱藏size資訊
            

            //避免結束拖曳後，顏色尚未恢復
            b_上.Fill = color_原始;
            b_下.Fill = color_原始;
            b_右.Fill = color_原始;
            b_左.Fill = color_原始;
            b_右上.Stroke = color_原始;
            b_右下.Stroke = color_原始;
            b_左上.Stroke = color_原始;
            b_左下.Stroke = color_原始;


            var rect = rg_遮罩.Rect;
            var mouse = func_取得滑鼠();


            //計算坐標
            int int_邊距 = 10;

            double x = rect.Width + rect.Left + int_邊距;
            double y = rect.Height + rect.Top + int_邊距;

            if (int_x2 == 0 && int_y2 == 0) {
                int_x2 = (int)x - 10;
                int_y2 = (int)y - 10;
            }

            if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下)
            {
                x = mouse.X + int_邊距;
            }
            if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右)
            {
                y = mouse.Y + int_邊距;
            }

            double lw = st_按鈕群.ActualWidth;
            double lh = st_按鈕群.ActualHeight;

            //在矩形選取範圍內
            if (mouse.X + 15 >= rect.Left &&
               mouse.X <= rect.Left + rect.Width - 15)
            {

                if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下)
                {
                    x = mouse.X + int_邊距;
                }
                else
                {
                    x = rect.Left - lw - int_邊距;
                }
            }

            if (mouse.Y + 15 >= rect.Top &&
              mouse.Y <= rect.Top + rect.Height - 15)
            {

                if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右)
                {
                    y = mouse.Y + int_邊距;
                }
                else
                {
                    y = rect.Top - lh - int_邊距 - 30;
                }
            }


            //超出螢幕
            if (x + lw + int_邊距 >= this.ActualWidth)
            {
                x = mouse.X - lw - int_邊距;
            }
            if (y + lh + int_邊距 >= this.ActualHeight)
            {
                y = mouse.Y - lh - int_邊距;
            }
            if (x < 0)
            {
                x = mouse.X + int_邊距;
            }
            if (y < 0)
            {
                y = mouse.Y + int_邊距;
            }



            if (int_拖曳模式 == 拖曳模式.右下)
            {
                x = rect.Width + rect.Left + int_邊距;
                y = rect.Height + rect.Top + int_邊距;

                st_按鈕群.Margin = new Thickness(x + 160, y+32, 20, 20);
            }
            else {

                st_按鈕群.Margin = new Thickness(x, y, 20, 20);
            }
          
            st_按鈕群.Visibility = Visibility.Visible;
           

        }



        /// <summary>
        /// 
        /// </summary>
        public void fun_確認儲存(String type)
        {

            if (int_拖曳模式 == 拖曳模式.none)
                return;

            var t = rg_遮罩.Rect;


            if ((int)(t.Width) == 0 || (int)(t.Height) == 0)
                return;

            /*System.Drawing.Bitmap img = KiCut(bimg, (int)(t.Left* d_解析度比例_x), (int)(t.Top* d_解析度比例_y),
                (int)(t.Width* d_解析度比例_x), (int)(t.Height* d_解析度比例_y));*/

            System.Drawing.Bitmap img = KiCut(
                bimg,
                (int)(t.Left), (int)(t.Top),
                (int)(t.Width), (int)(t.Height)
            );


            if (type == "jpg" || type == "png")
            {
                //儲存圖片
                String s_儲存路徑 = M.func_取得儲存檔名(type);
                func_SaveBitmap(img, type, s_儲存路徑);//存檔


                //自動存入剪貼簿
                try
                {
                    //if (M.checkBox_自動存入剪貼簿.IsChecked.Value)
                    //{
                        //Clipboard.SetData(DataFormats.Bitmap, img);
                    //}
                }
                catch { }


            }
            else if (type == "edit")
            {

                String s_儲存路徑 = M.func_取得儲存檔名("");
                //var c_edit_img = new C_edit_img(new System.Drawing.Bitmap(img), s_儲存路徑);

                //如果原視窗是置頂，就讓編輯視窗也置頂
                //if (M.Topmost)
                //{
                    //c_edit_img.TopMost = true;
                //}

            }
            else
            {
                //存入剪貼簿
                try
                {
                    Clipboard.SetData(DataFormats.Bitmap, img);
                }
                catch { }
            }

            //清理記憶體
            img.Dispose();
            img = null;


            func_關閉程式();

        }
        /// <summary>
        /// 
        /// </summary>
        /// 
        public void func_send(object sender, EventArgs e) {
            try
            {
                //int int_邊距 = 5;
                var rect = rg_遮罩.Rect;
                //var mouse = func_取得滑鼠();
                //double x = rect.Width + rect.Left + int_邊距;
                //double y = rect.Height + rect.Top + int_邊距;

               // while (send_m_st)
                //{
                //}
                var mouse = func_取得滑鼠();
                var sendjson = new Sendjson
                {
                    command = "pip",
                    x1 = int_x1.ToString(),
                    y1 = int_y1.ToString(),
                    x2 = int_x2.ToString(),
                    y2 = int_y2.ToString(),
                    x3 = ((int)mouse.X).ToString(),
                    y3 = ((int)mouse.Y).ToString(),
                };

                string message = JsonConvert.SerializeObject(sendjson);

                if (link_st == false)
                {
                    Int32 port = 5010;
                    client = new TcpClient("192.168.1.10", port);//192.168.0.100 192.168.1.10
                    link_st = true;
                }
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
                if (client.Connected)
                {
                    NetworkStream stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                }

                Console.WriteLine("Send: {0}", message);
                //Thread.Sleep(16);
                //MessageBox.Show(message);
            }
            catch (ArgumentNullException x)
            {
                Console.WriteLine("ArgumentNullException: {0}", x);

            }
            catch (SocketException x)
            {
                Console.WriteLine("SocketException: {0}", x);
            }

            
        }



        /// <summary>
        /// 
        /// </summary>
        public void func_關閉程式()
        {

            M.w_截圖 = null;

            bimg.Dispose();
            bimg = null;
            M.fun_清理記憶體();
            this.Close();
        }



        /// <summary>
        /// 圖片縮放
        /// </summary>
        public static System.Drawing.Bitmap Resize(System.Drawing.Bitmap originImage, Double times)
        {
            int width = Convert.ToInt32(originImage.Width * times);
            int height = Convert.ToInt32(originImage.Height * times);

            return Process(originImage, originImage.Width, originImage.Height, width, height);
        }
        private static System.Drawing.Bitmap Process(System.Drawing.Bitmap originImage, int oriwidth, int oriheight, int width, int height)
        {
            var resizedbitmap = new System.Drawing.Bitmap(width, height);
            var g = System.Drawing.Graphics.FromImage(resizedbitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(System.Drawing.Color.Transparent);
            g.DrawImage(originImage, new System.Drawing.Rectangle(0, 0, width, height), new System.Drawing.Rectangle(0, 0, oriwidth, oriheight), System.Drawing.GraphicsUnit.Pixel);
            return resizedbitmap;
        }


        /// <summary>
        /// 
        /// </summary>
        private void func_全選()
        {

            _timer.IsEnabled = false;
            //t_拖曳中.Enabled = false;



            b_右上.Margin = new Thickness(this.ActualWidth - 10, -10, 0, 0);
            b_右下.Margin = new Thickness(this.ActualWidth - 10, this.ActualHeight - 10, -10, 0);
            b_左上.Margin = new Thickness(-10, -10, 0, 0);
            b_左下.Margin = new Thickness(-10, this.ActualHeight - 10, 0, 0);

            b_下.Margin = new Thickness(0, this.ActualHeight - 11, 0, 0);
            b_右.Margin = new Thickness(this.ActualWidth - 11, 0, 0, 0);
            b_上.Margin = new Thickness(0, -10, 0, 0);
            b_左.Margin = new Thickness(-10, 0, 0, 0);


            int_拖曳模式 = 拖曳模式.中心;
            bool_初始 = false;

            double L = b_左.Margin.Left + 10;
            double T = b_上.Margin.Top + 10;
            double W = b_右.Margin.Left - b_左.Margin.Left;
            double H = b_下.Margin.Top - b_上.Margin.Top;
            b_中心拖曳.Width = Math.Abs(W) - 10;
            b_中心拖曳.Height = Math.Abs(H) - 10;
            b_中心拖曳.Margin = new Thickness(L + 5, T + 5, 0, 0);
            rg_遮罩.Rect = new Rect(L, T, Math.Abs(W) + 1, Math.Abs(H) + 1); //計算遮罩位置
            fun_顯示按鈕();

        }

        /// <summary>
        /// 
        /// </summary>
        public void func_重新()
        {
            int_x1 = 0;
            int_y1 = 0;
            int_x2 = 0;
            int_y2 = 0;

            //計算遮罩位置
            rg_遮罩.Rect = new Rect(0, 0, 0, 0);

            b_右上.Margin = new Thickness(-100, 0, 0, 0);
            b_右下.Margin = new Thickness(-100, 0, 0, 0);
            b_左上.Margin = new Thickness(-100, 0, 0, 0);
            b_左下.Margin = new Thickness(-100, 0, 0, 0);

            b_下.Margin = new Thickness(0, -100, 0, 0);
            b_右.Margin = new Thickness(-100, -100, 0, 0);

            bool_初始 = true;
            int_拖曳模式 = 拖曳模式.none;
            _timer.Start();
            //t_拖曳中.Start();
            st_按鈕群.Visibility = Visibility.Hidden;

        }





        /// <summary>
        /// 
        /// </summary>
        public System.Drawing.Point func_取得滑鼠()
        {

            var Work = System.Windows.Forms.Screen.GetBounds(new System.Drawing.Point((int)this.Left, (int)this.Top));

            //從螢幕的最左上角開始計算，否則多螢幕可能會出錯
            var mmm = System.Windows.Forms.Cursor.Position;
            mmm.X -= int_螢幕起始坐標_x;
            mmm.Y -= int_螢幕起始坐標_y;

            mmm.X = (int)(mmm.X / d_解析度比例_x);
            mmm.Y = (int)(mmm.Y / d_解析度比例_y);


            return mmm;
        }




        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public System.Drawing.Point func_目前拖曳物件的坐標()
        {

            System.Drawing.Point p = new System.Drawing.Point();
            var mou = func_取得滑鼠();

            p.X = mou.X;
            p.Y = mou.Y;

            Rectangle sender = re_目前拖曳物件;

            if (sender == b_上 || sender == b_下)
            {
                p.X = mou.X;
                p.Y = (int)(sender.Margin.Top + (sender.ActualHeight / 2));
            }

            if (sender == b_右 || sender == b_左)
            {
                p.X = (int)(sender.Margin.Left + (sender.ActualWidth / 2));
                p.Y = mou.Y;
            }

            if (sender == b_左上 || sender == b_左下 || sender == b_右上 || sender == b_右下)
            {
                p.X = (int)(sender.Margin.Left + (sender.ActualWidth / 2));
                p.Y = (int)(sender.Margin.Top + (sender.ActualHeight / 2));
            }


            return p;
        }


        /// <summary>
        /// 拖曳、改變大小
        /// </summary>
        private void T_Tick(object sender, EventArgs e)
        {

            if (int_拖曳模式 != 拖曳模式.中心)
            {

                var mou = func_目前拖曳物件的坐標();
                using (System.Drawing.Bitmap img = KiCut(bimg, (int)(mou.X - 15), (int)(mou.Y - 15), (int)(31), (int)(31)))
                {
                    if (img == null)
                    {
                        return;
                    }
                    using (var img2 = new System.Drawing.Bitmap(img, (int)(img.Width / d_解析度比例_x), (int)(img.Height / d_解析度比例_y)))
                    {

                        if (img == null || img2 == null)
                            return;
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            img_放大鏡.Source = ToBitmapSource(img2);
                        }));
                        //img_放大鏡.Source = ToBitmapSource(img2);
                    }
                }
                this.Dispatcher.Invoke(new Action(() =>
                {
                    border_放大鏡.Visibility = Visibility.Visible;
                }));
                //border_放大鏡.Visibility = Visibility.Visible;
            }
            else
            {
                border_放大鏡.Visibility = Visibility.Collapsed;
            }

            //border_放大鏡.Margin = new Thickness(mou.X+155, mou.Y+155, 0,0);

            //lab_size.Content += img.Height + "";
            //System.Console.WriteLine(img.Width + "  "+ img.Height);

            if (int_拖曳模式 == 拖曳模式.none)
            {//初始狀態（僅顯示十字線）

                b_左.Margin = new Thickness(func_取得滑鼠().X - 10, 0, 0, 0);
                b_上.Margin = new Thickness(0, func_取得滑鼠().Y - 10, 0, 0);

                //讓這個物件跟隨，就會顯示十字游標
                rect_游標.Margin = new Thickness(func_取得滑鼠().X - 30, func_取得滑鼠().Y - 30, 0, 0);

                func_顯示size();
                DoEvents();
                return;
            }



            if (int_拖曳模式 == 拖曳模式.中心)
            {//拖曳

                double dou_左中 = b_左.Margin.Left - b_中心拖曳.Margin.Left;
                double dou_右中 = b_右.Margin.Left - b_中心拖曳.Margin.Left;
                double dou_中下 = b_下.Margin.Top - b_中心拖曳.Margin.Top;
                double dou_中上 = b_上.Margin.Top - b_中心拖曳.Margin.Top;


                b_中心拖曳.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );

                b_左.Margin = new Thickness(b_中心拖曳.Margin.Left + dou_左中, 0, 0, 0);
                b_右.Margin = new Thickness(b_中心拖曳.Margin.Left + dou_右中, 0, 0, 0);
                b_下.Margin = new Thickness(0, b_中心拖曳.Margin.Top + dou_中下, 0, 0);
                b_上.Margin = new Thickness(0, b_中心拖曳.Margin.Top + dou_中上, 0, 0);
                Console.WriteLine("setrun");
                //func_send();

                
            }


            if (int_拖曳模式 == 拖曳模式.上)
            {

                b_上.Margin = new Thickness(0, func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1], 0, 0);
                b_左上.Margin = new Thickness(
                    b_左上.Margin.Left,
                    b_上.Margin.Top,
                    0, 0
                );
                b_右上.Margin = new Thickness(
                    b_右上.Margin.Left,
                    b_上.Margin.Top,
                    0, 0
                );

                DoEvents();

            }
            else if (int_拖曳模式 == 拖曳模式.下)
            {

                b_下.Margin = new Thickness(0, func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1], 0, 0);
                b_右下.Margin = new Thickness(
                      b_右下.Margin.Left,
                      b_下.Margin.Top,
                      0, 0
                );
                b_左下.Margin = new Thickness(
                      b_左下.Margin.Left,
                      b_下.Margin.Top,
                      0, 0
                );

                DoEvents();

            }


            if (int_拖曳模式 == 拖曳模式.右)
            {

                b_右.Margin = new Thickness(func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0], 0, 0, 0);
                b_右上.Margin = new Thickness(b_右.Margin.Left, b_右上.Margin.Top, 0, 0);
                b_右下.Margin = new Thickness(b_右.Margin.Left, b_右下.Margin.Top, 0, 0);
                DoEvents();

            }
            else if (int_拖曳模式 == 拖曳模式.左)
            {

                b_左.Margin = new Thickness(func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0], 0, 0, 0);
                b_左上.Margin = new Thickness(b_左.Margin.Left, b_左上.Margin.Top, 0, 0);
                b_左下.Margin = new Thickness(b_左.Margin.Left, b_左下.Margin.Top, 0, 0);
                DoEvents();
            }


            if (int_拖曳模式 == 拖曳模式.左上)
            {
                b_左上.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_上.Margin = new Thickness(0, b_左上.Margin.Top, 0, 0);//同步移動
                b_左.Margin = new Thickness(b_左上.Margin.Left, 0, 0, 0);
                DoEvents();

            }
            else if (int_拖曳模式 == 拖曳模式.右上)
            {

                b_右上.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_上.Margin = new Thickness(0, b_右上.Margin.Top, 0, 0);//同步移動
                b_右.Margin = new Thickness(b_右上.Margin.Left, 0, 0, 0);
                DoEvents();

            }
            else if (int_拖曳模式 == 拖曳模式.右下)
            {

                b_右下.Margin = new Thickness(
                    func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                    func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                    0, 0
                );
                b_下.Margin = new Thickness(0, b_右下.Margin.Top, 0, 0);//同步移動
                b_右.Margin = new Thickness(b_右下.Margin.Left, 0, 0, 0);
                DoEvents();


            }
            else if (int_拖曳模式 == 拖曳模式.左下)
            {

                b_左下.Margin = new Thickness(
                      func_取得滑鼠().X - d_滑鼠xy.X + d_xywh[0],
                      func_取得滑鼠().Y - d_滑鼠xy.Y + d_xywh[1],
                      0, 0
                );
                b_下.Margin = new Thickness(0, b_左下.Margin.Top, 0, 0);//同步移動
                b_左.Margin = new Thickness(b_左下.Margin.Left, 0, 0, 0);
                DoEvents();

            }


            b_左上.Margin = new Thickness(b_左.Margin.Left, b_上.Margin.Top, 0, 0);
            b_右下.Margin = new Thickness(b_右.Margin.Left, b_下.Margin.Top, 0, 0);
            b_左下.Margin = new Thickness(b_左.Margin.Left, b_下.Margin.Top, 0, 0);
            b_右上.Margin = new Thickness(b_右.Margin.Left, b_上.Margin.Top, 0, 0);
            DoEvents();


            double L = b_左.Margin.Left + 10;
            double T = b_上.Margin.Top + 10;
            double W = b_右.Margin.Left - b_左.Margin.Left;
            double H = b_下.Margin.Top - b_上.Margin.Top;

            try
            {
                b_中心拖曳.Width = Math.Abs(W) - 10;
                b_中心拖曳.Height = Math.Abs(H) - 10;
            }
            catch
            {
                b_中心拖曳.Width = 0;
                b_中心拖曳.Height = 0;
            }


            if (b_上.Margin.Top > b_下.Margin.Top)
                T = T + H;

            if (b_左.Margin.Left > b_右.Margin.Left)
                L = L + W;

            b_中心拖曳.Margin = new Thickness(L + 5, T + 5, 0, 0);


            rg_遮罩.Rect = new Rect(L, T, Math.Abs(W) + 1, Math.Abs(H) + 1);//計算遮罩位置


            //矩形選取
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {

                if (rg_遮罩.Rect.Width > rg_遮罩.Rect.Height)
                {
                    rg_遮罩.Rect = new Rect(L, T, Math.Abs(H) + 1, Math.Abs(H) + 1);
                }
                else
                {
                    rg_遮罩.Rect = new Rect(L, T, Math.Abs(W) + 1, Math.Abs(W) + 1);
                }

            }

            func_顯示size();
        }


        /// <summary>
        /// 
        /// </summary>
        private void func_顯示size()
        {

            var rect = rg_遮罩.Rect;
            var mouse = func_取得滑鼠();

            int int_邊距 = 5;

            double x = rect.Width + rect.Left + int_邊距;
            double y = rect.Height + rect.Top + int_邊距;

            if (int_x1 == 0 && int_y1 == 0)
            {
                int_x1 = (int)rect.Left;
                int_y1 = (int)rect.Top;
                
            }

            String size = "";

            if (int_拖曳模式 == 拖曳模式.none)
            {
                size = $"x:{mouse.X}  y:{mouse.Y}";
            }
            else
            {
                size = $"x1:{int_x1} y1:{int_y1}  x2:{(int)x - 5} y2:{(int)y - 5} x3:{mouse.X}  y3:{mouse.Y}";//w:{(int)rect.Width}  h:{(int)rect.Height
            }

            lab_size.Content = size;

            System.Console.WriteLine(size);

            

            if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下 || int_拖曳模式 == 拖曳模式.none)
            {
                x = mouse.X + int_邊距;
            }
            if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右 | int_拖曳模式 == 拖曳模式.none)
            {
                y = mouse.Y + int_邊距;
            }

            double lw = st_size與放大鏡.ActualWidth;
            double lh = st_size與放大鏡.ActualHeight;

            //在矩形選取範圍內
            if (mouse.X + 15 >= rect.Left &&
               mouse.X <= rect.Left + rect.Width - 15)
            {

                if (int_拖曳模式 == 拖曳模式.上 || int_拖曳模式 == 拖曳模式.下)
                {
                    x = mouse.X + int_邊距;
                }
                else
                {
                    x = rect.Left - lw - int_邊距;
                }
            }

            if (mouse.Y + 15 >= rect.Top &&
              mouse.Y <= rect.Top + rect.Height - 15)
            {

                if (int_拖曳模式 == 拖曳模式.左 || int_拖曳模式 == 拖曳模式.右)
                {
                    y = mouse.Y + int_邊距;
                }
                else
                {
                    y = rect.Top - lh - int_邊距;
                }
            }

            //超出螢幕
            if (mouse.X + lw + int_邊距 >= this.ActualWidth)
            {
                x = mouse.X - lw - int_邊距;
            }
            if (mouse.Y + lh + int_邊距 >= this.ActualHeight)
            {
                y = mouse.Y - lh - int_邊距;
            }
            if (x < 0)
            {
                x = mouse.X + int_邊距;
            }
            if (y < 0)
            {
                y = mouse.Y + int_邊距;
            }

            //右下

            this.Dispatcher.Invoke(new Action(() =>
            {
                st_size與放大鏡.Margin = new Thickness(x, y, 0, 0);
                st_size與放大鏡.Visibility = Visibility.Visible;
            }));

            //st_size與放大鏡.Visibility = Visibility.Visible;

        }



        /// <summary>
        /// 取得全螢幕的截圖
        /// </summary>
        private System.Drawing.Bitmap CaptureScreen()
        {
            /// new a bitmap with screen width and height
            var b = new System.Drawing.Bitmap(
                    (int)d_螢幕_w,
                    (int)d_螢幕_h);

            //從螢幕的最左上角開始計算
            var Work = System.Windows.Forms.Screen.GetBounds(new System.Drawing.Point(int_螢幕起始坐標_x, int_螢幕起始坐標_y));

            /// copy screen through .net form api
            using (var g = System.Drawing.Graphics.FromImage(b))
            {
                g.CopyFromScreen(int_螢幕起始坐標_x, int_螢幕起始坐標_y, 0, 0,
                    b.Size, System.Drawing.CopyPixelOperation.SourceCopy);
            }


            return b;
        }



        /// <summary>
        /// 儲存圖片
        /// </summary>
        void func_SaveBitmap(System.Drawing.Bitmap b, String type, string path)
        {


            System.Console.WriteLine(path);

            //png
            if (type == "png")
            {
                b.Save(path);
                return;
            }

            //------------


            //jpg

            var jpgEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);

            // Create an Encoder object based on the GUID
            // for the Quality parameter category.
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one
            // EncoderParameter object in the array.
            var myEncoderParameters = new System.Drawing.Imaging.EncoderParameters(1);

            var myEncoderParameter = new System.Drawing.Imaging.EncoderParameter(myEncoder, 98L);
            myEncoderParameters.Param[0] = myEncoderParameter;
            b.Save(path, jpgEncoder, myEncoderParameters);

        }
        private System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            System.Drawing.Imaging.ImageCodecInfo[] codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageDecoders();
            foreach (System.Drawing.Imaging.ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }




        /// <summary>
        /// 轉換成WPF圖片格式
        /// </summary>
        private static BitmapSource ToBitmapSource(System.Drawing.Bitmap b)
        {
            /// transform to hbitmap image
            IntPtr hb = b.GetHbitmap();

            /// convert hbitmap to bitmap source
            BitmapSource bs =
                Imaging.CreateBitmapSourceFromHBitmap(
                    hb, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            /// release temp hbitmap image
            DeleteObject(hb);

            return bs;
        }
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DeleteObject(IntPtr hObject);





        /// <summary>
        /// 剪裁 -- 用GDI+
        /// </summary>
        /// <param name="b">原始Bitmap</param>
        /// <param name="StartX">開始坐標X</param>
        /// <param name="StartY">開始坐標Y</param>
        /// <param name="iWidth">寬度</param>
        /// <param name="iHeight">高度</param>
        /// <returns>剪裁後的Bitmap</returns>
        public System.Drawing.Bitmap KiCut(System.Drawing.Bitmap b, int StartX, int StartY, int iWidth, int iHeight)
        {
            if (b == null)
            {
                return null;
            }

            //int w = (int)(b.Width / d_解析度比例_x);
            //int h = (int)(b.Height / d_解析度比例_y);
            int w = (b.Width);
            int h = (b.Height);


            if (StartX >= w || StartY >= h)
            {
                return null;
            }

            if (StartX + iWidth > w)
            {
                iWidth = w - StartX;

            }

            if (StartY + iHeight > h)
            {
                iHeight = h - StartY;

            }

            try
            {


                StartX = (int)(StartX * d_解析度比例_x);
                StartY = (int)(StartY * d_解析度比例_y);
                iWidth = (int)(iWidth * d_解析度比例_x);
                iHeight = (int)(iHeight * d_解析度比例_y);


                var bmpOut = new System.Drawing.Bitmap(iWidth, iHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);


                var g = System.Drawing.Graphics.FromImage(bmpOut);

                g.DrawImage(b,
                    new System.Drawing.Rectangle(0, 0, iWidth, iHeight),
                    new System.Drawing.Rectangle(StartX, StartY, iWidth, iHeight),
                    System.Drawing.GraphicsUnit.Pixel
                );

                g.Dispose();

                return bmpOut;
            }
            catch
            {
                return null;
            }
        }


        public static void DoEvents()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }

        private static Object ExitFrame(Object state)
        {
            ((DispatcherFrame)state).Continue = false;
            return null;
        }



    }

    enum 拖曳模式
    {

        none = -1,

        中心 = 0,

        上 = 10,
        下 = 20,
        右 = 30,
        左 = 40,


        左上 = 1,
        右上 = 2,
        右下 = 3,
        左下 = 4,


    }




}