using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;
using MatrixSDK.Client;
using MatrixSDK.Structures;
using System.Reflection;
using System.Drawing;
using System.Drawing.Imaging;
namespace MpdDj
{
	[AttributeUsage(AttributeTargets.Method)]
	public class BotCmd : Attribute{
		public readonly string CMD;
		public readonly string[] BeginsWith;
		public BotCmd(string cmd,params string[] beginswith){
			CMD = cmd;
			BeginsWith = beginswith;
		}
	}

    [AttributeUsage(AttributeTargets.Method)]
    public class BotFallback : Attribute {
		
	}

    [AttributeUsage(AttributeTargets.Method)]
    public class BotHelp : Attribute {
        public readonly string HelpText;
        public BotHelp(string help){
            HelpText = help;
        }
    }

	public class Commands
	{
		[BotCmd("ping")]
        [BotHelp("Ping the server and get the delay.")]
		public static void Ping(string cmd, string sender, MatrixRoom room){
			room.SendMessage ("Pong at " + DateTime.Now.ToLongTimeString());
		}

		[BotCmd("sfw")]
		[BotHelp("Get the sfw rating of this room")]
		public static void GetSFWRating(string cmd, string sender, MatrixRoom room){
			string level = GetRoomLevel(room);
            MMessageCustomHTML htmlmsg = new MMessageCustomHTML();
			if(level == ""){
				htmlmsg.formatted_body = "This room is set to <strong><font color=\"red\">explicit</font></strong>";
				htmlmsg.body = "This room is set to explicit";
			}
			else if(level == "-rating:e"){
				htmlmsg.formatted_body = "This room is set to <strong><font color=\"purple\">questionable</font></strong>";
				htmlmsg.body = "This room is set to questionable";
			}
			else{
				htmlmsg.formatted_body = "This room is set to <strong><font color=\"green\">safe</font></strong> (the default)";
				htmlmsg.body = "This room is set to safe (the default)";
			}
            room.SendMessage(htmlmsg);
		}

		public static string GetRoomLevel(MatrixRoom room){
			IniParser.Model.KeyDataCollection rooms = Configuration.Config["room_rating"];
			if(room.CanonicalAlias != null){
				if(rooms.ContainsKey(room.CanonicalAlias)){
					switch(rooms[room.CanonicalAlias]){
						case "e":
							return "";
						case "q":
							return "-rating:e";
					}
				}
			}	
			return "rating:s";
		}

		[BotCmd("[tags]")]
		[BotFallback()]
        [BotHelp("Find images on e621 by the given tags. The image will be randomized.")]
		public static void E621(string cmd, string sender, MatrixRoom room){
			try
			{
				//TODO: Validate this 
				//if(!tags.All(x => { return new Regex("/([\\w|:|-]+)+/",RegexOptions.ECMAScript).IsMatch(x)}){
				//	throw new
				//} 

				List<string> tags = cmd.Split(' ').ToList();
				string ratingString = GetRoomLevel(room);
				if(ratingString != ""){
					if(!tags.Contains(ratingString)){
						tags.Insert(0,ratingString);
					}
				}

				if(tags.Count > 6){
					throw new Exception("Tag limit of 6 exceeded (this includes the enforced rating).");
				}



				string url = String.Format("https://e621.net/post/index.json?limit=20&tags={0}",string.Join(" ",tags));
				Task<string> data = Program.http.GetStringAsync(url);
				data.Wait();
				
				JArray images = JArray.Parse(data.Result);
	
	
				if(images.Count == 0){
					throw new Exception("Couldn't find an image with those tags.");
				}
	
				string ext = "bacon";
				JObject image = null;
				int retries = 5;
				while(ext != "png" && ext != "jpg" && ext != "jpeg" && ext != "gif" && ext != "webm" && ext != "mp4" &&  retries >= 0){
					image = (JObject)images[new Random().Next(0,images.Count)];
					ext = image.GetValue("file_ext").ToObject<string>();
					retries--;
				}
	
				if(image == null){
					throw new Exception("Couldn't find an image with those tags.");
				}
				url = image.GetValue("file_url").ToObject<string>();
				Task<byte[]> bytes = Program.http.GetByteArrayAsync(url);
				bytes.Wait();

				//Do mimetypes
				string mime = "";
				switch(ext){
					case "jpg":
					case "jpeg":
						mime = "image/jpeg";
						break;
					case "gif":
						mime = "image/gif";
						break;
					case "png":
						mime = "image/png";
						break;
					case "webm":
						mime = "video/webm";
						break;
					case "mp4":
						mime = "video/mp4";
						break;
				}

				MatrixMediaFile file = Program.Client.UploadFile(mime,bytes.Result);
				MMessageImage msg = new MMessageImage();
				msg.url = file.GetMXCUrl();
				msg.body = url;
				room.SendMessage(msg);
			}
			catch(Exception e){
				room.SendNotice(e.Message);
			}
		}

		[BotCmd("help")]
        [BotHelp("This help text.")]
		public static void Help(string cmd, string sender, MatrixRoom room){
            string helptext = "";
            foreach(MethodInfo method in typeof(Commands).GetMethods(BindingFlags.Static|BindingFlags.Public)){
                BotCmd c = method.GetCustomAttribute<BotCmd> ();
                BotHelp h= method.GetCustomAttribute<BotHelp> ();
				
                if (c != null) {
                    helptext += String.Format("<p><strong>{0}</strong> {1}</p>",c.CMD, h != null ? System.Web.HttpUtility.HtmlEncode(h.HelpText) : "");
                }
            }
            MMessageCustomHTML htmlmsg = new MMessageCustomHTML();
            htmlmsg.body = helptext.Replace("<strong>","").Replace("</strong>","").Replace("<p>","").Replace("</p>","\n");
            htmlmsg.formatted_body = helptext;
            room.SendMessage(htmlmsg);
       }
	}
}

