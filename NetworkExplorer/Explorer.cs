using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetworkExplorer
{
    internal class Explorer
    {
        private object _zamek = new object();
        public List<Device> hosts = new List<Device>();
        byte[] localIP = new byte[4];

        //todo: consider using "ushort" as data type since maximum TCP port is 2^16 - 1 (65535)
        //klic k portum: 20,21=ftp, 22=ssh, 23=telnet, 25=smtp, 53=dns, 80=http, 110=pop3, 143=imap, 194=irc, 443=https, 3389=RDP
        private int[] wellknownPorts = { 80, 23, 443, 21, 22, 25, 3389, 110, 445, 139, 143, 53, 135, 3306, 8080, 1723, 111, 995, 993, 5900 };


        public Explorer() { } //todo: tohle mby bude potreba, asi na predani adresy a masky a options

        internal async Task PingSweepRange(byte[] ipToScan, byte mask)
        {

            List<Task> pings = new List<Task>();
            byte[] networkIP = GetNetAddress(ipToScan, mask);
            int numberOfHosts = GetNumberOfHosts(ipToScan, mask);

            //TODO: tohle je jenom debug    
            Console.WriteLine("network address: " + String.Join('.', networkIP));
            Console.WriteLine("spocitano povolenych host ip na siti: " + numberOfHosts);

            IsLocalMachineConnectedToScannedNet(networkIP, numberOfHosts); //sice nic nedelam s outputem, ale volam tuto metodu,
                                                                           //abych si priradil do promenne "localIP" moji ip ktera je na spolecnem subnetu jako skenovane ip

            //the actual ping loop.
            for (UInt32 i = IPtoUInt32(networkIP) + 1; i <= IPtoUInt32(networkIP) + numberOfHosts; i++)
            {
                if (UInt32toIP(i).SequenceEqual(localIP))
                {
                    //don't ping my ip, cause user prolly doesn't care whether his own pc is running
                    continue;
                }

                Ping ping = new Ping();
                var task = PingAsync(ping, UInt32toIP(i), hosts);
                pings.Add(task);

            }

            //pocka na dokonceni scanu
            Task.WhenAll(pings).Wait();
            //await Task.WhenAll(pings).ContinueWith(p =>
            //{
            //    //do stuff az se dodelaji pingy
            //    //(vypsat result)
            //});

            Console.Write("Scan dokoncen. ");

            if (hosts.Count == 0)
            {//pokud jsem nenasel zadne aktivni hosty, returnu
                Console.WriteLine("Nebyli nalezeni zadni aktivni hoste na zadane siti!");
                return;
            }

            Console.WriteLine("Nalezeno ip: " + hosts.Count);
            //string baseIP = String.Join('.', ipToScan.Take(3)); 
            //Console.WriteLine("base ip: " + baseIP);
            //Console.WriteLine("pingu (v pings): " + pings.Count);

            Console.WriteLine("Nalezeni hoste: ");

            foreach (Device host in hosts)//TODO: vypisovani hostu uz za behu 
            {
                Console.WriteLine(String.Join('.', host.IP) + " je online!");

                Task<String> macTask = GetMACAndManufacturerAsync(host.IP);
                macTask.Wait();
                string mac = macTask.Result;
                Console.WriteLine("\t" + mac);
            }


            //todo: dat hostname detection do samostatne metody
            #region !! nemazat !! potreba presunot do metody nebo tak neco (hostnames pocitacu)
            //Console.WriteLine("Trying to look up devices' hostnames");
            //foreach (Device device in hosts)
            //{//TODO: VERY GOOD THING, WORKS  ZOBRAZOVAT TENHLE VYSLEDEK NEKDE JINDE
            //    try
            //    {
            //        Console.Write(string.Join('.', new IPAddress(device.IP)) + " - ");
            //        Console.WriteLine(Dns.GetHostEntry(new IPAddress(device.IP)).HostName);
            //    }
            //    catch
            //    {
            //        Console.WriteLine("DNS jmeno nenalezeno, pravdepodobne z duvodu nedostupneho DNS serveru");
            //    }
            //}
            #endregion
        }

        internal void PingAdress(byte[] ip)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(String.Join('.', ip), 1000);    //1000 je timout v [ms]

            //Console.WriteLine($"Status: {reply.Status}\nRoundtrip time: {reply.RoundtripTime}\nAdress: {reply.Address}\nOptions: {reply.Options}");
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Scanned IP " + reply.Address + " and host is UP");

                hosts.Add(new Device(ip));

                ////todo: extract do samostatne metod
                //Console.WriteLine("Trying to look up device's hostname");
                //try
                //{
                //    Console.Write(string.Join('.', reply.Address) + " - ");
                //    Console.WriteLine(Dns.GetHostEntry(reply.Address).HostName);
                //}
                //catch
                //{
                //    Console.WriteLine("DNS jmeno nenalezeno, pravdepodobne z duvodu nedostupneho DNS serveru");
                //}                
            }

            else
            {
                Console.WriteLine("Host on " + reply.Address + " is either down or ICMP packets are filtered.");
            }
        }

        public List<string> ScanWellKnownPorts(byte[] ip)
        {
            List<Task> portScans = new List<Task>();
            List<string> ports = new List<string>();

            foreach (int port in wellknownPorts)
            {
                int refPort = port - 1;
                Task<string> scan = Task.Run(() =>
                {
                    return GetPortInfo(ip, ref refPort);
                });

                portScans.Add(scan);

                var awaiter = scan.GetAwaiter();

                awaiter.OnCompleted(() =>
                {
                    string text = awaiter.GetResult();
                    if (text != "")
                    {
                        ports.Add(text);
                    }
                });
            }

            //zobrazovani progresu
            while (Task.WhenAll(portScans).Status != TaskStatus.RanToCompletion)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    int done = portScans.Count(s => s.Status == TaskStatus.RanToCompletion);
                    Console.WriteLine($"{done}/{portScans.Count} ports scanned");

                }
            }

            return ports;
        }

        private bool IsPortOpen(byte[] ip, int port)
        {
            using (System.Net.Sockets.TcpClient tcpClient = new System.Net.Sockets.TcpClient())
            {
                try
                {
                    tcpClient.Connect(string.Join('.', ip), port);

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public string ScanPort(byte[] ip, int port)
        {
            int portNr = port - 1;  // this hack needed protoze aby to sknovani potu bylo thread safe, tak v GetPortInfo nejdrive inkrementuju cislo portu nez ho skenuju, takze musim zacinat o 1 nize
            return GetPortInfo(ip, ref portNr);
        }

        public List<string> ScanPortRange(byte[] ip, int startPort, int endPort)
        {
            List<Task> portScans = new List<Task>();
            List<string> ports = new List<string>();

            int port = startPort - 1;   //tenhle hack je potreba protoze aby metoda GetPortInfo byla thread safe, tak nejdrive inkrementuju cislo portu nez ho skenuju, takze musim zacinat o 1 nize

            for (int i = 0; i <= endPort - startPort; i++)
            {
                Task<string> scan = Task.Run<string>(() =>
                {
                    return GetPortInfo(ip, ref port);
                });

                portScans.Add(scan);

                var awaiter = scan.GetAwaiter();

                awaiter.OnCompleted(() =>
                {
                    string text = awaiter.GetResult();
                    if (text != "")
                    {
                        ports.Add(text);
                    }
                });
            }

            //zobrazovani progresu
            while (Task.WhenAll(portScans).Status != TaskStatus.RanToCompletion)
            {
                if (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                    int done = portScans.Count(s => s.Status == TaskStatus.RanToCompletion);
                    Console.WriteLine($"{done}/{portScans.Count} ports scanned");

                }
            }

            return ports;
        }

        private string GetPortInfo(byte[] ip, ref int portCounter)
        {
            int port = Interlocked.Increment(ref portCounter);

            using (TcpClient tcpClient = new TcpClient())
            {
                try
                {
                    tcpClient.Connect(string.Join('.', ip), port);
                    NetworkStream stream = tcpClient.GetStream();

                    stream.ReadTimeout = 1000;//todo: don't use magic number
                    String responseData = String.Empty;
                    byte[] data = new Byte[256];
                    Int32 bytes = stream.Read(data, 0, data.Length);

                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);

                    stream.Close();
                    tcpClient.Close();
                    return port + ": " + responseData;
                }
                catch (Exception ex)
                {
                    if (typeof(System.IO.IOException) == ex.GetType())
                    {
                        return port + ": open, but failed to respond";
                    }
                    else
                    {
                        //return OOO + ": closed";
                        return "";
                    }
                }
            }
        }

        private bool IsLocalMachineConnectedToScannedNet(byte[] networkIP, int numberOfHosts)
        {
            //Console.WriteLine("my ip: " + Dns.GetHostEntry(Dns.GetHostName())
            //    .AddressList
            //    .First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            //    .ToString());
            //Console.WriteLine("muj hostname: " + Dns.GetHostName());
            //Console.WriteLine("pocet mych hostnamu: " + Dns.GetHostEntry(Dns.GetHostName()).AddressList.Length);

            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(ip.ToString(), new(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")))//check for ipv4 ip
                {
                    byte[] byteIP = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        byteIP[i] = byte.Parse(ip.ToString().Split('.')[i]);
                    }

                    if (IPtoUInt32(byteIP) > IPtoUInt32(networkIP) && IPtoUInt32(byteIP) < IPtoUInt32(networkIP) + numberOfHosts)
                    {
                        Console.WriteLine("Nase IP " + ip.ToString() + " je na skenovanem networku");
                        localIP = Array.ConvertAll(ip.ToString().Split('/')[0].Split('.'), x => byte.Parse(x));

                        //toto asi presunuto do primo scanovaci smycky
                        ////smazu ip tohoto pocitace pokud je pripojen ke skenovane siti, protze nas nezajima
                        //Device device = hosts.Find(host => host.IP.SequenceEqual(localIP));
                        //hosts.Remove(device);   //TODO: BACHA MBY NA NULL ERRORY //EDIT: null errory nejou, akorat se to proste nesmaze
                        ////Console.WriteLine("smazal jsem nasi ip: " + hosts.Remove(device));  

                        return true;
                    }
                }
            }
            return false;//nenasla se zadna nase ip, ktera by byla na skenovanem networku
        }

        public async Task<string> GetMACAndManufacturerAsync(byte[] ip)
        {
            string output = "";

            //checknu ARP table
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
            info.FileName = "cmd.exe";
            info.Arguments = "/C arp -a";
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            process.StartInfo = info;

            process.Start();

            //tyhle dva regexy jsou kdyz bych hledat danou IP adresu jenom na urcitem localnim intereface. To snad ale nebude potreba
            //Regex regex = new Regex("(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))"); //(192\.168\.0\.103)\s+(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))
            //string regexString = @"(192\.168\.0\.103)\s+(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))";

            string regexString = @"\s+(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))"; //regex na MAC adresu
            Regex regex = new Regex("(" + String.Join(@"\.", ip) + ")" + regexString);
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine() ?? "";

                Match match = regex.Match(line);

                if (match.Success)
                {
                    string mac = match.Groups["mac"].Value;
                    output += mac;

                    //ping na zjisteni pristupu k internetu 
                    Ping cfPing = new Ping(); //cf protoze pinguju Cloud Flare DNS
                    PingReply cfReply = await cfPing.SendPingAsync("1.1.1.1", 1000);

                    if (cfReply.Status == IPStatus.Success) //check dostupnosti internetoveho pripojeni
                    {
                        //zjistim vyrobce sitove karty zarizeni
                        //string url = "https://api.macvendors.com/" + mac;
                        string url = "https://macvendors.co/api/vendorname/" + mac; //tato api ma apparently vetsi databazi a ta minula me nejak timeoutovala iirc

#pragma warning disable SYSLIB0014  //stfu for now, predelam mby l8tr az zjistim proc je to absolete
                        WebRequest wrGETURL = WebRequest.Create(url);
#pragma warning restore SYSLIB0014

                        Stream objStream;
                        objStream = wrGETURL.GetResponse().GetResponseStream();

                        StreamReader objReader = new StreamReader(objStream);

                        string sLine = "";
                        while (sLine != null)
                        {
                            sLine = objReader.ReadLine();
                            if (sLine != null)
                                output += " - " + sLine;
                        }
                    }
                    else
                    {
                        output += " - pro zjisteni vyrobce zarizeni se pripojte k internetu";
                    }
                }
            }
            return output;
        }

        private async Task PingAsync(Ping ping, byte[] ip, List<Device> hosts)
        {
            var reply = await ping.SendPingAsync(string.Join('.', ip), 100);

            if (reply.Status == IPStatus.Success)
            {
                //TODO: tady nejaky vypis co jsem nasel ig

                Console.WriteLine("Nalezeno bezici zarizeni s ip " + String.Join('.', ip));
                lock (_zamek)
                {
                    hosts.Add(new Device(ip));
                }
            }
        }

        private byte[] GetNetAddress(byte[] ip, byte mask)
        {
            string binMask = "";
            for (byte i = 0; i < mask; i++) { binMask += "1"; }
            binMask = binMask.PadRight(32, '0');

            byte[] maskOktety = new byte[4];
            byte[] netOktety = new byte[4];

            for (int i = 0; i < 4; i++) { maskOktety[i] = Convert.ToByte(binMask.Substring(i * 8, 8), fromBase: 2); }

            for (int i = 0; i < 4; i++) { netOktety[i] = (byte)(ip[i] & maskOktety[i]); }

            return netOktety;
        }

        private int GetNumberOfHosts(byte[] ip, byte mask)
        {
            //UInt32 netAdd = IPtoUInt32(GetNetAddress(ip, mask));
            //Console.WriteLine("netAdd in GetNumberOfHosts: " + netAdd);
            ////UInt32 binaryMask = IPtoUInt32(Convert.ToString(mask, 2).PadLeft(32, '0'));
            //GetNetAddress(ip, mask).ToList().ForEach(x => Console.Write(Convert.ToString(x, 2).PadLeft(8,'0'))); Console.WriteLine();
            //Console.WriteLine(new StringBuilder().Insert(0, "1", mask).ToString().PadRight(32, '0'));
            //UInt32 binaryMask = Convert.ToUInt32(new StringBuilder().Insert(0, "1", mask).ToString().PadRight(32, '0'), 2);
            //Console.WriteLine("binarymask in GetNumberOfHosts: " + binaryMask);

            //Console.WriteLine("Nr of hosts: " + (binaryMask - netAdd));
            //return (int)(netAdd - binaryMask);


            // predpokladam ze maska je mezi 0 a 30; 31 a 32 jsem vyfiltroval pri zadavani 
            return ((int)Math.Pow(2, 32 - mask)) - 2; //lmao it's that simple, just 2 to the power of the number of bits minus 2 (net. add. and broadcast)

        }

        //ipNumber will look for example like this: 3232235830, but it means 11000000101010000000000100110110 in binary, which is easy transalte to 11000000.10101000.00000001.00110110 => 192.168.1.54
        private byte[] UInt32toIP(UInt32 ipNumber)
        {
            string ipBinString = Convert.ToString(ipNumber, 2).PadLeft(32, '0');//convert the weird number to binary and pad it to have 32 characters.
            byte[] ipArray = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ipArray[i] = Convert.ToByte(ipBinString.Substring(i * 8, 8), 2);
            }
            return ipArray;
        }

        private UInt32 IPtoUInt32(byte[] ip)
        {
            string ipBinString = "";
            for (int i = 0; i < 4; i++)
            {
                ipBinString += Convert.ToString(ip[i], 2).PadLeft(8, '0');
            }
            return Convert.ToUInt32(ipBinString, 2);
        }
    }
}