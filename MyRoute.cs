using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SimpleHttpServer.Models;
using SimpleHttpServer.RouteHandlers;

namespace h5manager
{
    class MyRoute
    {
        private static string basePath = "projects/" + Form1.proj;
        private FileSystemRouteHandler handler = new FileSystemRouteHandler() { BasePath = basePath, ShowDirectories = true };

        public HttpResponse getHandler(HttpRequest request)
        {
            if (request.Path.StartsWith("__all_floors__.js"))
            {
                var ids = request.Path.IndexOf("&id=");
                if (ids >= 0)
                {
                    string[] floorIds = request.Path.Substring(ids + 4).Split(',');
                    var content = "";
                    foreach (string floorId in floorIds)
                    {
                        string filename = basePath + "/project/floors/" + floorId + ".js";
                        if (!File.Exists(filename))
                        {
                            return new HttpResponse()
                            {
                                ContentAsUTF8 = "Request Not found.",
                                ReasonPhrase = "Not Found",
                                StatusCode = "404",
                            };
                        }
                        content += File.ReadAllText(filename, Encoding.UTF8) + "\n";
                    }
                    return new HttpResponse()
                    {
                        ContentAsUTF8 = content,
                        StatusCode = "200",
                        ReasonPhrase = "OK"
                    };
                }

            }
            if (request.Path.StartsWith("__all_animates__"))
            {
                var ids = request.Path.IndexOf("&id=");
                if (ids >= 0)
                {
                    string[] floorIds = request.Path.Substring(ids + 4).Split(',');
                    var content = new List<string>();
                    foreach (string floorId in floorIds)
                    {
                        string filename = basePath + "/project/animates/" + floorId + ".animate";
                        content.Add(File.Exists(filename) ? File.ReadAllText(filename, Encoding.UTF8) : "");
                    }
                    return new HttpResponse()
                    {
                        ContentAsUTF8 = string.Join("@@@~~~###~~~@@@", content),
                        StatusCode = "200",
                        ReasonPhrase = "OK"
                    };
                }
            }
            return handler.Handle(request);
        }
        public HttpResponse postHandler(HttpRequest request)
        {
            //Console.WriteLine(Form1.proj);
            string[] strings = request.Content.Split('&');
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (string s in strings)
            {
                // string[] keyvalue = s.Split('=');
                int index = s.IndexOf("=");
                if (index > 0)
                {
                    dictionary.Add(s.Substring(0, index), s.Substring(index + 1));
                }
                // dictionary.Add(keyvalue[0], System.Web.HttpUtility.UrlDecode(keyvalue[1], Encoding.UTF8));
            }

            // Console.WriteLine(request.Path);

            if (request.Path.StartsWith("readFile"))
                return readFileHandler(dictionary);

            if (request.Path.StartsWith("writeFile"))
                return writeFileHandler(dictionary);

            if (request.Path.StartsWith("writeMultiFiles"))
                return writeMultiFilesHandler(dictionary);

            if (request.Path.StartsWith("listFile"))
                return listFileHandler(dictionary);

            if (request.Path.StartsWith("makeDir"))
                return makeDirHandler(dictionary);

            if (request.Path.StartsWith("moveFile"))
                return moveFileHandler(dictionary);

            if (request.Path.StartsWith("deleteFile"))
                return deleteFileHandler(dictionary);

            return new HttpResponse()
            {
                ContentAsUTF8 = "Request Not found.",
                ReasonPhrase = "Not Found",
                StatusCode = "404",
            };

        }

        private static HttpResponse readFileHandler(Dictionary<String, String> dictionary)
        {

            string type = dictionary["type"];
            if (type == null || !type.Equals("base64")) type = "utf8";
            string filename = basePath + "/" + dictionary["name"];
            if (filename == null || !File.Exists(filename))
            {
                return new HttpResponse()
                {
                    ContentAsUTF8 = "File Not Exists!",
                    StatusCode = "404",
                    ReasonPhrase = "Not found"
                };
            }
            byte[] bytes = File.ReadAllBytes(filename);
            return new HttpResponse()
            {
                ContentAsUTF8 = type == "base64" ? Convert.ToBase64String(bytes) : Encoding.UTF8.GetString(bytes),
                StatusCode = "200",
                ReasonPhrase = "OK"
            };
        }

        private static HttpResponse writeFileHandler(Dictionary<String, String> dictionary)
        {
            string type = dictionary["type"];
            if (type == null || !type.Equals("base64")) type = "utf8";
            string filename = basePath + "/" + dictionary["name"], content = dictionary["value"];
            byte[] bytes;
            if (type == "base64")
                bytes = Convert.FromBase64String(content);
            else
                bytes = Encoding.UTF8.GetBytes(content);
            File.WriteAllBytes(filename, bytes);
            return new HttpResponse()
            {
                ContentAsUTF8 = Convert.ToString(bytes.Length),
                StatusCode = "200",
                ReasonPhrase = "OK"
            };
        }

        private static HttpResponse writeMultiFilesHandler(Dictionary<String, String> dictionary)
        {
            string filename = basePath + "/" + dictionary["name"], content = dictionary["value"];

            string[] filenames = filename.Split(';'), contents = content.Split(';');
            long length = 0;
            for (int i = 0; i < filenames.Length; ++i)
            {
                if (i >= contents.Length) continue;
                byte[] bytes = Convert.FromBase64String(contents[i]);
                length += bytes.LongLength;
                File.WriteAllBytes(filenames[i], bytes);
            }

            return new HttpResponse()
            {
                ContentAsUTF8 = Convert.ToString(length),
                StatusCode = "200",
                ReasonPhrase = "OK"
            };
        }

        private static HttpResponse listFileHandler(Dictionary<string, string> dictionary)
        {
            string name = basePath + "/" + dictionary["name"];
            if (name == null || !Directory.Exists(name))
            {
                return new HttpResponse()
                {
                    ContentAsUTF8 = "Directory Not Exists!",
                    StatusCode = "404",
                    ReasonPhrase = "Not found"
                };
            }
            string[] filenames = Directory.GetFiles(name);
            for (int i = 0; i < filenames.Length; i++) filenames[i] = "\"" + Path.GetFileName(filenames[i]) + "\"";
            string content = "[" + string.Join(", ", filenames) + "]";
            //Console.WriteLine(content);
            return new HttpResponse()
            {
                ContentAsUTF8 = content,
                StatusCode = "200",
                ReasonPhrase = "OK"
            };
        }

        private static HttpResponse makeDirHandler(Dictionary<string, string> dictionary)
        {
            string name = basePath + "/" + dictionary["name"];
            if (Directory.Exists(name))
            {
                return new HttpResponse()
                {
                    ContentAsUTF8 = "Directory Already Exists!",
                    StatusCode = "200",
                    ReasonPhrase = "OK"
                };
            }
            Directory.CreateDirectory(name);
            return new HttpResponse()
            {
                ContentAsUTF8 = "Make Directory Success",
                StatusCode = "200",
                ReasonPhrase = "OK"
            };
        }

        private static HttpResponse moveFileHandler(Dictionary<String, String> dictionary)
        {
            string src = dictionary["src"];
            string dest = dictionary["dest"];
            if (src == null || !File.Exists(src))
            {
                return new HttpResponse()
                {
                    ContentAsUTF8 = "File Not Exists!",
                    StatusCode = "404",
                    ReasonPhrase = "Not found"
                };
            }
            if (dest == null)
            {
                return new HttpResponse()
                {
                    ContentAsUTF8 = "Must Provide Destination!",
                    StatusCode = "404",
                    ReasonPhrase = "Not found"
                };
            }
            if (!Path.Equals(src, dest))
            {
                if (File.Exists(dest) && !Path.Equals(src, dest))
                {
                    File.Delete(dest);
                }
                File.Move(src, dest);
            }
            return new HttpResponse()
            {
                ContentAsUTF8 = "Move Success",
                StatusCode = "200",
                ReasonPhrase = "OK"
            };
        }

        private static void deleteFile(string path)
        {
            //Console.WriteLine(path);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (string f in Directory.GetFileSystemEntries(path))
                {
                    deleteFile(f);
                }
                Directory.Delete(path);
            }
        }

        private static HttpResponse deleteFileHandler(Dictionary<String, String> dictionary)
        {
            string filename = basePath + "/" + dictionary["name"];
            if (filename == null || !(File.Exists(filename) || Directory.Exists(filename)))
            {
                return new HttpResponse()
                {
                    ContentAsUTF8 = "File Not Exists!",
                    StatusCode = "404",
                    ReasonPhrase = "Not found"
                };
            }
            deleteFile(filename);
            return new HttpResponse()
            {
                ContentAsUTF8 = "Delete Success",
                StatusCode = "200",
                ReasonPhrase = "OK"
            };
        }
    }
}
