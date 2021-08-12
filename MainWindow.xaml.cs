using CaptureGMM.cs;
using CaptureGMM.windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CaptureGMM
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        //public C_setting SET;
        public String s_快速鍵 = "RCtrl + PrtScrSysRq";//快速鍵預設值
        public String s_快速鍵_全螢幕 = "PrtScrSysRq";//快速鍵預設值
        public String s_快速鍵_目前視窗 = "RAlt + PrtScrSysRq";//快速鍵預設值
        public bool bool_自定儲存路徑 = false;
        public bool bool_單層儲存路徑 = false;
        public String s_自定儲存路徑 = "D:\\圖片";



        //public W_設定 w_設定;
        //public C_頁籤拖曳 c_分頁;
        public String s_筆記路徑 = "imgs";
        //WebBrowserOverlay wbo;



        public double d_解析度比例_x = 1;
        public double d_解析度比例_y = 1;

        public System.Windows.Forms.WebBrowser web_資料夾;

        public MainWindow()
        {
            InitializeComponent();

            C_Adapter.Initialize();

            but_截圖.Click += (sender, e) => {
                func_截圖();

            };

            this.Loaded += MainWindow_Loaded;

        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //var c_毛玻璃 = new aero_glass();
            //c_毛玻璃.func_啟用毛玻璃(this);

            //new C_全域按鍵偵測(this);

            //取得解析度偏差
            PresentationSource source = PresentationSource.FromVisual(this);
            d_解析度比例_x = source.CompositionTarget.TransformToDevice.M11;
            d_解析度比例_y = source.CompositionTarget.TransformToDevice.M22;

        }




        /// <summary>
        /// 直接去的圖片儲存的目錄
        /// </summary>
        /// <returns></returns>
        public String func_取得儲存資料夾()
        {

            String p = func_取得儲存根目錄() + "\\"; //String p = func_取得儲存根目錄() + "\\" + c_分頁.b_but_text.Text;

            if (bool_單層儲存路徑)
            {
                p = func_取得儲存根目錄();
            }

            if (Directory.Exists(p) == false)
            {
                Directory.CreateDirectory(p);
                func_重新整理資料夾();
            }

            return p;
        }

        /// <summary>
        /// 取得儲存 子資料夾 的根目錄資料夾
        /// </summary>
        /// <returns></returns>
        public String func_取得儲存根目錄()
        {

            //避免結尾有多餘的\
            s_自定儲存路徑 = s_自定儲存路徑.Replace("/", "\\");

            if (s_自定儲存路徑.Substring(s_自定儲存路徑.Length - 1) == "\\")
            {
                s_自定儲存路徑 = s_自定儲存路徑.Substring(0, s_自定儲存路徑.Length - 1);
            }

            String s_dir = func_exe_path() + "\\" + s_筆記路徑;

            if (bool_自定儲存路徑)
            {
                if (Directory.Exists(s_自定儲存路徑))
                {
                    s_dir = s_自定儲存路徑;
                }
                else
                {
                    bool_自定儲存路徑 = false;
                }
            }

            //如果資料夾不存在，就新建
            if (Directory.Exists(s_dir) == false)
            {
                //新增資料夾
                Directory.CreateDirectory(s_dir);
            }

            /*
            if (bool_單層儲存路徑 == false) {
                //如果資料夾不存在，就新建
                if (Directory.Exists(s_dir) == false) {
                    //新增資料夾
                    Directory.CreateDirectory(s_dir + "\\" + "New Folder 1");
                }
            }*/

            return s_dir;
        }


        /// <summary>
        /// 回傳圖片的儲存路徑
        /// </summary>
        /// <param name="type">檔案類型，例如 jpg 或 png </param>
        /// <returns></returns>
        public String func_取得儲存檔名(String type)
        {

            if (type != "")
                type = "." + type;

            String s_根目錄 = func_取得儲存資料夾();
            String name = DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
            String s_儲存路徑 = s_根目錄 + "\\" + name + type;

            //如果檔案已經存在
            if (File.Exists(s_儲存路徑))
            {
                for (int i = 1; i <= 99999; i++)
                {
                    s_儲存路徑 = s_根目錄 + "\\" + name + $" ({i})" + type;
                    if (File.Exists(s_儲存路徑) == false)
                    {
                        return s_儲存路徑;
                    }
                }
            }


            return s_儲存路徑;
        }

        public screenshot w_截圖;

        public void func_截圖()
        {
            if (w_截圖 != null)
                return;
            w_截圖 = new screenshot(this, e_截圖類型.選取截圖);
        }

        public void func_重新整理資料夾()
        {

            try
            {
                web_資料夾.Navigate(func_取得儲存資料夾());
            }
            catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        public void func_開啟資料夾()
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + func_取得儲存資料夾().Replace("/", "\\") + "\"");
            }
            catch { }
        }


        public String func_exe_path()
        {
            return System.Windows.Forms.Application.StartupPath;
        }

        public void fun_鎖定視窗(Boolean bol)
        {
            try
            {
                //如果視窗已經縮小至右下角，則不啟用
                if (this.ShowInTaskbar == false)
                    return;

                if (bol)
                {
                    this.IsEnabled = false;
                    //wbo.Visibility = Visibility.Collapsed;
                    //this.Opacity = 0.9;

                }
                else
                {
                    this.IsEnabled = true;
                   // wbo.Visibility = Visibility.Visible;
                    //this.Opacity = 1;
                }

            }
            catch
            {

                //如果程式沒有正常退出，就強制結束程式
                System.Environment.Exit(System.Environment.ExitCode);

            }

        }



        /// <summary>
        /// 清理記憶體
        /// </summary>
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        public void fun_清理記憶體()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
            }
        }


    }
}
