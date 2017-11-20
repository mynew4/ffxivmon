﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace FFXIVMonReborn
{
    class Struct
    {
        public static StructListItem[] Parse(string data, byte[] packet)
        {
            // Get rid of any comments
            Regex r = new Regex("\\/\\*(.*)\\*\\/");
            data = r.Replace(data,"");
            r = new Regex("\\/\\/(.*)");
            data = r.Replace(data, "");

            Debug.WriteLine(data);

            List<StructListItem> output = new List<StructListItem>();

            var lines = Regex.Split(data, "\r\n|\r|\n");
            int at = 3;

            using (MemoryStream stream = new MemoryStream(packet))
            {
                stream.Position = 0x20;
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    while (at != lines.Length - 1)
                    {
                        StructListItem item = new StructListItem();

                        var line = lines[at];

                        int pos = 0;
                        while (!Char.IsLetter(line[pos]))
                        {
                            ++pos;
                        }

                        string dataType = "";

                        while (line[pos] != ' ')
                        {
                            dataType += line[pos];
                            pos++;
                        }

                        Debug.WriteLine(dataType);
                        item.DataTypeCol = dataType;

                        pos++;

                        string name = "";

                        while (line[pos] != ';')
                        {
                            name += line[pos];
                            pos++;
                        }

                        Debug.WriteLine(name);
                        item.NameCol = name;

                        item.offset = stream.Position;
                        item.OffsetCol = stream.Position.ToString("X");

                        StructListItem[] aryItems = null;
                        
                        if (!name.EndsWith("]"))
                            ParseCType(dataType, reader, ref item);
                        else
                            aryItems = ParseCArray(dataType, reader, ref item, name);

                        output.Add(item);
                        
                        if(aryItems != null)
                            output.AddRange(aryItems);

                        Debug.WriteLine($"{item.NameCol} - {item.OffsetCol} - {item.DataTypeCol} - {item.ValueCol}");

                        at++;
                    }
                }
            }

            return output.ToArray();
        }

        private static StructListItem[] ParseCArray(string dataType, BinaryReader reader, ref StructListItem item, string name)
        {
            List<StructListItem> output = new List<StructListItem>();
            
            int count = int.Parse(SubstringBetweenIndexes(name, name.IndexOf("[") + 1, name.LastIndexOf("]")));

            for (int i = 0; i < count; i++)
            {
                StructListItem aryItem = new StructListItem();
                aryItem.NameCol = "  " + SubstringBetweenIndexes(name, 0, name.IndexOf("[")) + $"[{i}]";
                aryItem.offset = reader.BaseStream.Position;
                aryItem.OffsetCol = reader.BaseStream.Position.ToString("X");
                
                ParseCType(dataType, reader, ref aryItem);
                
                output.Add(aryItem);
            }

            return output.ToArray();
        }
        
        public static string SubstringBetweenIndexes(string value, int startIndex, int endIndex)
        {
            return value.Substring(startIndex, endIndex - startIndex);
        }
        
        /// <summary>
        /// Parse value as string and it's lenght to a StructListItem
        /// </summary>
        private static void ParseCType(string dataType, BinaryReader reader, ref StructListItem item)
        {
            switch (dataType)
            {
                case "uint8_t":
                    item.ValueCol = reader.ReadByte().ToString();
                    item.typeLength = 1;
                    break;
                case "uint16_t":
                    item.ValueCol = reader.ReadUInt16().ToString();
                    item.typeLength = 2;
                    break;
                case "uint32_t":
                    item.ValueCol = reader.ReadUInt32().ToString();
                    item.typeLength = 4;
                    break;
                case "uint64_t":
                    item.ValueCol = reader.ReadUInt64().ToString();
                    item.typeLength = 8;
                    break;
            }
        }
    }

    public class StructListItem
    {
        public string DataTypeCol { get; set; }
        public string NameCol { get; set; }
        public string ValueCol { get; set; }
        public string OffsetCol { get; set; }

        public long offset;
        public byte[] dataChunk;
        public int typeLength;
    }
}