using System.Resources;
using System.IO;
using System.Linq;
using System;

namespace GenerateEntityFromDB
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Any(a => a == "-?" || a == "-h" || a == "/?" || a == "/h"))
            {
                Console.WriteLine(@"対話式");
                Console.WriteLine(@"GES_DB.exe");
                Console.WriteLine(@"コマンドライン引数指定");
                Console.WriteLine(@"GES_DB.exe ""DB接続文字列"" ""出力先ディレクトリパス"" [""namespace（省略可）""]");
                Console.WriteLine(@"ヘルプ");
                Console.WriteLine(@"GES_DB.exe -h");
                return;
            }
            if (args.Any(a => a == "-v" || a == "/v"))
            {
                Console.WriteLine($"Generate Entity Sourcecode from DB  Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}");
                return;
            }
            var nameSpace = "";
            var outputPath = "";
            var connectionString = "";
            if (args.Length > 2)
            {
                connectionString = args[0];
                outputPath = args[1];
                if (args.Length == 3)
                {
                    nameSpace = args[2];
                }
            }
            else
            {
                while (connectionString.Length == 0)
                {
                    Console.Write("DB接続文字列を入力してください。:");
                    connectionString = Console.ReadLine();
                }
                while (outputPath.Length == 0)
                {
                    Console.Write("出力先ディレクトリのパスを入力してください。:");
                    outputPath = Console.ReadLine();
                }
                Console.Write("namespaceを入力してください（省略可）:");
                nameSpace = Console.ReadLine();
            }
            while (outputPath.Last() == '\\')
            {
                outputPath = outputPath.Remove(outputPath.Length - 1);
            }
            var output = new OutputCSFile(nameSpace, outputPath);
            var tableInfos = GenerateFromDB.GetTableInfos(connectionString);
            if (tableInfos.Count() == 0) return;
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
            tableInfos.AsParallel().ForAll(info => {
                var sourceCodes = output.GenerateEntityClassSourceCode(info);
                output.WriteFile(info.Name, sourceCodes);
                Console.WriteLine($"Class:{info.Name}を出力しました。");
            });
            var dapperEx = output.GenerateDapperExtentionClassSourceCode();
            output.WriteFile("DapperExtentions", dapperEx);
            var iEntity = output.GenerateIEntitySourceCode();
            output.WriteFile("IEntity", iEntity);
            Console.Write("出力が完了しました。何かキーを押してください。");
            Console.ReadLine();
        }
    }
}
