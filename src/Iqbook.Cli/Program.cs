using Iqbook.Core;
using System.Text.Json;

if (args.Length == 0)
{
    PrintHelp();
    return;
}

var svc = new IqbookPackageService();

switch (args[0])
{
    case "init-sample":
        {
            var dir = args.Length > 1 ? args[1] : "sample_book";
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, "assets/images"));
            Directory.CreateDirectory(Path.Combine(dir, "assets/maps"));

            var content = new StoryContent
            {
                StartNodeId = "start",
                Nodes = new()
                {
                    new Node{ Id="start", Type="dialogue", Text="Вы стоите перед замком.", Illustration="assets/images/castle.png", Choices = new(){ new Choice{ Text="Войти", NextNode="hall" }, new Choice{ Text="Уйти", NextNode="forest" } } },
                    new Node{ Id="hall", Type="map", Map="assets/maps/hall.json" },
                    new Node{ Id="forest", Type="dialogue", Text="Вы ушли в лес.", Choices = new() }
                }
            };
            File.WriteAllText(Path.Combine(dir, "content.json"), JsonSerializer.Serialize(content, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(Path.Combine(dir, "assets/maps/hall.json"), "{\n  \"image\": \"assets/images/hall_map.png\",\n  \"zones\": [\n    { \"rect\": [0.1,0.2,0.3,0.2], \"node\": \"forest\" }\n  ]\n}");
            File.WriteAllBytes(Path.Combine(dir, "assets/images/castle.png"), Array.Empty<byte>());
            File.WriteAllBytes(Path.Combine(dir, "assets/images/hall_map.png"), Array.Empty<byte>());
            Console.WriteLine($"Sample initialized at {dir}");
            break;
        }
    case "gen-keys":
        {
            var outDir = args.Length > 1 ? args[1] : ".";
            var (priv, pub) = Crypto.GenerateRsaPem();
            File.WriteAllText(Path.Combine(outDir, "private.pem"), priv);
            File.WriteAllText(Path.Combine(outDir, "public.pem"), pub);
            Console.WriteLine("Keys generated.");
            break;
        }
    case "pack":
        {
            // pack <sourceDir> <out.iqbook> <title> <author> <private.pem> <public.pem> [password]
            if (args.Length < 7) throw new ArgumentException("pack requires 6+ args");
            svc.CreatePackage(args[1], args[2], args[3], args[4], File.ReadAllText(args[5]), File.ReadAllText(args[6]), args.Length > 7 ? args[7] : null);
            Console.WriteLine("Package created.");
            break;
        }
    case "play":
        {
            // play <book.iqbook> [password]
            var loaded = svc.LoadAndVerify(args[1], args.Length > 2 ? args[2] : null);
            Console.WriteLine($"Loaded: {loaded.Metadata.Title} by {loaded.Metadata.Author}");
            var engine = new StoryEngine(loaded.Story);
            while (true)
            {
                Console.WriteLine($"\n[{engine.CurrentNode.Id}] {engine.CurrentNode.Text ?? "(media node)"}");
                var choices = engine.AvailableChoices();
                if (choices.Count == 0)
                {
                    Console.WriteLine("Конец ветки.");
                    break;
                }
                for (var i = 0; i < choices.Count; i++) Console.WriteLine($"{i + 1}. {choices[i].Text}");
                Console.Write("> ");
                if (!int.TryParse(Console.ReadLine(), out var num) || num < 1 || num > choices.Count) continue;
                engine.Choose(num - 1);
            }
            break;
        }
    default:
        PrintHelp();
        break;
}

static void PrintHelp()
{
    Console.WriteLine("iqbook commands:");
    Console.WriteLine("  init-sample <dir>");
    Console.WriteLine("  gen-keys <dir>");
    Console.WriteLine("  pack <sourceDir> <out.iqbook> <title> <author> <private.pem> <public.pem> [password]");
    Console.WriteLine("  play <book.iqbook> [password]");
}
