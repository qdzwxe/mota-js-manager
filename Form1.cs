using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Microsoft.Win32;

using SimpleHttpServer;
using SimpleHttpServer.Models;
using SimpleHttpServer.RouteHandlers;

// This is the code for your desktop app.
// Press Ctrl+F5 (or go to Debug > Start Without Debugging) to run your app.

namespace h5manager
{
    public partial class Form1 : Form
    {
        private DirectoryInfo[] dire;

        private int port;
        private string url;
        private string text;
        private bool showError = true;

        private Dictionary<string, int> opened = new Dictionary<string, int>();
        private List<HttpServer> httpServers = new List<HttpServer>();
        private List<Thread> threads = new List<Thread>();
        private List<MyRoute> myRoutes = new List<MyRoute>();

        //private List<string> files = new List<string>();
        private string[] toBeCopied_file = { "editor.html", "editor-mobile.html", "index.html", "logo.png", "main.js", "runtime.d.ts", "styles.css", "启动服务.exe", "server.py", "B站视频教程.url" };
        private string[] toBeCopied_dir = { "project" };
        private string[] toBeLinked_dir = { "_docs", "_server", "extensions", "libs", "常用工具" }; //, "project"

        public static string proj;

        public Form1()
        {
            InitializeComponent();
        }

        private void toDire(string path)
        {
            DirectoryInfo root = new DirectoryInfo(path);
            DirectoryInfo[] dics = root.GetDirectories();
            dire = dics;
            if (dics.Length > 0)
            {
                comboBox1.Items.Clear();
                comboBox2.Items.Clear();
                foreach (DirectoryInfo dic in dics)
                {
                    comboBox1.Items.Add(dic.ToString());
                    comboBox2.Items.Add(dic.ToString());
                }

            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string path1 = "template";

            if (Directory.Exists(path1))
            {
                string[] toBeTested_dir = { "_docs", "_server", "extensions", "libs", "project" }; //, "常用工具"
                string[] toBeTested_file = { "editor.html", "index.html", "logo.png", "main.js", "runtime.d.ts", "styles.css", "启动服务.exe" };//, "editor-mobile.html", "server.py","B站视频教程.url"
                bool check = true;
                foreach (string dir in toBeTested_dir)
                {
                    if (!Directory.Exists(path1 + "\\" + dir))
                    {
                        check = false;
                        break;
                    }
                }
                foreach (string file in toBeTested_file)
                {
                    if (!File.Exists(path1 + "\\" + file))
                    {
                        check = false;
                        break;
                    }
                }
                if (check)
                {
                    string path2 = "projects";
                    toDire(path2);
                    /*DirectoryInfo[] dics = dire;

                    if (dics.Length > 0)
                    {
                        foreach (DirectoryInfo dic in dics)
                        {
                            comboBox1.Items.Add(dic.ToString());
                            comboBox2.Items.Add(dic.ToString());
                        }

                    }*/

                }
                else
                {
                    MessageBox.Show("缺少必要文件！请自行检查或与作者联系。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    System.Environment.Exit(1);
                }
            }
            else
            {
                MessageBox.Show("缺少必要文件！请自行检查或与作者联系。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                System.Environment.Exit(1);
            }


        }

        private bool portInUse(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipGlobalProperties.GetActiveTcpListeners();
            foreach (IPEndPoint ipEndPoint in ipEndPoints)
            {
                if (ipEndPoint.Port == port) return true;
            }
            return false;
        }

        private bool checkChrome()
        {
            RegistryKey browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Clients\StartMenuInternet");
            if (browserKeys == null)
                browserKeys = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet");
            string[] names = browserKeys.GetSubKeyNames();
            foreach (string name in names)
            {
                if (name.ToLower().Contains("chrome"))
                    return true;
            }
            return false;
        }

        private void openUrl(string url)
        {

            if (checkChrome())
            {
                try
                {
                    Process.Start("chrome.exe", url);
                    return;
                }
                catch (Exception)
                {
                }
            }

            if (showError)
            {
                MessageBox.Show("你当前没有安装Chrome浏览器，使用其他浏览器可能会导致本程序闪退或无法正常工作，强烈推荐下载Chrome浏览器后再进行操作。",
                    "警告", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                showError = false;
            }

            Process.Start(url);
        }

        private void copyFile(string sourcePath, string name, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                try
                {
                    File.Copy(sourcePath + "\\" + name, destPath + "\\" + name);
                }
                catch
                {
                    MessageBox.Show("复制失败，文件已存在！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
        }

        private void copyDir(string srcPath, string aimPath)
        {
            try
            {
                // 检查目标目录是否以目录分割字符结束如果不是则添加之
                if (aimPath[aimPath.Length - 1] != System.IO.Path.DirectorySeparatorChar)
                {
                    aimPath += System.IO.Path.DirectorySeparatorChar;
                }

                // 判断目标目录是否存在如果不存在则新建之
                if (!System.IO.Directory.Exists(aimPath))
                {
                    System.IO.Directory.CreateDirectory(aimPath);
                }

                // 得到源目录的文件列表，该里面是包含文件以及目录路径的一个数组
                // 如果你指向copy目标文件下面的文件而不包含目录请使用下面的方法
                // string[] fileList = Directory.GetFiles(srcPath);
                string[] fileList = System.IO.Directory.GetFileSystemEntries(srcPath);

                // 遍历所有的文件和目录
                foreach (string file in fileList)
                {
                    // 先当作目录处理如果存在这个目录就递归Copy该目录下面的文件
                    if (System.IO.Directory.Exists(file))
                    {
                        copyDir(file, aimPath + System.IO.Path.GetFileName(file));
                    }

                    // 否则直接Copy文件
                    else
                    {
                        System.IO.File.Copy(file, aimPath + System.IO.Path.GetFileName(file), true);
                    }
                }
            }

            catch (Exception e)
            {
                throw;
            }
        }

        private void runCmd(string cmd)
        {
            Process proc = new Process();
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            proc.StandardInput.WriteLine(cmd);
            proc.Close();
        }

        private void createProj(string path, string pathProj)
        {
            if (pathProj == null) pathProj = "template";
            DirectoryInfo dir = new DirectoryInfo(path);
            dir.Create();

            foreach (string file in toBeCopied_file)
            {
                copyFile("template", file, path);
            }
            foreach (string di in toBeCopied_dir)
            {
                copyDir(pathProj + "\\" + di, path + "\\" + di);
            }
            foreach (string di in toBeLinked_dir)
            {
                string cmds = "";
                if (Directory.Exists("template\\" + di))
                {
                    string cmd = "mklink /j ";
                    cmd += Application.StartupPath + "\\" + path + "\\" + di + " ";
                    cmd += Application.StartupPath + "\\template\\" + di;
                    cmds += cmd + "\n";
                    //MessageBox.Show(cmd);

                }
                runCmd(cmds);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length != 0)
            {
                DirectoryInfo[] dics = dire;
                if (dics.Length > 0)
                {
                    foreach (DirectoryInfo dic in dics)
                    {
                        if (textBox1.Text == dic.ToString())
                        {
                            MessageBox.Show("目录已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            return;
                        }
                    }

                }

                string path1 = "projects\\" + textBox1.Text;

                createProj(path1, "template");

                toDire("projects");

                MessageBox.Show("创建成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                textBox1.Text = null;

            }
            else
            {
                MessageBox.Show("请输入名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (comboBox1.Text.Length != 0)
            {
                proj = comboBox1.Text;
                string path1 = "projects\\" + proj;
                if (Directory.Exists(path1))
                {
                    foreach (string tower in opened.Keys)
                    {
                        if (tower == proj) return;
                    }
                    //num = 0;
                    port = 1055;
                    while (portInUse(port))
                    {
                        //num++;
                        port++;
                    }


                    //url = "http://127.0.0.1:" + port + "/";
                    myRoutes.Add(new MyRoute());

                    // 启动
                    httpServers.Add(new HttpServer(port, new List<Route>()
                    {
                        new Route()
                        {
                            Callable = myRoutes[myRoutes.Count-1].getHandler,
                            UrlRegex = "^/(.*)$",
                            Method = "GET"
                        },
                        new Route()
                        {
                            Callable = myRoutes[myRoutes.Count-1].postHandler,
                            UrlRegex = "^/(.*)$",
                            Method = "POST"
                        },
                    }));

                    threads.Add(new Thread(new ThreadStart(httpServers[httpServers.Count - 1].Listen)));
                    threads[threads.Count - 1].Start();

                    opened.Add(proj, port);
                    listBox1.Items.Add(proj + ":" + port);
                    listBox1.SelectedIndex = listBox1.Items.Count - 1;
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (textBox3.Text.Length != 0)
            {
                DirectoryInfo[] dics = dire;
                if (dics.Length > 0)
                {
                    foreach (DirectoryInfo dic in dics)
                    {
                        if (textBox3.Text == dic.ToString())
                        {
                            MessageBox.Show("目录已存在", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            return;
                        }
                    }

                    //string path = Application.StartupPath;
                    string path1 = "projects\\" + textBox3.Text;

                    createProj(path1, textBox2.Text);

                    MessageBox.Show("导入成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    toDire("projects");

                    textBox2.Text = null;
                    textBox3.Text = null;
                }
            }
            else
            {
                MessageBox.Show("请输入名称", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                Environment.Exit(System.Environment.ExitCode);
            }
            catch (Exception)
            {

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                text = listBox1.SelectedItem.ToString();
                url = "http://127.0.0.1:" + text.Split(':')[1] + "/";
                openUrl(url + "index.html");
            }
            else
            {
                MessageBox.Show("请选择工程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                text = listBox1.SelectedItem.ToString();
                url = "http://127.0.0.1:" + text.Split(':')[1] + "/";
                openUrl(url + "editor.html");
            }
            else
            {
                MessageBox.Show("请选择工程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != null)
            {
                int num = listBox1.SelectedIndex;
                text = listBox1.SelectedItem.ToString().Split(':')[0];

                httpServers[num].Stop();
                httpServers.RemoveAt(num);
                threads[num].Abort();
                //threads[num].Interrupt();
                //threads[num].Join();
                //MessageBox.Show(threads[num].IsAlive.ToString());

                threads.RemoveAt(num);
                listBox1.Items.RemoveAt(num);
                opened.Remove(text);
                myRoutes.RemoveAt(num);

            }
            else
            {
                MessageBox.Show("请选择工程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择导入路径";
            dialog.ShowNewFolderButton = false;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string foldPath = dialog.SelectedPath;
                if (Directory.Exists(foldPath + "\\project"))
                {
                    textBox2.Text = dialog.SelectedPath;
                    string[] autoNames = textBox2.Text.Split('\\');
                    textBox3.Text = autoNames[autoNames.Length - 1];
                }
                else
                {
                    MessageBox.Show("请选择工程根目录", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择导出路径";
            dialog.ShowNewFolderButton = true;
            //dialog.RootFolder = Environment.CurrentDirectory;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                textBox4.Text = dialog.SelectedPath;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (textBox4.Text.Length != 0)
            {
                if (comboBox2.Text.Length != 0)
                {
                    copyDir("projects\\" + comboBox2.Text, textBox4.Text + "\\" + comboBox2.Text);

                    DialogResult dr = MessageBox.Show("导出成功，是否需要打开相应文件夹？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                    if (dr == DialogResult.Yes)
                    {
                        //MessageBox.Show(textBox4.Text + "\\" + comboBox2.Text, "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        System.Diagnostics.Process.Start("explorer.exe", textBox4.Text + "\\" + comboBox2.Text);
                    }
                }
                else
                {
                    MessageBox.Show("请选择导出工程", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("请选择导出路径", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", Application.StartupPath + "\\projects\\" + comboBox1.Text);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            if (!File.Exists("template\\常用工具\\便捷PS工具.exe"))
            {
                MessageBox.Show("找不到常用工具目录下的便捷PS工具！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Process.Start("template\\常用工具\\便捷PS工具.exe");
        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (!File.Exists("template\\常用工具\\RM动画导出器.exe"))
            {
                MessageBox.Show("找不到常用工具目录下的RM动画导出器！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Process.Start("template\\常用工具\\RM动画导出器.exe");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (!File.Exists("template\\常用工具\\怪物数据导出器.exe"))
            {
                MessageBox.Show("找不到常用工具目录下的怪物数据导出器！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Process.Start("template\\常用工具\\怪物数据导出器.exe");
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (!File.Exists("template\\常用工具\\动画编辑器.exe"))
            {
                MessageBox.Show("找不到常用工具目录下的动画编辑器！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Process.Start("template\\常用工具\\动画编辑器.exe");
        }

        private void button16_Click(object sender, EventArgs e)
        {
            if (!File.Exists("template\\常用工具\\伤害和临界值计算器.exe"))
            {
                MessageBox.Show("找不到常用工具目录下的伤害和临界值计算器！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Process.Start("template\\常用工具\\伤害和临界值计算器.exe");
        }

        private void button15_Click(object sender, EventArgs e)
        {
            if (opened.Count != 0)
            {
                url = "http://127.0.0.1:" + (listBox1.SelectedItem.ToString().Split(':')[1]) + "/_docs/";
            }
            else
            {
                url = "https://h5mota.com/games/template/_docs";
            }

            openUrl(url);
        }

        private void button11_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show("迁移本工具或修改过存储目录后，可尝试对工程进行修复，是否继续？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (dr == DialogResult.Yes)
            {
                foreach (DirectoryInfo dir in dire)
                {
                    string path = "projects\\" + dir.ToString();
                    foreach (string di in toBeLinked_dir)
                    {
                        string path1 = path + "\\" + di;
                        int l1 = 0;
                        int l2 = 0;

                        if (Directory.Exists(path1))
                        {
                           // bool deleted = false;
                            try
                            {
                                l1 = Directory.GetDirectories(path1).Length;
                                l2 = Directory.GetFiles(path1).Length;
                            }
                            catch
                            {

                            }
                            finally
                            {
                                //Directory.Delete(path1);
                                //deleted = true;
                            }
                            if (!(l1 > 0 || l2 > 0)) Directory.Delete(path1);
                        }
                        //Directory.
                        if (!(l1 > 0 || l2 > 0))
                        {
                            string cmds = "";
                            if (Directory.Exists("template\\" + di))
                            {
                                string cmd = "mklink /j ";
                                cmd += Application.StartupPath + "\\" + path1 + " ";
                                cmd += Application.StartupPath + "\\template\\" + di;
                                cmds += cmd + "\n";
                                //MessageBox.Show(cmd);

                            }
                            runCmd(cmds);

                        }                      
                    }

                }
                MessageBox.Show("修复完毕。若仍不能使用，请使用导入功能再次修复！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

            }
            else
            {

            }
        }
    }

}
