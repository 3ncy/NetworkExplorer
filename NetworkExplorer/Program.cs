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
            /*
             * Dve klicove casti, co program bude umet:
             * 1) zjistit aktivni hosty na networku
             * 2) zjistit co za oteverne porty na nich je
             * 
             * co je potreba udelat pro jednotlive casti
             * 1)
             *  (zjistit si masku site) 
             *      - byla by to cool featurka, ale uzivatel ji muze zadat, tohle budu implementovat az zbyde cas
             *  pingnout vsechny hosty a zjistit kdo je online
             *      separate class "Pinger"
             *          ta pingne vsechny hosty asynchronne 
             *              (asi na jinem vlakne???? aby na hlavnim slo se ptat na status searche jako to ma nmap)
             *              
             *      
             *  
             * 
             * 
             */


            //handele a rozdeleni commandu 
            //format prikazu bude NetworkExplorer (mby aliasnout nebo vydavat exe jako "nexp")
            //nexp 192.168.1.1/24           //pingne co je na siti za online pc
            //nexp 192.168.1.1              //
            //nexp 192.168.1.1:8080         //oskenuje co bezi na tom danem portu za dvojteckou
            //nexp 192.168.1.1 -ports       //oskenuje co bezi na specifikovanem hostovi
            //nexp 192.168.1.1/24 -ports    //oskenuje vsechny hosty na siti a vrati co na nich bezi (nejdriv vrati jenom senzma hostu)


            string vstup = args[0];
            //string vstup = "192.168.30.2/24";
            byte maska = 32;
            Regex ipRegex = new(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            byte[] ipToScan = new byte[4];

            //TODO: oznamit znacku pc (sitovky) podle MAC adresy

            //TODO: mby extractnout tohle do samostatne metody Check if ip is valid

            if (ipRegex.IsMatch(vstup))
            {
                if (vstup.Contains('/')) //v ip je i maska
                {
                    if (!byte.TryParse(vstup.Split('/')[1], out maska))//parse co je za lomitkem (masku)
                    {
                        Console.WriteLine("Zadejte ip a masku ve tvaru 192.168.1.1/24 bez mezer");
                        return;
                    }
                    if (1 >= maska && maska <= 30)//jestli maska neni v normalnim rangi pro subnety
                    {
                        Console.WriteLine("Maska musi byt mezi 1 a 30");
                        return;
                    }

                    //convert ip to byte array
                    try
                    {
                        ipToScan = Array.ConvertAll(vstup.Split('/')[0].Split('.'), x => byte.Parse(x));
                    }
                    catch (OverflowException e)
                    {
                        Console.WriteLine("Zadejte validni ip (vsechny oktety musi byt mezi hodnotami 0 a 255");
                    }
                }
                else
                {
                    //validni ip bez masky
                    try
                    {
                        ipToScan = Array.ConvertAll(vstup.Split('/')[0].Split('.'), x => byte.Parse(x));
                    }
                    catch (OverflowException e)
                    {
                        Console.WriteLine("Zadejte validni ip (vsechny oktety musi byt mezi hodnotami 0 a 255");
                    }
                }
            }
            else
            {
                Console.WriteLine("Zadejte ip adresu a pripadne masku ve tvaru 192.168.1.1/24 bez mezer");
                return;
            }

            //todo: jen debug
            //Console.WriteLine("ip zadana uzivatelem " + String.Join('.', ipToScan));
            //Console.WriteLine("maska zadana uzivatelem " + maska);

            //handle nejakych commandu
            //pokud dostanu jenom single ip bez argumewntu a masky, tak na ni oskenuju porty.
            //pokud dostanu jenom ip s maskou, tak opinguju hosty a vratim je ktere jsou online
            //pokud dostanu jenom ip a port, oskenuju ten port abych zjistil co na nem (jestli neco) bezi


            Explorer explorer = new Explorer();


            //TODO: predelat tohle aby se vovlala jedna metoda s argumentam nebo tak nejak idk
            if (maska == 32)
            {
                explorer.PingAdress(ipToScan);

                Task<String> macTask = explorer.GetMACAndManufacturerAsync(ipToScan);
                macTask.Wait();
                string mac = macTask.Result;
                Console.WriteLine("\t" + mac);
            }
            else
            {
                explorer.PingSweepRange(ipToScan, maska).Wait();
            }


        }
    }
}