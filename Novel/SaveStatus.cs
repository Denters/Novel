using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Timers;
using System.Windows.Media.Imaging;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace Novel
{
    public class Status_Sceneario_Image_Complex
    {
        public Status_Scenario status_scenario;
        public BitmapSource status_image;
    }
    [Serializable]
    public class Status_Table
    {
        private int length = SaveStatus.SAVEDATA_LENGTH;//セーブファイル作成可能数の定義
        internal List<Status_Scenario> scenarios = new List<Status_Scenario>();
        public Status_Table()
        {
            for(int i = 0; i < length; i++)
            {
                scenarios.Add(null);
            }
        }
    }
    [Serializable]
    public class Status_Scenario
    {
        public Dictionary<string, string> flag_dic;//フラグ管理
        public MainWindow.Sentence ss;
        public DateTime SaveTime;
        public string BackIMG;
        public int line_pos;
        public string comment;
        public string file_name;
        public Dictionary<MainWindow.POSITION, string> CharaIMG;
    }
    public class Status_Scenario_IMG
    {
        public BitmapSource img;
    }
    public static class SaveStatus
    {
        public static string FILE_NAME = "save_data";
        private static string EXECUTABLE_PATH = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static int SAVEDATA_LENGTH = 6;
        public static void save(Status_Scenario sst,Status_Scenario_IMG ssti,int id_no)
        {
            Status_Table stt = new Status_Table();
            stt.scenarios[id_no] = sst;
            //stt.scenarios[id_no] = sst;
            try
            {
                //保存するファイルのフルパス設定
                string path = EXECUTABLE_PATH + "\\" + FILE_NAME;

                //FileStreamオブジェクトを、Createモードで生成
                FileStream fs = new FileStream(path, FileMode.Create);

                //BinaryFormatterオブジェクト生成
                BinaryFormatter bf = new BinaryFormatter();
                //サロゲート
                //SurrogateSelector selector = new SurrogateSelector();
                //var surrogate = new SerializationSurrogate();
                //データをシリアル化して保存
                bf.Serialize(fs, stt);

                //ファイルを閉じる
                fs.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "書き込みエラー", MessageBoxButton.OK);
            }

            try
            {
                //保存するファイルのフルパス設定
                string path = EXECUTABLE_PATH + "\\Resources\\" + FILE_NAME + id_no.ToString();
                System.Drawing.Bitmap bmp = ToBitmap(ssti.img, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                bmp.Save(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "書き込みエラー", MessageBoxButton.OK);
            }
        }
        public static Status_Sceneario_Image_Complex load(int id_no)
        {
            Status_Table stt = new Status_Table();
            Status_Sceneario_Image_Complex ssic = new Status_Sceneario_Image_Complex();
            try
            {
                //読み込みするファイルのフルパス設定
                string path = EXECUTABLE_PATH + "\\" + FILE_NAME;
                if (!File.Exists(path))
                {
                    return null;
                }
                //FileStreamオブジェクトを、Openモードで生成
                FileStream fs = new FileStream(path, FileMode.Open);
                //BinaryFormatterオブジェクト生成
                BinaryFormatter bf = new BinaryFormatter();
                //データをシリアル化して格納
                stt = (Status_Table)bf.Deserialize(fs);
                fs.Close();
                ssic.status_scenario = stt.scenarios[id_no];
                ssic.status_image = read_bitmap(id_no);

                
            }
            catch (Exception ex) when ( ex is FileNotFoundException)
            {
                MessageBox.Show("セーブデータが見つかりません(´-﹏-`；)", "読み込みエラー", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show("読み込みエラー", "読み込みエラー", MessageBoxButton.OK);
            }
            return ssic;
        }
        public static int Scenario_Length()
        {
            int answer;
            answer = -1;
            Status_Table stt = new Status_Table();
            try
            {
                //読み込みするファイルのフルパス設定
                string path = EXECUTABLE_PATH + "\\" + FILE_NAME;
                if (!File.Exists(path))
                {
                    return -1;
                }
                //FileStreamオブジェクトを、Openモードで生成
                FileStream fs = new FileStream(path, FileMode.Open);
                //BinaryFormatterオブジェクト生成
                BinaryFormatter bf = new BinaryFormatter();
                //データをシリアル化して保存
                stt = (Status_Table)bf.Deserialize(fs);
            }
            catch (Exception ex) when (ex is FileNotFoundException)
            {
                MessageBox.Show("セーブデータが見つかりません(´-﹏-`；)", "読み込みエラー", MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show("読み込みエラー", "読み込みエラー", MessageBoxButton.OK);
            }
            for (int i = 0; i < SAVEDATA_LENGTH; i++)///!!!体験版等言うことでセーブファイル作成可能数は６と想定
            {
                if (stt.scenarios[i] != null)
                    answer++;
            }
            return answer;
        }
        public static bool Savefile_Exist()
        {
            string path = EXECUTABLE_PATH + "\\" + FILE_NAME;
            if (File.Exists(path))
                return true;
            else
                return false;
        }
        public static BitmapSource read_bitmap(int id_no)
        {
            string path = EXECUTABLE_PATH + "\\Resources\\" + FILE_NAME + id_no.ToString();
            if (!File.Exists(path))
            {
                BitmapSource nul_bit =(BitmapSource)ToImageSource(Properties.Resources.nodata);
                return nul_bit;
            }
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(path);
            BitmapSource bmps = (BitmapSource)ToImageSource(bmp);
            return bmps;
        }


        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public static ImageSource ToImageSource(this System.Drawing.Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
        public static System.Drawing.Bitmap ToBitmap(this BitmapSource bitmapSource, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            int width = bitmapSource.PixelWidth;
            int height = bitmapSource.PixelHeight;
            int stride = width * ((bitmapSource.Format.BitsPerPixel + 7) / 8);  // 行の長さは色深度によらず8の倍数のため
            IntPtr intPtr = IntPtr.Zero;
            try
            {
                intPtr = Marshal.AllocCoTaskMem(height * stride);
                bitmapSource.CopyPixels(new Int32Rect(0, 0, width, height), intPtr, height * stride, stride);
                using (var bitmap = new System.Drawing.Bitmap(width, height, stride, pixelFormat, intPtr))
                {
                    // IntPtrからBitmapを生成した場合、Bitmapが存在する間、AllocCoTaskMemで確保したメモリがロックされたままとなる
                    // （FreeCoTaskMemするとエラーとなる）
                    // そしてBitmapを単純に開放しても解放されない
                    // このため、明示的にFreeCoTaskMemを呼んでおくために一度作成したBitmapから新しくBitmapを
                    // 再作成し直しておくとメモリリークを抑えやすい
                    return new System.Drawing.Bitmap(bitmap);
                }
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(intPtr);
            }
        }
    }
}
