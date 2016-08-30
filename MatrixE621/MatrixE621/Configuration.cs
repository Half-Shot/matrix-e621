using System;
using System.IO;
using IniParser;
using IniParser.Model;
namespace MpdDj
{
	public class Configuration
	{
		public static IniData Config { get; private set; }
		public static IniData DefaultConfiguration(){
			IniData defaultData = new IniData ();
			SectionData Matrix = new SectionData ("matrix");
			SectionData RoomRating = new SectionData ("room_rating");

			defaultData.Sections.Add (Matrix); 
			defaultData.Sections.Add (RoomRating); 

			Matrix.Keys.AddKey("host","https://localhost:8448");
			Matrix.Keys.AddKey("user","username");
			Matrix.Keys.AddKey("pass","password");
			Matrix.Keys.AddKey("rooms","#RoomA,#RoomB:localhost,#RoomC");
			return defaultData;
		}

		public static void ReadConfig(string cfgpath){
			if (File.Exists (cfgpath)) {
				FileIniDataParser parser = new FileIniDataParser ();
				Config = parser.ReadFile (cfgpath);
			} else {
				Console.WriteLine ("[Warn] The config file could not be found. Using defaults");
				Config = DefaultConfiguration ();
			}
		}
	}
}

