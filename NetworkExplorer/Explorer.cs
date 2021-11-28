using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NetworkExplorer
{
    internal class Explorer
    {
        private object _zamek = new object();
        List<Device> hosts = new List<Device>();
        byte[] localIP = new byte[4];

        public Explorer() //todo: tohle mby bude potreba, asi na predani adresy a masky a options
        { }

        internal async Task PingSweepRange(byte[] ipToScan, byte mask)
        {

            List<Task> pings = new List<Task>();
            byte[] networkIP = GetNetAddress(ipToScan, mask);
            int numberOfHosts = GetNumberOfHosts(ipToScan, mask);

            //TODO: tohle je jenom debug    
            Console.WriteLine("network address: " + String.Join('.', networkIP));
            Console.WriteLine("spocitano povolenych host ip na siti: " + numberOfHosts);



            for (UInt32 i = IPtoUInt32(networkIP) + 1; i <= IPtoUInt32(networkIP) + numberOfHosts; i++)
            {
                //Console.WriteLine(String.Join('.', UInt32toIP(i)));

                Ping ping = new Ping();
                var task = PingAsync(ping, UInt32toIP(i));
                pings.Add(task);

            }

            string baseIP = String.Join('.', ipToScan.Take(3));
            Console.WriteLine("base ip: " + baseIP);
            Console.WriteLine("pingu (v pings): " + pings.Count);
            Console.WriteLine("hostu (v hosts):" + hosts.Count);

            //pocka na
            await Task.WhenAll(pings).ContinueWith(p =>
            {
                //do stuff az se dodelaji pingy
                //(vypsat result)
                Console.WriteLine("Nalezeno ip: " + hosts.Count);
            });

            //await Task.WhenAll(pings);//tohle to nidky nereachne?
            Console.WriteLine(hosts.Count + "hostu");
            Console.WriteLine("Nalezeni hoste: ");
            foreach (Device host in hosts)//TODO: vypisovani hostu uz za behu
            {
                Console.WriteLine(String.Join('.', host.IP));
            }

            IsLocalMachineConnectedToScannedNet(networkIP, numberOfHosts); //sice nic nedelam s outputem, ale volam tuto metodu,
                                                                           //abych si priradil do promenne "localIP" moji ip ktera je na spolecnem subnetu jako skenovane ip

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
                        return true;
                    }
                }
            }
            return false;//nenasla se zadna nase ip, ktera by byla na skenovanem networku
        }

        public string GetMACAndManufacturer(byte[] ip)
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

            //Regex regex = new Regex("(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))"); //(192\.168\.0\.103)\s+(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))
            //string regexString = @"(192\.168\.0\.103)\s+(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))";
            string regexString = @"\s+(?'mac'([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2}))";
            Regex regex = new Regex("(" + String.Join(@"\.", ip) + ")" + regexString);
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine() ?? "";
                //Console.WriteLine(line);
                Match match = regex.Match(line);

                if (match.Success)
                {
                    string mac = match.Groups["mac"].Value;
                    output += mac;


                    Ping cfPing = new Ping();
                    PingReply cfReply = cfPing.Send("1.1.1.1", 1000);
                    if (cfReply.Status == IPStatus.Success)//check dostupnosti internetoveho pripojeni
                    {
                        //zjistim vyrobce sitove karty zarizeni
                        //string url = "https://api.macvendors.com/" + mac;
                        string url = "https://macvendors.co/api/vendorname/" + mac;

                        WebRequest wrGETURL = WebRequest.Create(url);

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
                        Console.WriteLine("neni pristup k interenetu");
                        //pokud nemam pripojeni k internetu, vypisu jenom MAC adresu, mozna pozdeji se rozhodnu pro j=nejakou hlasku
                        //Console.WriteLine("no interenet connection");
                    }
                }
            }
            return output;
        }


        private async Task PingAsync(Ping ping, byte[] ip)
        {
            var reply = await ping.SendPingAsync(string.Join('.', ip), 100);

            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("yes");
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
        /*
        //converts 192.168.1.54 to 11000000.10101000.00000001.00110110 to UInt32 in binary (11000000101010000000000100110110) which is 3232235830 and is easier to iterate upon
        //private UInt32 IPtoUint32(string ip)
        //{
        //    byte[] ipArray = new byte[4];
        //    for (int i = 0; i < 4; i++)
        //    {
        //        ipArray[i] = byte.Parse(ip.Split('.')[i]);//todo:???broken??
        //    }
        //    string ipBinString = "";
        //    for (int i = 0; i < 4; i++)
        //    {
        //        ipBinString += Convert.ToString(ipArray[i], 2).PadLeft(8, '0');
        //    }
        //    return Convert.ToUInt32(ipBinString, 2);
        //}
        */
    }
}
