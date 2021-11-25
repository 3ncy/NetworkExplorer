using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace NetworkExplorer
{
    internal class Explorer
    {
        private object _zamek = new object();
        List<Device> hosts = new List<Device>();

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



            //TODO: predelat parametr baseIP aby se to ziskalo z ip a masky
            //for now to jenom stripnu
            string baseIP = String.Join('.', ipToScan.Take(3));
            Console.WriteLine("base ip: " + baseIP);
            //TODO: udelat poradne pocitani range na scan z masky
            //if (mask == 24)
            //{
            //    for (int i = 1; i < 255; i++)
            //    {
            //        string ip = baseIP + i.ToString();

            //        Ping ping = new Ping();
            //        var task = PingAsync(ping, ip);
            //        pings.Add(task);
            //    }
            //}

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



            Console.WriteLine("my ip: " + Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .First(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .ToString());
            Console.WriteLine("hostname: " + Dns.GetHostName());
            Console.WriteLine("pocet hostnamu: " + Dns.GetHostEntry(Dns.GetHostName()).AddressList.Length);

            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(ip.ToString(), new(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")))
                {
                    byte[] byteIP = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        byteIP[i] = byte.Parse(ip.ToString().Split('.')[i]);
                    }

                    if (IPtoUInt32(byteIP) > IPtoUInt32(networkIP) && IPtoUInt32(byteIP) < IPtoUInt32(networkIP) + numberOfHosts)
                    {
                        Console.WriteLine("Nase IP " + ip.ToString() + " je na skenovanem networku");
                    }
                }
            }

            foreach(Device device in hosts)
            {//TODO: VERY GOOD THING, WORKS  ZOBRAZOVAT TENHLE VYSLEDEK NEKDE JINDE
                Console.WriteLine(string.Join('.', device.IP) + " - " + Dns.GetHostEntry(new IPAddress(device.IP)).HostName);
            }
        }

        internal void PingAdress(byte[] ip)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(String.Join('.', ip), 1000);    //1000 je timout v [ms]

            //Console.WriteLine($"Status: {reply.Status}\nRoundtrip time: {reply.RoundtripTime}\nAdress: {reply.Address}\nOptions: {reply.Options}");
            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("Scanned IP " + reply.Address + " and host is UP");
            }
            else
            {
                Console.WriteLine("Host on " + reply.Address + " is either down or ICMP packets are filtered.");
            }
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
            //TODO: udelat GetNetAddress metodu

            //do binMask si dam maksu site v binarni podobe
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
