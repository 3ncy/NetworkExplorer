using System;
using System.Collections.Generic;
using System.Linq;
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

        internal async void PingSweepRange(string ipToScan, int mask)
        {
            //TODO: predelat parametr baseIP aby se to ziskalo z ip a masky
            //for now to jenom stripnu
            string baseIP = ipToScan.Substring(0, ipToScan.LastIndexOf('.') + 1);
            Console.WriteLine(baseIP);

            List<Task> pings = new List<Task>();


            /*
             * 192.168.5.140
             * 192.168.6.4
             * 
             * 
             */






            //TODO: udelat poradne pocitani range na scan z masky
            if (mask == 24)
            {
                for (int i = 0; i < 255; i++)
                {
                    string ip = baseIP + i.ToString();

                    Ping ping = new Ping();
                    var task = PingAsync(ping, ip);
                    pings.Add(task);
                }
            }

            Console.WriteLine("pingu: " + pings.Count);
            Console.WriteLine("hostu:" + hosts.Count);

            //pocka na 
            //await Task.WhenAll(pings).ContinueWith(p =>
            //{
            //    //do stuff az se dodelaji pingy
            //    //(vypsat result)
            //    Console.WriteLine("Nalezeno ip: " + hosts.Count);
            //});

            await Task.WhenAll(pings);//tohle to nidky nereachne?
            Console.WriteLine(hosts.Count + "hostu");
        }

        internal void PingAdress(string ip)
        {
            Ping ping = new Ping();
            PingReply reply = ping.Send(ip, 1000);

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

        private async Task PingAsync(Ping ping, string ip)
        {
            var reply = await ping.SendPingAsync(ip, 1000);

            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine("yes");
                lock (_zamek)
                {
                    hosts.Add(new Device(ip));
                }
            }
        }

        private string GetNetAddress(string address, byte mask)
        {
            //TODO: udelat GetNetAddress metodu
            byte[] oktety = Array.ConvertAll(address.Split('.'), byte.Parse);
            string binMask = "";
            for (byte i = 0; i < mask; i++) { binMask += "1"; }
            binMask = binMask.PadRight(32, '0');

            byte[] maskOktety = new byte[4];
            byte[] netOktety = new byte[4];

            for (int i = 0; i < 4; i++) { maskOktety[i] = Convert.ToByte(binMask.Substring(i * 8, 8), fromBase: 2); }

            for (int i = 0; i < 4; i++) { netOktety[i] = (byte)(oktety[i] & maskOktety[i]); }

            return string.Join('.', netOktety);
        }

        private int GetNumberOfHosts(string ip, byte mask)
        {
            string netAdd = GetNetAddress(ip, mask);

            string binMask = Convert.ToString(mask, 2).PadLeft(32, '0');


            
        }



        //ipNumber will look for example like this: 3232235830, but it means 11000000101010000000000100110110 in binary, which is easy transalte to 11000000.10101000.00000001.00110110 => 192.168.1.54
        private string UInt32toIP(UInt32 ipNumber)
        {
            string ipBinString = Convert.ToString(ipNumber, 2).PadLeft(32, '0');//convert the weird number to binary and pad it to have 32 characters.
            byte[] ipArray = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ipArray[i] = Convert.ToByte(ipBinString.Substring(i * 8, 8), 2);
            }
            return String.Join(".", ipArray);
        }

        //converts 192.168.1.54 to 11000000.10101000.00000001.00110110 to UInt32 in binary (11000000101010000000000100110110) which is 3232235830 and is easier to iterate upon
        private UInt32 IPtoUint32(string ip)
        {
            byte[] ipArray = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ipArray[i] = byte.Parse(ip.Split('.')[i]);
            }
            string ipBinString = "";
            for (int i = 0; i < 4; i++)
            {
                ipBinString += Convert.ToString(ipArray[i], 2).PadLeft(8, '0');
            }
            return Convert.ToUInt32(ipBinString, 2);
        }
    }
}
