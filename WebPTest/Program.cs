using System;
using System.Drawing;
using System.Windows.Forms;
using WebPWrapper;

namespace WebPTest
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //[STAThread]
        static void Main(string[] args)
        {
            /*Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WebPExample());
            */
            using (var webp = new WebP())
            {
                var bmp = new Bitmap(@"vrctest.png");
                Console.WriteLine($"Loaded image {bmp.PixelFormat} {bmp.Width}x{bmp.Height}");
                webp.EncodeWithMeta(bmp, "out.webp");
            }
            Console.WriteLine("Done");
        }
    }
}
