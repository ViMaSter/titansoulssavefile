using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;

namespace TitanSouls
{
    public class Savefile
    {
        #region Raw data
        /// <summary>
        /// Absolute path to the savefile (supplied in the ctor)
        /// </summary>
        public string AbsolutePath;

        /// <summary>
        /// Full dump of the XML-string
        /// </summary>
        public string XMLContent;

        /// <summary>
        /// MD5-checksum - currently of unreproducable origin
        /// </summary>
        public string Checksum;
        #endregion

        #region Additional data
        /// <summary>
        /// Whether or not the file could successfully be parsed/is a valid Titan Souls savefile
        /// 
        /// This member is set when validating the checksum obtained in ::SeperateSavefileContent. If the checksum is not a valid MD5-hash, this variable is false
        /// </summary>
        public bool IsValid;
        #endregion


        #region Parsed info
        /// <summary>
        /// Raw value of time played in Titan Souls' own time format
        /// 
        /// Titan Souls stores the time played by taking the milliseconds spent ingame and int.Parse(x*60) it
        /// Therefore: Dividing this value by 60, results in the seconds spent playing and milliseconds having 1/60-precision
        /// </summary>
        public int TimePlayed = -1;

        private TimeSpan timePlayedInTimeSpan = TimeSpan.Zero;

        /// <summary>
        /// TimeSpan object displaying the time spent ingame 
        /// </summary>
        public TimeSpan TimePlayedInTimeSpan
        {
            get
            {
                if (timePlayedInTimeSpan == TimeSpan.Zero)
                {
                    timePlayedInTimeSpan = new TimeSpan(0, 0, (int)(TimePlayed / 60f));
                }
                return timePlayedInTimeSpan;
            }
        }

        /// <summary>
        /// Sum of all player deaths
        /// </summary>
        public int Deaths = -1;

        /// <summary>
        /// List of names of all bosses slain
        /// </summary>
        public List<string> BossesSlain;

        /// <summary>
        /// Keys that denote progress in the game, since parts of the game can be played non-linear
        /// </summary>
        public List<string> KeysUnlocked;
        #endregion

        #region Static
        /// <summary>
        /// Helper function used to seperate XML content and MD5 checksum
        /// </summary>
        /// <param name="content">String containing the whole savefile</param>
        /// <param name="checksum">out parameter: Variable in which to store the MD5 checksum - will be string.Empty if content is invalid</param>
        /// <param name="XMLcontent">out parameter: XML content - will be string.Empty if content is invalid</param>
        /// <returns>Whether or not content contained a MD5 checksum</returns>
        public static bool SeperateSavefileContent(string content, out string checksum, out string XMLcontent)
        {
            // Seperate the checksum from the XML-part
            List<string> rows = new List<string>(content.Split('\n'));
            if (rows[rows.Count - 1].Trim().Length != 32)
            {
                checksum = string.Empty;
                XMLcontent = string.Empty;
                return false;
            }

            // Store checksum
            checksum = rows[rows.Count - 1].Trim();

            // And save the leftover XML-part
            rows.RemoveAt(rows.Count - 1);
            XMLcontent = string.Concat(rows);

            return true;
        }
        #endregion

        /// <summary>
        /// Constructor. Will try to set all members by reading out the file at the path specified in the first parameter
        /// </summary>
        /// <param name="absolutePathToSavefile">Path to the savefile to load</param>
        public Savefile(string absolutePathToSavefile)
        {
            // Load the whole file into a string
            string RawContent = File.ReadAllText(absolutePathToSavefile);
            
            IsValid = SeperateSavefileContent(RawContent, out Checksum, out XMLContent);
            if (!IsValid)
                return;


            AbsolutePath = absolutePathToSavefile;

            using (XmlReader Reader = XmlReader.Create(new StringReader(XMLContent)))
            {
                // Parse the file and display each of the nodes.
                while (Reader.Read())
                {
                    switch (Reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (Reader.Name)
                            {
                                case "Kills":
                                    BossesSlain = new List<string>(Int32.Parse(Reader.GetAttribute("count")));
                                    break;
                                case "Key":
                                    if (KeysUnlocked == null)
                                    {
                                        KeysUnlocked = new List<string>();
                                    }
                                    KeysUnlocked.Add(Reader.GetAttribute("id"));
                                    break;
                                case "Titan":
                                    BossesSlain.Add(Reader.GetAttribute("id"));
                                    break;
                                case "time":
                                    TimePlayed = System.Int32.Parse(Reader.GetAttribute("val"));
                                    break;
                                case "Deaths":
                                    Deaths = System.Int32.Parse(Reader.GetAttribute("count"));
                                    break;
                            }
                            break;
                    }
                }
            }
        }
    }
}
