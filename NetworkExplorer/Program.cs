using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace NetworkExplorer
{
    public class Program
    {
        public static void Main(string[] args)
        {

            //handele a rozdeleni commandu 
            //format prikazu bude NetworkExplorer (mby aliasnout nebo vydavat exe jako "nexp")
            //nexp 192.168.0.1/24
            //nexp 192.168.0.1
            //nexp 192.168.0.1 -p
            //nexp 192.168.0.1/24 -p
            //nexp 192.168.0.1 -p 8080
            //nexp 192.168.0.1/24 -p 8080
            //nexp 192.168.0.1 -p 22-57
            //nexp 192.168.0.1/24 -p 22-57
            //    args[0]↑  args[1]↑   ↑args[2]



            Regex ipRegex = new(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            byte[] ipToScan = new byte[4];
            byte maska = 32;//default 32, to pouzivam kdyz mam na mysli jen jedno zarizeni
            bool scanPorts = false;
            int startPort = default(int);
            int endPort = default(int);

            //Console.WriteLine("args: ");
            //foreach (string arg in args)
            //{
            //    Console.WriteLine("\"" + arg + "\"");
            //    Console.WriteLine(arg.Trim() == "-p");
            //}

            //handle/parse input:
            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }
            if (args.Contains("help"))  //TODO: tady si dat pozor kdyz uzivatel bude zadavat webovou stranku a ne ip
            {
                ShowHelp();
                return;
            }


            //nejdrive zjistit jeste je vice parametru

            if (args.Length > 1)
            {
                if (args[1].Trim() == "-p") //jestli chci skenovat port(y)
                {
                    scanPorts = true;
                    if (args.Length > 2)    //vetsi nez 2 arg, tzn 3 argumenty, z toho prvni je ip adresa a druhy je "-p" 
                    {
                        if (args[2].Contains('-')) //chci skenovat range portu
                        {
                            if (!int.TryParse(args[2].Split('-')[0], out startPort) || !int.TryParse(args[2].Split('-')[1], out endPort))
                            {
                                Console.WriteLine("Nevalidni cislo portu.");
                                return;
                            }
                            if (startPort > 65535 || startPort < 1 || endPort > 65535 || endPort < 1)
                            {
                                Console.WriteLine("Cislo portu musi byt mezi 1 a 65535");
                                return;
                            }
                            if (startPort > endPort)
                            {
                                Console.WriteLine("Port na zacatku skenovaneho rozsahu musi byt mensi nez port na jeho konci!");
                                return;
                            }
                        }
                        else //chci skenovat jen jeden port
                        {
                            if (!int.TryParse(args[2], out startPort))
                            {
                                Console.WriteLine("Nevalidni cislo portu");
                                return;
                            }
                            if (startPort > 65535 || startPort < 1)
                            {
                                Console.WriteLine("Cislo portu musi byt mezi 1 a 65535");
                                return;
                            }
                            endPort = startPort;    //protoze chci skenovat jen jeden port
                        }
                    }
                    else
                    {
                        //uzivatel chce skenovat porty, ale nezadal jake, tzn oskenuju well known ports
                    }
                }
            }

            //parsovani toho jestli chci skenovat porty a jake je vyreseno, ted naparsovani ip

            if (ipRegex.IsMatch(args[0]))   //vstup obsahuje validni ip
            {
                if (args[0].Contains('/'))  //chci skenovat subnet (vstup obsahuje masku)
                {
                    if (!byte.TryParse(args[0].Split('/')[1], out maska))   //parse co je za lomitkem (masku)
                    {
                        Console.WriteLine("Zadejte ip a masku ve tvaru 192.168.1.1/24 bez mezer");
                        return;
                    }
                    if (1 > maska || maska > 30)  //jestli maska neni v normalnim rangi pro subnety
                    {   //todo: mozna povolit i masku /0, ale ta by obsahovala celej IPv4 internet
                        Console.WriteLine("Maska musi byt mezi 1 a 30");
                        return;
                    }

                    try
                    {
                        ipToScan = Array.ConvertAll(args[0].Split('/')[0].Split('.'), x => byte.Parse(x));  //pokus o naparsovani zadane ip
                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("Zadejte validni ip (vsechny oktety musi byt mezi hodnotami 0 a 255)");
                    }
                }
                else //chci skenovat jenom jedno zarizeni
                {
                    try
                    {
                        ipToScan = Array.ConvertAll(args[0].Split('/')[0].Split('.'), x => byte.Parse(x));  //pokus o naparsovani zadane ip
                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("Zadejte validni ip (vsechny oktety musi byt mezi hodnotami 0 a 255)");
                    }
                }
            }
            else //nevalidni vstup, zobrazim help
            {
                ShowHelp();
                return;
            }

            void ShowHelp()
            {
                Console.WriteLine("TOTO JE BASIC HELP, neni hototva");
                //Console.WriteLine("Zadejte ip adresu a pripadne masku ve tvaru 192.168.1.1/24 bez mezer");

                //todo: dodelat help metodu
                //throw new NotImplementedException("Metoda 'Show Help' jeste neni hotova");
            }


            //HANDLE COMMANDS:

            //Console.WriteLine($"ip: {String.Join('.', ipToScan)}\nmask: {maska}\nscanPorts; {scanPorts}\nstartPort: {startPort}\nendPort: {endPort}");


            Explorer explorer = new();


            if (maska == 32)
            {
                explorer.PingAdress(ipToScan);

                Task<string> macTask = explorer.GetMACAndManufacturerAsync(ipToScan); //je to jako task, protoze nektere veci muzou trochu trvat pri executovan iteto metody
                macTask.Wait();
                string mac = macTask.Result;
                Console.WriteLine("\t" + mac);

                if (scanPorts)
                {
                    if (startPort != 0 && startPort == endPort)   //skenuju jen jeden port
                    {
                        string result = explorer.ScanPort(ipToScan, startPort);

                        if (result == String.Empty)
                        {
                            Console.WriteLine("\t" + startPort + ": port neodpovida (je pravdepodobne zavreny)");
                        }
                        else
                        {
                            Console.WriteLine("\t" + result);
                        }
                        //Console.WriteLine(explorer.ScanPort(ipToScan, startPort));
                    }
                    else if (startPort == 0 && endPort == 0)
                    {
                        Console.WriteLine("Zahajen scan castych tcp portu.... (toto muze chvili trvat)");
                        List<string> ports = explorer.ScanWellKnownPorts(ipToScan);

                        if (ports.Count == 0)
                        {
                            Console.WriteLine("Nebyly nalezeny zadne otevrene porty");
                            return;
                        }
                        Console.WriteLine("Nalezene otevrene porty:");
                        foreach (string text in ports)
                        {
                            Console.WriteLine(text);
                        }
                    }
                    else //skenuju range portu
                    {
                        Console.WriteLine($"Zahajen scan tcp portu {startPort} az {endPort}.... (toto muze chvili trvat)");

                        List<string> ports = explorer.ScanPortRange(ipToScan, startPort, endPort);

                        if (ports.Count == 0)
                        {
                            Console.WriteLine("Nebyly nalezeny zadne otevrene porty");
                            return;
                        }
                        Console.WriteLine("Nalezene otevrene porty:");
                        foreach (string text in ports)
                        {
                            Console.WriteLine(text);
                        }
                    }
                }
            }
            else //chci skenovat celej range
            {
                var pingSweepTask = explorer.PingSweepRange(ipToScan, maska);
                pingSweepTask.Wait();   //musim waitovat, aby mi program neskoncil a nezabil background vlakna na kterych bezi pingy

                //todo: jen debug
                Console.WriteLine(pingSweepTask.Status);
                Console.WriteLine(pingSweepTask.Result.Count);


                List<Device> hosts = pingSweepTask.Result;

                Console.WriteLine("hosts.count: " + hosts.Count);

                Console.Write("Scan dokoncen. ");

                if (hosts.Count == 0)
                {//pokud jsem nenasel zadne aktivni hosty, returnu
                    Console.WriteLine("Nebyli nalezeni zadni aktivni hoste na zadane siti!");
                    return;
                }

                Console.WriteLine("Celkem nalezeno " + hosts.Count + " bezicich zarizeni");
                Console.WriteLine("Nalezeni hoste: ");

                foreach (Device host in hosts)//TODO: vypisovani hostu uz za behu 
                {
                    Console.WriteLine(String.Join('.', host.IP) + " je online!");

                    Task<String> macTask = explorer.GetMACAndManufacturerAsync(host.IP);
                    macTask.Wait();
                    string mac = macTask.Result;
                    Console.WriteLine("\t" + mac);

                    if (scanPorts)
                    {

                        if (startPort != 0 && startPort == endPort)   //skenuju jen jeden port
                        {
                            Console.WriteLine("Porty:");

                            string result = explorer.ScanPort(host.IP, startPort);
                            if (result == String.Empty)
                            {
                                Console.WriteLine("\t" + startPort + ": port neodpovida (je pravdepodobne zavreny)");
                            }
                            else
                            {
                                Console.WriteLine("\t" + result);
                            }
                        }
                        else if (startPort == 0 && endPort == 0)    //scan well known ports
                        {
                            Console.WriteLine("Zahajen scan castych tcp portu.... (toto muze chvili trvat)");

                            List<string> ports = explorer.ScanWellKnownPorts(host.IP);

                            if (ports.Count == 0)
                            {
                                Console.WriteLine("Nebyly nalezeny zadne otevrene porty");
                            }
                            else
                            {
                                Console.WriteLine("Nalezene otevrene porty:");
                                foreach (string text in ports)
                                {
                                    Console.WriteLine(text);
                                }
                            }
                        }
                        else //scanuju range poru ktery je specifikovany pomoci startPort a endPort
                        {
                            Console.WriteLine($"Zahajen scan tcp portu {startPort} az {endPort}.... (toto muze chvili trvat)");

                            List<string> ports = explorer.ScanPortRange(host.IP, startPort, endPort);

                            if (ports.Count == 0)
                            {
                                Console.WriteLine("Nebyly nalezeny zadne otevrene porty");
                            }
                            else
                            {
                                Console.WriteLine("Nalezene otevrene porty:");
                                foreach (string text in ports)
                                {
                                    Console.WriteLine(text);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}