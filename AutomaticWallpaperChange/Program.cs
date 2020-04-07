using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AutomaticWallpaperChange
{
    class Program
    {
        static void Main(string[] args)
        {
            //设置当前程序开机自启动【如果设置了Windows的 任务计划程序就可以注释掉这个开机自启】
            SetExecSelfStarting();
            //获取必应壁纸URL
            var url = GetBingImageUrl();
            //下载图片到本地并返回本地图片文件路径
            var filePath = DownloadImageAndSaveFile(url);
            //设置壁纸
            SystemParametersInfo(20, 0, filePath, 2);
        }

        /// <summary>
        /// 向注册表注册开机自启
        /// Win10需要使用系统管理员权限运行VS才能调试，同理启动这个程序也需要系统管理员权限运行
        /// 这个只需要成功注册一次，后面就可以开机自动启动此程序了
        /// </summary>
        public static void SetExecSelfStarting()
        {
            try
            {
                var execPath = Application.ExecutablePath; //当前程序路径
                using (var rk = Registry.LocalMachine)
                {
                    var resigetryPath = Environment.Is64BitOperatingSystem //判断是否为64位系统
                        ? "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Run"
                        : "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
                    using (var rk2 = rk.CreateSubKey(resigetryPath))
                    {
                        if (rk2 == null) return;
                        var value = rk2.GetValue("AutomaticWallpaperChange") ?? string.Empty;
                        if (execPath.Equals(value.ToString(), StringComparison.OrdinalIgnoreCase)) return;
                        rk2.SetValue("AutomaticWallpaperChange", execPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 下载图片并存储到临时文件夹下
        /// </summary>
        /// <param name="url">图片URL</param>
        /// <returns>保存下载图片文件的路径</returns>
        private static string DownloadImageAndSaveFile(string url)
        {
            using (var client = new WebClient())
            {
                //创建临时文件目录下的存储必应图片的绝对路径
                var filePath = Path.Combine(Path.GetTempPath(), "bing.jpg");
                //将图片下载到这个路径下
                client.DownloadFile(url, filePath);
                //返回当前图片路径
                return filePath;
            }
        }

        /// <summary>
        /// 获取必应图片
        /// </summary>
        /// <returns>必应图片URL</returns>
        private static string GetBingImageUrl()
        {
            using (var client = new WebClient())
            {
                //设置下载的HTML文件的编码为UTF-8
                client.Encoding = Encoding.UTF8;
                //下载必应中国的首页HTML文件
                var html = client.DownloadString("https://cn.bing.com/");
                //使用正则得到背景图片地址
                var match = Regex.Match(html, "id=\"bgLink\".*?href=\"(.+?)\"");
                //得到背景图片URL
                return string.Format("https://cn.bing.com{0}", match.Groups[1].Value);
            }
        }

        /// <summary>
        /// Windows系统函数
        /// </summary>
        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        public static extern int SystemParametersInfo(
            int uAction,
            int uParam,
            string lpvParam,
            int fuWinIni
        );
    }
}
