using System;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using CsvHelper.Configuration;
using MaxMind.GeoIP2;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Linq;

namespace LandrAudition
{

        public class Program
        {
        public static async Task Main()
        {

            //Variables
            string fileNameIn = "ipIn.json";
            string fileNameOut = "ipOut.json";
            string path = @"C:\JsonFiles\";
            string pathIn = Path.Combine(path, fileNameIn);
            string pathOut = Path.Combine(path, fileNameOut);

            var ipAdresses = new List<string>();

            string[] csvLines = File.ReadAllLines(pathIn);
            List<string> jsonFichier = new List<string>();


            //Pas de header sur le CSV
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false
            };

            using (var client = new WebServiceClient(722785, "MmLJo9g8EGTtze9a", host: "geolite.info"))
            {

                //Recupération ip Utilisateur
                string ipUser = GetLocalIPAddress();

                Console.WriteLine(ipUser);

                //Geolocalisation Utilisateur
                var response = client.Country();
                Console.WriteLine(response.Country.Name);


                //Ecrit toutes les IP du fichier dans un tableau
                for (int i = 0; i < csvLines.Length; i++)
                {
                    string[] rowData = csvLines[i].Split(",");
                    ipAdresses.Add(rowData[0]);
                }

                //Initialisation Fichier
                var fichierIP = new FichierIP { };

                using FileStream createStream = File.Create(pathOut);

                await JsonSerializer.SerializeAsync(createStream, fichierIP);
                await createStream.DisposeAsync();

                foreach(string ip in ipAdresses)
                {
                    var ipNoQuotes = from Match match in Regex.Matches(ip, "\"([^\"]*)\"")
                                 select match.ToString();

                    foreach(string ipQuotes in ipNoQuotes)
                    {
                       string ipFinal = ipQuotes.Substring(1, ipQuotes.Length - 2);

                        try
                        {
                            string country = client.Country(ipFinal).ToString();


                            //Recupère le pays de la reponse API
                            int pFrom = country.IndexOf("Country=") + "Country=".Length;
                            int pTo = country.LastIndexOf(",");

                            string countryFiltered = country.Substring(pFrom, pTo - pFrom);
                            string CountryFinal = countryFiltered.Substring(0, countryFiltered.IndexOf(','));

                            //Ecriture dans fichier Json
                            List<FichierIP> _data = new List<FichierIP>();
                            _data.Add(new FichierIP()
                            {
                                IpAdress = ipFinal,
                                Localisation = CountryFinal

                            });

                            foreach(FichierIP ipJsonOut in _data) 
                            {
                                string json = JsonSerializer.Serialize(_data);
                                File.AppendAllText(@pathOut, json);
                            }
                        }

                        catch(MaxMind.GeoIP2.Exceptions.AddressNotFoundException)
                        {
                            Console.WriteLine("L'adresse IP " + ipFinal + " est invalide ou protégée, impossible de géolocaliser");
                        }
                    }

                }

            }

        }

        //Recupère IP Utilisateur
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ipUSer in host.AddressList)
            {
                if (ipUSer.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ipUSer.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }
}




