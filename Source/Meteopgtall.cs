﻿#region Copyright
///<remarks>
/// <GRAMM Mesoscale Model>
/// Copyright (C) [2019]  [Dietmar Oettl, Markus Kuntner]
/// This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by
/// the Free Software Foundation version 3 of the License
/// This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.
/// You should have received a copy of the GNU General Public License along with this program.  If not, see <https://www.gnu.org/licenses/>.
///</remarks>
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace GRAMM_CSharp_Test
{
    class Meteopgtall
    {
        //computes the correct filenumber for intermediate GRAMM flow field files
        public static void meteopgtall_calculate(int Ori_Weather_nr, int IWETTER, double DTI, double TIME, int OUTPUT, double TLIMIT2, ref int Filenumber)
        {
            //compute index of intermediate file
            Int16 IHOUR = Convert.ToInt16(Math.Floor(TIME / OUTPUT));

            //compute filenumber
            Filenumber = Ori_Weather_nr;
            List<Int16> counter = new List<Int16>();
            //reading meteopgt.all

            try
            {
                using (FileStream fs = new FileStream("meteopgt.all", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader myreader = new StreamReader(fs))
                    {
                        string[] text = new string[10];
                        text = myreader.ReadLine().Split(new char[] { ' ', ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        text = myreader.ReadLine().Split(new char[] { ' ', ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int inid = 1; inid < Program.IWETTER; inid++)
                        {
                            text = myreader.ReadLine().Split(new char[] { ' ', ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            double WINDGE = Math.Max(Convert.ToDouble(text[1].Replace(".", Program.decsep)), 0.001);
                            int AKLA = Convert.ToInt16(text[2]);
                            double ITIME = TLIMIT2;
                            Integrationtime(AKLA, WINDGE, TLIMIT2, Ori_Weather_nr, ref ITIME);

                            //number of additional weather situations
                            int addnumb = Convert.ToInt16(Math.Ceiling(ITIME / OUTPUT - 1));
                            for (int i = 1; i <= addnumb; i++)
                            {
                                counter.Add(1);
                            }
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error when reading file meteopgt.all during intermediate output of GRAMM flow fields - Execution stopped");
                Environment.Exit(0);
            }

            Filenumber += counter.Count + IHOUR;
        }

        //computes a new meteopgt.all with additional weather situations at the end of the original file, where intermediate GRAMM flow fields are stored
        public static void meteopgtall_generate(int Ori_Weather_nr, double TLIMIT2, int OUTPUT)
        {
            List<string> texttoadd = new List<string>();
            bool extended_meteofile = false;
            //reading meteopgt.all

            try
            {
                using (FileStream fs = new FileStream("meteopgt.all", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader myreader = new StreamReader(fs))
                    {

                        string[] text = new string[10];
                        text = myreader.ReadLine().Split(new char[] { ' ', ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        text = myreader.ReadLine().Split(new char[] { ' ', ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        for (int inid = 1; inid <= Ori_Weather_nr; inid++)
                        {
                            text = myreader.ReadLine().Split(new char[] { ' ', ';', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            double WINDGE = Math.Max(Convert.ToDouble(text[1].Replace(".", Program.decsep)), 0.001);
                            double WINDDIR = Convert.ToDouble(text[0].Replace(".", Program.decsep)) * 10;
                            int AKLA = Convert.ToInt16(text[2]);
                            double ITIME = TLIMIT2;
                            Integrationtime(AKLA, WINDGE, TLIMIT2, Ori_Weather_nr, ref ITIME);
                            //number of additional weather situations
                            int addnumb = Convert.ToInt16(Math.Ceiling(ITIME / OUTPUT - 1));
                            //Console.WriteLine(inid.ToString()+"/" + addnumb.ToString() +"/" + ITIME.ToString() + "/" + OUTPUT.ToString());
                            text[1] = text[1].Replace(".", string.Empty);
                            for (int i = 1; i <= addnumb; i++)
                            {
                                texttoadd.Add(Convert.ToString(Convert.ToInt16(WINDDIR)) + "." + text[1] + "," + inid.ToString() + "." + i.ToString() + "," + text[2] + ",0");
                            }
                        }
                        if (myreader.EndOfStream)
                            extended_meteofile = true;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Error when reading file meteopgt.all during intermediate output of GRAMM flow fields - Execution stopped");
                Environment.Exit(0);
            }
        				
            //writing extended meteopgt.all, only if it has not already been done before
            if (extended_meteofile == true)
            {
                try
                {
                    using (StreamWriter mywriter = File.AppendText("meteopgt.all"))
                    {
                        for (int inid = 0; inid < texttoadd.Count; inid++)
                        {
                            mywriter.WriteLine(texttoadd[inid]);
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Error when writing extended file meteopgt.all for intermediate output of GRAMM flow fields - Execution stopped");
                    Environment.Exit(0);
                }
            }
        }

        //computes the GRAMM integration time in dependence on wind speed and stability class
        public static void Integrationtime(int stability, double windspeed, double TLIMIT2, int dynamic, ref double DTI)
        {
            //New time scheme OETTL, 10 Feb 2017 to increase wind field variability
            DTI = TLIMIT2;

            if (stability < 4)
            {
                if (windspeed < 3)
                {
                    DTI = TLIMIT2 * 1.0;
                }
                else if (windspeed < 5)
                {
                    DTI = TLIMIT2 * 0.5;
                }
                else if (windspeed < 7)
                {
                    DTI = TLIMIT2 * 0.3;
                }
                else 
                {
                    DTI = TLIMIT2 * 0.17;
                }

                //in case of dynamic sun rise simulation
                if (dynamic > 0)
                {
                    DTI = 21600;  //should be exactly 6 hours as the simulation starts at 6 o'clock in the morning
                }
            }
            if (stability == 6)
            {
                if (windspeed <= 2.0)
                {
                    //in case of dynamic sun rise simulation
                    if (dynamic > 0)
                    {
                        DTI = TLIMIT2 * 3.0;
                    }
                    else
                    {
                        DTI = Math.Min(TLIMIT2 * 1.5, 3600);
                    }
                }
                /*
                else if (windspeed < 1.0)
                {
                    DTI = TLIMIT2 * 1.5;
                }
                */
                else 
                {
                    DTI = TLIMIT2 * 1.0;
                }
            }
            if (stability == 7)
            {
                if (windspeed < 0.5F)
                {
                    //in case of dynamic sun rise simulation
                    if (dynamic > 0)
                    {
                        DTI = Math.Min(TLIMIT2 * 2.0, 7200);
                    }
                    else
                    {
                        DTI = Math.Min(TLIMIT2 * 2.0, 3600);
                    }
                }
                else if (windspeed < 1.0)
                {
                    //in case of dynamic sun rise simulation
                    if (dynamic > 0)
                    {
                        DTI = Math.Min(TLIMIT2 * 1.5, 7200);
                    }
                    else
                    {
                        DTI = Math.Min(TLIMIT2 * 1.5, 3600);
                    }
                }
                else 
                {
                    DTI = Math.Min(TLIMIT2 * 1.0, 3600);
                }
            }

            if (stability == 4)
            {
                DTI = Math.Min(TLIMIT2 * 0.17, 600);
            }
        }
    }
}