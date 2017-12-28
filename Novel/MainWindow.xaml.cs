using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Markup;
using Microsoft.SmallBasic.Library;
using System.Drawing;
using System.Windows.Interop;


namespace Novel
{

    ///<summary>
    ///定数
    /// </summary>
    ///TODO :SE,BGMの実装-----OK
    ///TODO :FONTサイズ-------OK
    ///TODO :JUMPSCENE/シナリオファイル移動
    ///TODO :GAMEOVER---------OK



    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        static public int SAVEDATA_LENGTH = SaveStatus.SAVEDATA_LENGTH;
        enum PagesName { GameBoard, GameTitle, SaveWindow, LoadWindow };


        public struct Sentence
        {
            public string speaker;//話している人物
            public int counter;//カウンター(文字送り用)
            public List<int> chara_length;//総文字数(文字送り用)
            public int chara_c;//文字数カウント(文字送り用)
            public int cell_fcs;//セル数カウント
            public List<string> chara;//現在の文章
            public bool finished;
            public bool isBOLD;
            public System.Windows.Media.FontFamily font_family;
            public double font_size;

        }
        private string EXECUTABLE_PATH = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        const int Speed = 3;
        /// <summary>
        /// シーン名とエクセル上での対応、シーン管理
        /// </summary>
        Dictionary<string, int> scene_ata;
        /// <summary>
        /// フラグ管理
        /// </summary>
        Dictionary<string, string> flag_dic;
        /// <summary>
        /// 捜査中の選択肢格納
        /// </summary>
        public Dictionary<int, string> choice;
        /// <summary>
        /// 現在操作中のフラグ名
        /// </summary>
        string now_choice;
        /// <summary>
        /// ファイルの総文章
        /// </summary>
        string[] lines;
        /// <summary>
        /// Scenarioファイル名
        /// </summary>
        string file_name;
        /// <summary>
        /// 現在の文章のファイル上の位置
        /// </summary>
        int line_pos = 0;
        int speed = Speed;//文字送りスピード調節
        string current_bgimg;
        Dictionary<POSITION, string> chara_and_pos = new Dictionary<POSITION, string>();

        bool locked = false;//trueでボタン押しでの文字送りをキャンセルされる
        bool auto_read = false;//Autoモード
        bool skip_read = false;//Skipモード
        Sentence ss;
        DispatcherTimer dispatcherTimer;
        DispatcherTimer auto_read_timer;
        DispatcherTimer skip_read_timer;
        NavigationService NVS;



        MediaPlayer bgm = new MediaPlayer();//BGM

        public enum POSITION
        {
            LEFT, CENTER, RIGHT
        }
        public enum SIZE
        {
            SMALL, MIDDLE, LARGE
        }
        public void ss_init()//全ての初期化ではない，
        {
            ss.chara = new List<string>();
            ss.chara_length = new List<int>();
            ss.finished = true;
            ss.chara_c = 0;
            ss.counter = 0;
            ss.cell_fcs = 0;
            ss.font_family = null;
            ss.font_size = 0;
        }
        /// <summary>
        /// ページリスト
        /// </summary>
        private List<Uri> _uriList = new List<Uri>() {
            new Uri("GameTitle.xaml",UriKind.Relative),
            //new Uri("Step02Page.xaml",UriKind.Relative),
            //new Uri("Step03Page.xaml",UriKind.Relative),
        };

        private void Main_Window_Loaded(object sender, RoutedEventArgs e)
        {
            NVS.Navigate(_uriList[0]);
        }

        public MainWindow()
        {
            //lines = File.ReadAllLines(@"C:\Users\denter\Documents\Visual Studio 2015\Projects\Novel\Novel\Resources\scenario2.csv");

            InitializeComponent();
            NVS = this.MainFrame.NavigationService;

            dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal);
            dispatcherTimer.Interval = new TimeSpan(10000);
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Start();

            auto_read_timer = new DispatcherTimer(DispatcherPriority.Normal);
            auto_read_timer.Interval = new TimeSpan(0, 0, 0, 0, 1000);
            auto_read_timer.Tick += new EventHandler(auto_reader);

            skip_read_timer = new DispatcherTimer(DispatcherPriority.Normal);
            skip_read_timer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            skip_read_timer.Tick += new EventHandler(textBlock_MouseUp);
            bgm = new MediaPlayer();
            bgm.MediaEnded += new EventHandler(media_loop);
            scenario_read(@"Resources\scenario.csv");

            //Console.WriteLine("start");
            Show();
            //textBlock_MouseUp();
            Main_Window.Width += 1;
            //sub_make sm = new sub_make();
            //sm.Show();
        }
        private void Windowsize_Change(SIZE size)
        {
            if (size == SIZE.SMALL)
            {
                Main_Window.Height = 540;
                Main_Window.Width = 960;
            }else if(size == SIZE.MIDDLE)
            {
                Main_Window.Height = 720;
                Main_Window.Width = 1280;

            }
            else if(size == SIZE.LARGE)
            {
                Main_Window.Height = 1280;
                Main_Window.Width = 1920;

            }
        }
        private void scenario_read(string file_name)
        {
            scene_ata = new Dictionary<string, int>();
            flag_dic = new Dictionary<string, string>();
            lines = System.IO.File.ReadAllLines(file_name);
            ss_init();
            int i = 0;
            foreach (string line in lines)
            {
                string[] words = line.Split(',');//ファイル読み込み時に全行走査
                if (words[0] == "SCENE")
                {
                    scene_ata.Add(words[1], i);///SCENE追加
                }
                i++;
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (ss.counter >= speed && (!ss.finished))//カウンター&文字列exist
            {

                if (ss.chara_c < ss.chara_length[ss.cell_fcs] && ss.chara_length[ss.cell_fcs] != 0 && ss.chara[ss.cell_fcs] != null)
                {//文字送り中でtrue
                    //文中に存在する構文関係の処理
                    if (ss.chara_c == ss.chara[ss.cell_fcs].IndexOf("<br>", ss.chara_c))//改行処理
                    {
                        LineBreak lb = new LineBreak();
                        textBlock.Inlines.Add(lb);
                        ss.chara_c += 4;
                        ss.counter *= -5;
                        return;
                    }
                    else if (ss.chara[ss.cell_fcs] == "BOLD")
                    {
                        ss.isBOLD = true;
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "END_BOLD")
                    {
                        ss.isBOLD = false;
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "DELAY_1")
                    {
                        Thread.Sleep(500);
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "DELAY_2")
                    {
                        Thread.Sleep(1000);
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "DELAY_3")
                    {
                        Thread.Sleep(1500);
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "FONT_0")
                    {
                        ss.font_family = new System.Windows.Media.FontFamily("Meiryo UI");
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "FONT_1")
                    {
                        ss.font_family = new System.Windows.Media.FontFamily("Yu Gothic UI Semibold");
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "FONT_SMALL")
                    {
                        ss.font_size = 20;
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "FONT_MEDIUM")
                    {
                        ss.font_size = 30;
                        ss.cell_fcs++;
                    }
                    else if (ss.chara[ss.cell_fcs] == "FONT_LARGE")
                    {
                        ss.font_size = 40;
                        ss.cell_fcs++;
                    } else if (ss.chara[ss.cell_fcs] == "PLAY_SE")
                    {
                        string sound_src = ss.chara[++ss.cell_fcs];
                        PLAY_sound(sound_src);
                        ss.cell_fcs++;
                    } else if (ss.chara[ss.cell_fcs] == "PLAY_BGM")
                    {
                        string sound_src = ss.chara[++ss.cell_fcs];
                        PLAY_bgm(sound_src);
                        ss.cell_fcs++;
                    } else if (ss.chara[ss.cell_fcs] == "GAMEOVER")
                    {
                        PageChange(PagesName.GameTitle);
                    }

                    if (ss.chara[ss.cell_fcs] == "") { return; }

                    Run t = new Run();
                    t.Text = ss.chara[ss.cell_fcs][ss.chara_c].ToString();

                    if (ss.isBOLD)
                    {
                        //Bold b = new Bold();
                        //b.Inlines.Add(t);
                        t.FontWeight = FontWeights.Bold;
                        //textBlock.Inlines.Add(b);
                    }
                    if (ss.font_family != null)
                    {
                        t.FontFamily = ss.font_family;
                    }
                    if (ss.font_size != 0)
                    {
                        t.FontSize = ss.font_size;
                    }
                    textBlock.Inlines.Add(t);

                    //textBlock.Text= chara.Substring(0, chara_c+1);//これを廃止して，全てaddにする
                    ss.chara_c++;
                }
                else
                {
                    if (ss.cell_fcs == ss.chara.Count - 1)
                    {
                        ss.finished = true;
                        speed = Speed;
                        if (auto_read == true)
                            auto_read_timer.Start();
                    }
                    ss.cell_fcs++;
                    ss.chara_c = 0;
                }
                ss.counter = 0;
            }
            ss.counter++;
            //throw new NotImplementedException();

        }
        private void textBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            textBlock_MouseUp();
        }
        private void textBlock_MouseUp(object sender, EventArgs e)
        {
            textBlock_MouseUp();
        }
        private void textBlock_MouseUp()
        {
            bool auto_next = true;
            while (auto_next)
            {
                auto_next = false;
                if (locked) return;
                if (!ss.finished)//文字送りカット
                {
                    speed = 0;
                    return;
                }
                if (!(lines[line_pos].Split(',')[0] == "BRANCH"))
                {
                    ss_init();
                    textBlock.Inlines.Clear();
                    string line = lines[line_pos];
                    if (line.Substring(0, 3) == "EOF")
                    {
                        Close();
                        return;
                    }
                    string ret = lines[line_pos++];
                    while (ret != null)
                    {
                        ret = draw_text(ret);
                        if (ret == "AUTO_NEXT")
                        {
                            auto_next = true;
                            break;
                        }
                    }
                }
                else
                {
                    string ret = lines[line_pos++];
                    draw_text(ret);
                }
            }
            return;
        }
        private void BGimage_change(string src)
        {
            //string img_path = @"C:\Users\denter\Documents\Visual Studio 2015\Projects\Novel\Novel\Resources\" + src;
            //string img_path = @".\Resources\" + src;
            /*double x = Main_Window.Width, y = Main_Window.Height;
            string img_path = EXECUTABLE_PATH + "\\Resources\\" + src;
            img_path = System.IO.Path.GetFullPath(img_path);
            ImageBrush imb = new ImageBrush();
            System.Windows.Controls.Image img = new System.Windows.Controls.Image();
            img.Source = new BitmapImage(new Uri(img_path));
            BitmapImage bti = new BitmapImage();
            bti.BeginInit();
            bti.UriSource = new Uri(img_path);
            bti.EndInit();
            imb.ImageSource = img.Source;
            imb.Stretch = Stretch.UniformToFill;
            draw_board.Background = bti;
            Main_Window.Width = x;
            Main_Window.Height = y;
            */
            current_bgimg = src;
            double x = Main_Window.Width, y = Main_Window.Height;
            string img_path = EXECUTABLE_PATH + "\\Resources\\" + src;
            BitmapImage bti = new BitmapImage();
            bti.BeginInit();
            bti.UriSource = new Uri(img_path);
            bti.EndInit();
            background.Width = x;
            background.Height = y;
            background.Source = bti;
            Main_Window.Width = x;
            Main_Window.Height = y;

        }
        private string draw_text(string line)
        {
            string[] frame;
            frame = line.Split(',');
            //1セル目の判定
            if (frame[0] == "IMG_BACK")
            {
                BGimage_change(frame[1]);
                return "AUTO_NEXT";
            }
            else if (frame[0] == "IMG_CHARA")
            {
                string position = frame[2];
                POSITION ps = POSITION.RIGHT;
                if (position == "LEFT") ps = POSITION.LEFT;
                else if (position == "CENTER") ps = POSITION.CENTER;
                else if (position == "RIGHT") ps = POSITION.RIGHT;

                CHimage_change(frame[1], ps);
                return "AUTO_NEXT";
            }
            else if (frame[0] == "SCENE")
            {
                return "AUTO_NEXT";
            }
            else if (frame[0] == "BRANCH")
            {
                locked = true;
                if (!flag_dic.ContainsKey(frame[1]))
                    flag_dic.Add(frame[1], null);
                now_choice = frame[1];
                int choice_level = int.Parse(frame[2]);
                string[,] choice_data = new string[4, 2];
                choice = new Dictionary<int, string>();
                for (int i = 0; i < choice_level; i++)
                {
                    string[] line_temp = lines[line_pos++].Split(',');
                    choice.Add(i, line_temp[1]);
                    choice_data[i, 0] = line_temp[1];
                    choice_data[i, 1] = line_temp[2];
                }
                choice_visible(choice_level, choice_data);
                return null;
            }
            else if (frame[0] == "IF_GOTO")
            {

                string branch_key = frame[1];
                for (int i = 0; i < int.Parse(frame[2]); i++)
                {

                    string[] line_temp = lines[line_pos++].Split(',');
                    if (line_temp[1] == flag_dic[branch_key])
                    {
                        line_pos = scene_ata[line_temp[2]];
                    }
                }
                string linee = next_sentence();
                ss_init();
                return linee;
            }
            else if (frame[0] == "JUMP_SCENE")
            {
                string jump_name = frame[1];
                if (frame[2] != "")
                {
                    Scenario_change(frame[2], frame[1]);
                }
                line_pos = scene_ata[jump_name];
                ss_init();
                string linee = next_sentence();
                return linee;
            }
            if (frame[1].Length != 0) ss.speaker = frame[1];//ss.speakerのセット
            c_label.Content = ss.speaker;
            for (int i = 2; i < frame.Length; i++)
            {

                if (frame[i] == "//")
                {
                    break;
                }
                ss.chara.Add(frame[i]);//会話配列へセット
                ss.chara_length.Add(frame[i].Length);
            }

            ss.chara_c = 0;
            ss.finished = false;
            return null;
        }

        private void CHimage_change(string src, POSITION ps)
        {
            if (ps == POSITION.LEFT)
            {

                if (chara_and_pos.ContainsKey(POSITION.LEFT))
                    chara_and_pos[POSITION.LEFT] = src;
                else
                    chara_and_pos.Add(POSITION.LEFT, src);
                if (src == "NULL")
                    CHARA_1.Source = null;
            }
            if (ps == POSITION.CENTER)
            {
                CHARA_2.Source = null;
                if (chara_and_pos.ContainsKey(POSITION.CENTER))
                    chara_and_pos[POSITION.CENTER] = src;
                else
                    chara_and_pos.Add(POSITION.CENTER, src);
                if (src == "NULL")
                    CHARA_1.Source = null;
            }
            if (ps == POSITION.RIGHT)
            {
                CHARA_3.Source = null;
                if (chara_and_pos.ContainsKey(POSITION.RIGHT))
                    chara_and_pos[POSITION.RIGHT] = src;
                else
                    chara_and_pos.Add(POSITION.RIGHT, src);
                if (src == "NULL")
                    CHARA_1.Source = null;
            }
            if (src == "NULL")
                return;
            //string img_path = @"C:\Users\denter\Documents\Visual Studio 2015\Projects\Novel\Novel\Resources\" + src;
            //string img_path = @".\Resources\" + src;
            double x = Main_Window.Width - 2, y = Main_Window.Height - 40;
            string img_path = EXECUTABLE_PATH + "\\Resources\\" + src;
            BitmapImage biti = new BitmapImage();
            biti.BeginInit();
            biti.UriSource = new Uri(img_path);
            biti.EndInit();
            if (ps == POSITION.LEFT)
            {
                CHARA_1.Source = biti;
            }
            if (ps == POSITION.CENTER)
            {
                CHARA_2.Source = biti;
            }
            if (ps == POSITION.RIGHT)
            {
                CHARA_3.Source = biti;
            }
            //CHARA_1.Width = x;
            CHARA_1.Height = y;
            CHARA_1.Margin = new Thickness(0, 0, 0, 0);
            //CHARA_2.Width = x;
            CHARA_2.Height = y;
            CHARA_2.Margin = new Thickness(Width / 3, 0, 0, 0);
            //CHARA_3.Width = x;
            CHARA_3.Height = y;
            CHARA_3.Margin = new Thickness(Width / 3 * 2, 0, 0, 0);
            //draw_board.Background = imb;
        }
        private void Scenario_change(string file_name, string scene)
        {

        }
        private void PLAY_sound(string src)
        {
            string se_path = EXECUTABLE_PATH + "\\Resources\\" + src;
            BitmapImage biti = new BitmapImage();
            Sound.Play(se_path);
        }
        private void PLAY_bgm(string src)
        {
            bgm.Stop();
            string bgm_path = EXECUTABLE_PATH + "\\Resources\\" + src;
            bgm.Open(new Uri(bgm_path));
            bgm.Play();
        }
        private void media_loop(object sender, EventArgs e)
        {
            bgm.Stop();
            bgm.Play();
        }

        private string next_sentence()
        {
            if (line_pos >= lines.Length) return "EOF";
            string line = lines[line_pos++];
            return line;
        }

        private void button_Copy_Click(object sender, RoutedEventArgs e)
        {
            string line = lines[--line_pos];

            ss_init();
            textBlock.Inlines.Clear();
            draw_text(line);

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            int lp = line_pos;
            lines = System.IO.File.ReadAllLines(@"Resources\scenario.csv");
            line_pos = 0;
            for (int i = 0; i < lp; i++)
            {
                ss_init();
                textBlock.Inlines.Clear();
                string line = lines[i];
                draw_text(line);
            }
            line_pos = lp;
        }
        private void choice_visible(int level, string[,] choice_data)
        {
            System.Windows.Controls.Image[] choice_bgs = { choice1_bg, choice2_bg, choice3_bg, choice4_bg };
            Label[] choices = { choice1, choice2, choice3, choice4 };
            for (int i = 0; i < level; i++)
            {
                choices[i].Visibility = Visibility.Visible;
                choice_bgs[i].Visibility = Visibility.Visible;
                choices[i].Content = choice_data[i, 1];
            }
        }
        private void choice_hidden()
        {
            int level = 4;
            System.Windows.Controls.Image[] choice_bgs = { choice1_bg, choice2_bg, choice3_bg, choice4_bg };
            Label[] choices = { choice1, choice2, choice3, choice4 };
            for (int i = 0; i < level; i++)
            {
                choices[i].Visibility = Visibility.Hidden;
                choice_bgs[i].Visibility = Visibility.Hidden;
            }
        }
        private void choice_comp(int choice_num)
        {
            flag_dic[now_choice] = choice[choice_num];
            locked = false;
            choice_hidden();
            textBlock_MouseUp();
        }
        private void choice1_bg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            choice_comp(0);

        }
        private void choice2_bg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            choice_comp(1);
        }

        private void choice3_bg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            choice_comp(2);

        }

        private void choice4_bg_MouseUp(object sender, MouseButtonEventArgs e)
        {
            choice_comp(3);
        }

        private void button_Save_Click(object sender, RoutedEventArgs e)
        {
            temp_source = Capture_Screen();
            PageChange(PagesName.SaveWindow);

            //Status_Save(1);
        }

        private void Status_Save(int id_no)
        {
            Status_Scenario Sstatus = new Status_Scenario();
            Status_Scenario_IMG Sstatus_IMG = new Status_Scenario_IMG();
            Sstatus_IMG.img = temp_source;
            Sstatus.file_name = file_name;
            Sstatus.line_pos = line_pos - 1;
            Sstatus.comment = textBlock.Text;
            Sstatus.flag_dic = flag_dic;
            Sstatus.BackIMG = current_bgimg;
            Sstatus.CharaIMG = chara_and_pos;
            Sstatus.SaveTime = DateTime.Now;

            SaveStatus.save(Sstatus, Sstatus_IMG, id_no);
            //throw new NotImplementedException();
        }

        private void button_Load_Click(object sender, RoutedEventArgs e)///まだ修正していない
        {
            PageChange(PagesName.LoadWindow);
            /*
            Status_Sceneario_Image_Complex table = new Status_Sceneario_Image_Complex();
            //SaveStatus save_load = new SaveStatus();
            Status_Scenario scenario = new Status_Scenario();
            table = SaveStatus.load(1);
            scenario = table.status_scenario;
            flag_dic = scenario.flag_dic;
            line_pos = scenario.line_pos;
            image1.Source = table.status_image;
            DateTime savetime = scenario.SaveTime;
            textBlock_MouseUp();
            */
        }

        private void button_Config_Click(object sender, RoutedEventArgs e)
        {
            NVS.Navigate(_uriList[0]);
            MainFrame.Visibility = Visibility.Visible;
            //textBlock_MouseUp();
        }

        private void button_Skip_Click(object sender, RoutedEventArgs e)
        {
            skip_read = !skip_read;
            if (skip_read_timer.IsEnabled == true)
                skip_read_timer.Stop();
            else
                skip_read_timer.Start();
        }

        private void button_Auto_Click(object sender, RoutedEventArgs e)
        {
            auto_read = !auto_read;
            if (auto_read_timer.IsEnabled == true)
                auto_read_timer.Stop();
            else
                auto_read_timer.Start();
            //auto_reader();
        }

        private void button_Log_Click(object sender, RoutedEventArgs e)
        {

        }
        private void auto_reader(object sender, EventArgs e)
        {
            auto_read_timer.Stop();
            textBlock_MouseUp();
            //auto_read_timer.Stop();
        }
        private void skip_reader(object sender, EventArgs e)
        {
            skip_read_timer.Stop();
            textBlock_MouseUp();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double x = Main_Window.Width;
            double y = Main_Window.Height;
            textBlock.Width = Text_Grid.Width - 100;
            CHARA_1.Width = Width / 3;
            CHARA_1.Margin = new Thickness(0, 0, 0, 0);
            CHARA_2.Width = Width / 3;
            CHARA_2.Margin = new Thickness(x / 3, 0, 0, 0);
            CHARA_3.Width = Width / 3;
            CHARA_3.Margin = new Thickness(x / 3 * 2, 0, 0, 0);
            background.Width = x;
            background.Height = y;

        }

        /*
        private void SaveWindow_gamestart_Click(object sender, RoutedEventArgs e)
        {
            
            image1.Source = Capture_Screen();
            //PageChange(PagesName.GameBoard);
        }*/
        private void PageChange(PagesName pn)
        {
            if(pn != PagesName.LoadWindow&& pn != PagesName.SaveWindow)
                PageChange_Last = pn;
            GameBoard.Visibility = Visibility.Hidden;
            GameTitle.Visibility = Visibility.Hidden;
            SaveWindow.Visibility = Visibility.Hidden;
            LoadWindow.Visibility = Visibility.Hidden;
            if (pn == PagesName.SaveWindow)
                SaveWindow.Visibility = Visibility.Visible;
            if (pn == PagesName.GameBoard)
                GameBoard.Visibility = Visibility.Visible;
            if (pn == PagesName.GameTitle)
                GameTitle.Visibility = Visibility.Visible;
            if (pn == PagesName.LoadWindow)
                LoadWindow.Visibility = Visibility.Visible;

        }
        PagesName PageChange_Last = PagesName.GameTitle;
        private void PageBack()
        {
            PageChange(PageChange_Last);
        }
        BitmapSource temp_source;
        private BitmapSource Capture_Screen()
        {
            using (var screenBmp = new Bitmap(
                (int)1500, (int)800, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (var bmpGraphics = Graphics.FromImage(screenBmp))
            {
                bmpGraphics.CopyFromScreen(
                    (int)draw_board.PointToScreen(new System.Windows.Point(0, 0)).X,
                    (int)draw_board.PointToScreen(new System.Windows.Point(0, 0)).Y,
                    0, 0, screenBmp.Size);
                return Imaging.CreateBitmapSourceFromHBitmap(
                    screenBmp.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                    );
            }
        }
        List<Status_Sceneario_Image_Complex> Save_tables = new List<Status_Sceneario_Image_Complex>();
        private void SaveWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SaveWindow_update();
        }
        private void SaveWindow_update()
        {
            if (SaveWindow.IsVisible)////////
            {
                Status_Sceneario_Image_Complex Save_table = new Status_Sceneario_Image_Complex();
                Status_Scenario scenario = new Status_Scenario();
                for (int i = 0; i < SAVEDATA_LENGTH; i++)
                {
                    Save_table = SaveStatus.load(i);
                    Save_tables.Add(Save_table);
                }
                SaveWindow_li1.Source = Save_tables[0].status_image;
                SaveWindow_li2.Source = Save_tables[1].status_image;
                SaveWindow_li3.Source = Save_tables[2].status_image;
                SaveWindow_li4.Source = Save_tables[3].status_image;
                SaveWindow_li5.Source = Save_tables[4].status_image;
                SaveWindow_li6.Source = Save_tables[5].status_image;
            }
        }
        private void Savedata_Selected(int id_no)
        {

            if (Save_tables[id_no].status_scenario == null)
            {
                SaveWindow_SaveButton.Visibility = Visibility.Visible;
                SaveWindow_Sam.Source = Save_tables[id_no].status_image;
                SaveWindow_Comment.Content = "データなし";
                SaveWindow_Time.Content = "でーたなし";
                Save_CurrentSerected = id_no;
                return;

            }
            SaveWindow_SaveButton.Visibility = Visibility.Visible;
            SaveWindow_Sam.Source = Save_tables[id_no].status_image;
            SaveWindow_Comment.Content = Save_tables[id_no].status_scenario.comment;
            DateTime dati = Save_tables[id_no].status_scenario.SaveTime;
            SaveWindow_Time.Content = dati.ToShortDateString() + "," + dati.ToShortTimeString();
            Save_CurrentSerected = id_no;
        }
        int Save_CurrentSerected;

        private void GameTitle_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (GameTitle.IsVisible)
            {
                if (SaveStatus.Savefile_Exist())
                {
                    GameTitle_Continue.Visibility = Visibility.Visible;
                    GameTitle_Continue_label.Visibility = Visibility.Visible;
                }
                else
                {
                    GameTitle_Continue.Visibility = Visibility.Hidden;
                    GameTitle_Continue_label.Visibility = Visibility.Hidden;
                }
            }
        }

        private void GameTitle_Start_MouseDown(object sender, MouseButtonEventArgs e)
        {
            line_pos = 0;
            scenario_read(@"Resources\scenario.csv");
            PageChange(PagesName.GameBoard);
        }

        private void GameTitle_Close_Click(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void GameTitle_savemode_Click(object sender, MouseButtonEventArgs e)
        {
            PageChange(PagesName.LoadWindow);

        }

        List<Status_Sceneario_Image_Complex> Load_tables = new List<Status_Sceneario_Image_Complex>();
        private void LoadWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //SaveStatus save_load = new SaveStatus();
            Status_Sceneario_Image_Complex Load_table = new Status_Sceneario_Image_Complex();
            Status_Scenario scenario = new Status_Scenario();
            for (int i = 0; i < SAVEDATA_LENGTH; i++)
            {
                Load_table = SaveStatus.load(i);
                Load_tables.Add(Load_table);
            }
            //scenario = Load_table.status_scenario;
            //flag_dic = scenario.flag_dic;
            //line_pos = scenario.line_pos;
            //DateTime savetime = scenario.SaveTime;
            LoadWindow_li1.Source = Load_tables[0].status_image;
            LoadWindow_li2.Source = Load_tables[1].status_image;
            LoadWindow_li3.Source = Load_tables[2].status_image;
            LoadWindow_li4.Source = Load_tables[3].status_image;
            LoadWindow_li5.Source = Load_tables[4].status_image;
            LoadWindow_li6.Source = Load_tables[5].status_image;
        }

        private void LoadData_Selected(int id_no)
        {
            if (Load_tables[id_no].status_scenario == null) return;
            LoadWindow_LoadButton.Visibility = Visibility.Visible;
            LoadWindow_Sam.Source = Load_tables[id_no].status_image;
            LoadWindow_Comment.Content = Load_tables[id_no].status_scenario.comment;
            DateTime dati = Load_tables[id_no].status_scenario.SaveTime;
            LoadWindow_Time.Content = dati.ToShortDateString() + "," + dati.ToShortTimeString();
            Load_CurrentSerected = id_no;
        }
        int Load_CurrentSerected;
        private void LoadWindow_li1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadData_Selected(0);
        }
        private void LoadWindow_li2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadData_Selected(1);
        }
        private void LoadWindow_li3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadData_Selected(2);
        }

        private void LoadWindow_li4_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadData_Selected(3);
        }

        private void LoadWindow_li5_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadData_Selected(4);
        }

        private void LoadWindow_li6_MouseDown(object sender, MouseButtonEventArgs e)
        {
            LoadData_Selected(5);
        }

        private void LoadWindow_LoadButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Status_Sceneario_Image_Complex table = new Status_Sceneario_Image_Complex();
            //SaveStatus save_load = new SaveStatus();
            Status_Scenario scenario = new Status_Scenario();
            table = SaveStatus.load(Load_CurrentSerected);
            scenario = table.status_scenario;
            flag_dic = scenario.flag_dic;
            line_pos = scenario.line_pos;
            BGimage_change(scenario.BackIMG);
            DateTime savetime = scenario.SaveTime;
            chara_setup(scenario.CharaIMG);
            PageChange(PagesName.GameBoard);
            textBlock_MouseUp();
        }
        private void chara_setup(Dictionary<MainWindow.POSITION, string> charas)
        {
            if (charas.ContainsKey(POSITION.LEFT))
                CHimage_change(charas[POSITION.LEFT], POSITION.LEFT);
            if (charas.ContainsKey(POSITION.CENTER))
                CHimage_change(charas[POSITION.CENTER], POSITION.CENTER);
            if (charas.ContainsKey(POSITION.RIGHT))
                CHimage_change(charas[POSITION.RIGHT], POSITION.RIGHT);

        }

        private void label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://densan.club/creation-content/%E5%89%B5%E4%BD%9C%E7%89%A91test/");
            }
            catch { }
        }

        private void SaveWindow_li1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Savedata_Selected(0);
        }
        private void SaveWindow_li2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Savedata_Selected(1);
        }
        private void SaveWindow_li3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Savedata_Selected(2);
        }
        private void SaveWindow_li4_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Savedata_Selected(3);
        }
        private void SaveWindow_li5_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Savedata_Selected(4);
        }
        private void SaveWindow_li6_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Savedata_Selected(5);
        }

        private void SaveWindow_SaveButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Status_Save(Save_CurrentSerected);
            SaveWindow_update();
        }

        private void LoadWindow_Back_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PageBack();
        }

        private void SaveWindow_Back_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PageBack();
        }

        private void image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void GameTitle_Setting_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Setting_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {

        }
    }
}
