using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.IO;

namespace SiteScraper
{
    class Program
    {

        const string siteUrl = "https://mydoctorfinder.com";
        // Modify this based on your file system.
        const string filePath = @"C:\Users\user\Documents\Doctors.xml";



        static void Main(string[] args)
        {
            //Dictionary<string, int> lettersAndPageNumbers = new Dictionary<string, int>();

            Program p = new Program();
            List<Doctor> doctors = new List<Doctor>();
            System.Xml.Serialization.XmlSerializer xmlSerializer = null;

            FileStream ostrm;
            StreamWriter writer;
            TextWriter oldOut = Console.Out;

            try
            {
                
                ostrm = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(ostrm);
                for (int i = 1; i <= 760; i++)
                {
                    foreach(Doctor doctor in p.saveDoctorsAndHospitals(i))
                    {
                        doctors.Add(doctor);
                    }
                }
                xmlSerializer  = new System.Xml.Serialization.XmlSerializer(doctors.GetType());

            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open Redirect.txt for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);
            xmlSerializer.Serialize(Console.Out, doctors);
            Console.WriteLine();
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();

        }

        public List<Doctor> saveDoctorsAndHospitals(int pageNumber)
        {
            List<Doctor> doctors = new List<Doctor>();

            HtmlWeb web = new HtmlWeb();
            HtmlDocument document = web.Load($"{siteUrl}/doctors?page={pageNumber}");

            // Get the last pagination number.
            HtmlAgilityPack.HtmlNodeCollection resultItems = document.DocumentNode.SelectNodes("//div[@class=\"result-item-content\"]");
            if (resultItems != null)
            {
                foreach(HtmlAgilityPack.HtmlNode resultItem in resultItems)
                {
                    Doctor dr = new Doctor();

                    string fullName = resultItem.ChildNodes[1].InnerText;
                    dr.Fullname = Regex.Replace(fullName, @"\t|\n|\r", "");

                    HtmlNodeCollection resultItemContents = resultItem.ChildNodes;
                    HtmlNode tableNode = resultItem.ChildNodes[3];

                    // Specialty
                    HtmlNode specialtyNode = tableNode.ChildNodes[1].ChildNodes[3];
                    dr.Specialties = getSpecialties(specialtyNode);

                    // Hospitals
                    List<Hospital> hospitals = getHospitals(tableNode.ChildNodes[3].ChildNodes);
                    dr.Hospitals = new List<Hospital>(hospitals);

                    // HMO
                    dr.HMOs = getHMO(tableNode.ChildNodes[5].ChildNodes[3]);
                    
                    doctors.Add(dr);
                }
            }

            return doctors;
        }

        public List<Hospital> getHospitals(HtmlNodeCollection hospitalNodeCollection)
        {
            List<Hospital> hospitals = new List<Hospital>();
            if(hospitalNodeCollection != null)
            {
                if(hospitalNodeCollection[3].HasChildNodes)
                {
                    HtmlNodeCollection aHospitals = hospitalNodeCollection[3].SelectNodes("a");
                    if(aHospitals != null)
                    {
                        foreach(HtmlNode aHospital in aHospitals)
                        {
                            string hospitalName = aHospital.InnerText;
                            string hospitalUrl = aHospital.Attributes[0].Value;
                            hospitals.Add(new Hospital
                            {
                                Name = hospitalName,
                                Url = hospitalUrl
                            });
                        }
                    }
                }
            }
            return hospitals;
        }

        public List<String> getHMO(HtmlNode hmoNode)
        {
            List<String> hmos = new List<string>();
            if(hmoNode != null)
            {
                string[] arr = Regex.Replace(hmoNode.InnerText, @"\t|\n|\r", "").Split(",".ToCharArray());
                hmos.AddRange(arr);
            }
            return hmos;
        }

        public List<Specialty> getSpecialties(HtmlNode specialtyNode)
        {
            List<Specialty> specialties = new List<Specialty>();

            string[] arr = Regex.Replace(specialtyNode.InnerText, @"\t|\r", "").Split("\n".ToCharArray());
            arr = arr.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            foreach (string text in arr)
            {
                specialties.Add(new Specialty { Description = text });
            }

            return specialties;
        }
    }

    public class Doctor
    {
        public string Fullname { get; set; }
        public List<string> HMOs { get; set; }
        public List<Specialty> Specialties { get; set; }
        public List<Hospital> Hospitals { get; set; }
    }

    public class Specialty
    {
        public string Description { get; set; }
    }

    public class Hospital
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Province { get; set; }
        public string ContactNo { get; set; }
        public List<string> HMOs { get; set; }
        public string Url { get; set; }
    }
}
