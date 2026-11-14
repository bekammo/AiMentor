// P0-T6 technical spike. Throwaway code; findings are recorded in docs/spikes.md.
//
//   dotnet run -- agent              Agent Framework 1.28 with a fake IChatClient (offline)
//   dotnet run -- tokens             Georgian token counts with o200k (offline)
//   dotnet run -- vector             SqlVector<float> + VectorDistance     (env SPIKE_SQL)
//   dotnet run -- github             Georgian chat + embeddings, GitHub Models (env GITHUB_TOKEN)
//   dotnet run -- azure              Azure OpenAI v1 endpoint with the OpenAI client (env AZURE_OPENAI_ENDPOINT, AZURE_OPENAI_API_KEY, AZURE_OPENAI_CHAT, AZURE_OPENAI_EMBED)
//   dotnet run -- pdf <dir>          PdfPig text extraction on real lecture PDFs
using System.ClientModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.ML.Tokenizers;
using OpenAI;
using UglyToad.PdfPig;

Console.OutputEncoding = Encoding.UTF8;
var command = args.FirstOrDefault() ?? "help";
return command switch
{
    "agent" => await Spike.Agent(),
    "tokens" => Spike.Tokens(),
    "vector" => await Spike.Vector(),
    "github" => await Spike.Remote(
        new Uri("https://models.github.ai/inference"),
        Spike.Env("GITHUB_TOKEN"),
        Environment.GetEnvironmentVariable("SPIKE_CHAT_MODEL") ?? "openai/gpt-4.1-mini",
        Environment.GetEnvironmentVariable("SPIKE_EMBED_MODEL") ?? "openai/text-embedding-3-small"),
    "azure" => await Spike.Remote(
        new Uri(Spike.Env("AZURE_OPENAI_ENDPOINT").TrimEnd('/') + "/openai/v1/"),
        Spike.Env("AZURE_OPENAI_API_KEY"),
        Spike.Env("AZURE_OPENAI_CHAT"),
        Spike.Env("AZURE_OPENAI_EMBED")),
    "pdf" => Spike.Pdf(args.ElementAtOrDefault(1) ?? Path.Combine("..", "data")),
    _ => Spike.Help(),
};

static class Spike
{
    public const string Question = "რა არის რეკურსია და რა არის მისი საბაზისო შემთხვევა?";

    public static readonly string[] Passages =
    [
        // relevant
        "რეკურსია არის ფუნქციის მიერ საკუთარი თავის გამოძახება. ყოველ რეკურსიულ ფუნქციას სჭირდება საბაზისო შემთხვევა, რომელიც აჩერებს გამოძახებებს, წინააღმდეგ შემთხვევაში სტეკი გადაივსება.",
        // same course, different topic
        "მასივი არის ერთი ტიპის ელემენტების მიმდევრობა, რომლებიც მეხსიერებაში ერთმანეთის გვერდით ინახება და ინდექსით მიიღწევა.",
        // unrelated
        "საქართველოს დედაქალაქი თბილისია, რომელიც მტკვრის ნაპირზე მდებარეობს.",
    ];

    public static string Env(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } v ? v : throw new InvalidOperationException($"Set environment variable {name}.");

    public static int Help()
    {
        Console.WriteLine("Commands: agent | tokens | vector | github | azure | pdf <dir>");
        return 1;
    }

    // ---------------------------------------------------------------- agent
    public static async Task<int> Agent()
    {
        var fake = new FakeChatClient("რეკურსია არის ფუნქციის თვითგამოძახება [1].");
        ChatClientAgent agent = fake.AsAIAgent(instructions: "უპასუხე მხოლოდ ქართულად.", name: "mentor");
        var session = await agent.CreateSessionAsync();

        AgentResponse response = await agent.RunAsync(Question, session);
        Console.WriteLine($"RunAsync text: {response.Text}");
        Console.WriteLine($"System prompt reached client: {fake.LastMessages.Any(m => m.Role == ChatRole.System) || fake.LastOptions?.Instructions is not null}");
        Console.WriteLine($"  options.Instructions = {fake.LastOptions?.Instructions}");

        var sb = new StringBuilder();
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("და მაგალითი?", session))
        {
            sb.Append(update.Text);
        }
        Console.WriteLine($"RunStreamingAsync text: {sb}");
        Console.WriteLine($"History sent on 2nd turn: {fake.LastMessages.Count} messages (expects prior turn included)");

        var run = new ChatClientAgentRunOptions(new ChatOptions { Temperature = 0.2f, MaxOutputTokens = 200 });
        await agent.RunAsync("ტესტი", await agent.CreateSessionAsync(), run);
        Console.WriteLine($"Per-run ChatOptions honored: temperature={fake.LastOptions?.Temperature}, max={fake.LastOptions?.MaxOutputTokens}");
        return 0;
    }

    sealed class FakeChatClient(string reply) : IChatClient
    {
        public List<ChatMessage> LastMessages { get; private set; } = [];
        public ChatOptions? LastOptions { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            LastOptions = options;
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, reply)));
        }

        public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            LastMessages = messages.ToList();
            LastOptions = options;
            foreach (var word in reply.Split(' '))
            {
                await Task.Yield();
                yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
            }
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    // ---------------------------------------------------------------- tokens
    public static int Tokens()
    {
        var tokenizer = TiktokenTokenizer.CreateForEncoding("o200k_base");
        var english = "Recursion is when a function calls itself. Every recursive function needs a base case that stops the calls, otherwise the stack overflows.";
        foreach (var (label, text) in new[] { ("ka", Passages[0]), ("en", english), ("ka-question", Question) })
        {
            var words = text.Split((char[])[' ', ',', '.', '?'], StringSplitOptions.RemoveEmptyEntries).Length;
            var tokens = tokenizer.CountTokens(text);
            Console.WriteLine($"{label,-12} words={words,3} tokens={tokens,3} tokens/word={tokens / (double)words:F2} chars={text.Length}");
        }
        foreach (var size in new[] { 300, 600 })
        {
            var perWord = tokenizer.CountTokens(Passages[0]) / (double)Passages[0].Split(' ').Length;
            Console.WriteLine($"chunk {size} tokens ≈ {size / perWord:F0} Georgian words");
        }
        return 0;
    }

    // ---------------------------------------------------------------- vector
    public static async Task<int> Vector()
    {
        var options = new DbContextOptionsBuilder<SpikeDb>().UseSqlServer(Env("SPIKE_SQL")).Options;
        await using var db = new SpikeDb(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
        Console.WriteLine("Server: " + (await db.Database.SqlQueryRaw<string>("SELECT @@VERSION AS [Value]").SingleAsync()).Split('\n')[0]);
        Console.WriteLine("Edition: " + await db.Database.SqlQueryRaw<string>("SELECT CAST(SERVERPROPERTY('Edition') AS nvarchar(128)) AS [Value]").SingleAsync());

        float[] Vec(params float[] head) { var v = new float[1024]; head.CopyTo(v, 0); return v; }
        db.Chunks.AddRange(
            new SpikeChunk { CourseId = 1, Text = "near", Embedding = new SqlVector<float>(Vec(1f, 0.1f)) },
            new SpikeChunk { CourseId = 1, Text = "far", Embedding = new SqlVector<float>(Vec(0f, 1f)) },
            new SpikeChunk { CourseId = 2, Text = "other course, identical vector", Embedding = new SqlVector<float>(Vec(1f, 0f)) });
        await db.SaveChangesAsync();

        var query = new SqlVector<float>(Vec(1f, 0f));
        var q = db.Chunks
            .Where(c => c.CourseId == 1)
            .OrderBy(c => EF.Functions.VectorDistance("cosine", c.Embedding, query))
            .Select(c => new { c.Id, c.Text, Distance = EF.Functions.VectorDistance("cosine", c.Embedding, query) })
            .Take(5);
        Console.WriteLine("SQL:\n" + q.ToQueryString());
        foreach (var r in await q.ToListAsync())
        {
            Console.WriteLine($"  {r.Id} {r.Text,-32} distance={r.Distance:F4}");
        }

        var roundTrip = await db.Chunks.AsNoTracking().FirstAsync(c => c.Text == "near");
        Console.WriteLine($"Round-trip length={roundTrip.Embedding.Length}, [0..2]={string.Join(",", roundTrip.Embedding.Memory.Span[..2].ToArray())}");

        var sw = Stopwatch.StartNew();
        var rnd = new Random(42);
        for (var batch = 0; batch < 5; batch++)
        {
            for (var i = 0; i < 1000; i++)
            {
                var v = new float[1024];
                for (var j = 0; j < v.Length; j++) { v[j] = (float)rnd.NextDouble(); }
                db.Chunks.Add(new SpikeChunk { CourseId = 1, Text = $"rnd {batch}/{i}", Embedding = new SqlVector<float>(v) });
            }
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
        }
        Console.WriteLine($"Inserted 5000 random 1024-d vectors in {sw.ElapsedMilliseconds} ms");
        sw.Restart();
        for (var i = 0; i < 10; i++) { await q.ToListAsync(); }
        Console.WriteLine($"Exact top-5 over ~5000 rows: {sw.ElapsedMilliseconds / 10.0:F1} ms per query");
        return 0;
    }

    sealed class SpikeDb(DbContextOptions<SpikeDb> options) : DbContext(options)
    {
        public DbSet<SpikeChunk> Chunks => Set<SpikeChunk>();

        protected override void OnModelCreating(ModelBuilder b) =>
            b.Entity<SpikeChunk>().Property(c => c.Embedding).HasColumnType("vector(1024)");
    }

    sealed class SpikeChunk
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public string Text { get; set; } = "";
        public SqlVector<float> Embedding { get; set; }
    }

    // ---------------------------------------------------------------- github / azure
    public static async Task<int> Remote(Uri endpoint, string key, string chatModel, string embedModel)
    {
        var client = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = endpoint });

        IEmbeddingGenerator<string, Embedding<float>> embedder = client.GetEmbeddingClient(embedModel).AsIEmbeddingGenerator();
        var embedOptions = new EmbeddingGenerationOptions { Dimensions = 1024 };
        var sw = Stopwatch.StartNew();
        var vectors = await embedder.GenerateAsync([Question, .. Passages], embedOptions);
        Console.WriteLine($"Embeddings: model={embedModel} count={vectors.Count} dims={vectors[0].Vector.Length} in {sw.ElapsedMilliseconds} ms");
        for (var i = 0; i < Passages.Length; i++)
        {
            Console.WriteLine($"  cos(question, passage {i}) = {Cosine(vectors[0].Vector.Span, vectors[i + 1].Vector.Span):F4}");
        }

        IChatClient chat = client.GetChatClient(chatModel).AsIChatClient();
        var agent = chat.AsAIAgent(instructions:
            "შენ ხარ უნივერსიტეტის კურსის AI მენტორი. უპასუხე მხოლოდ ქართულად და მხოლოდ მოცემული კონტექსტის საფუძველზე. " +
            "ყოველ ფაქტს მიუთითე წყარო ფორმატით [n]. თუ კონტექსტში პასუხი არ არის, თქვი: „ეს საკითხი კურსის მასალებში არ არის განხილული\".");
        var prompt = $"<context>\n[1] {Passages[0]}\n[2] {Passages[1]}\n</context>\n<question>{Question}</question>";
        sw.Restart();
        long? ttft = null;
        var text = new StringBuilder();
        UsageDetails? usage = null;
        await foreach (var update in agent.RunStreamingAsync(prompt))
        {
            ttft ??= sw.ElapsedMilliseconds;
            text.Append(update.Text);
            usage = update.Contents.OfType<UsageContent>().FirstOrDefault()?.Details ?? usage;
        }
        Console.WriteLine($"Chat: model={chatModel} ttft={ttft} ms total={sw.ElapsedMilliseconds} ms tokens in/out={usage?.InputTokenCount}/{usage?.OutputTokenCount}");
        Console.WriteLine(text.ToString());
        var georgian = text.ToString().Count(c => c is >= 'ა' and <= 'ჿ');
        var letters = text.ToString().Count(char.IsLetter);
        Console.WriteLine($"Georgian letter share: {georgian / (double)Math.Max(letters, 1):P0}; has [1]: {text.ToString().Contains("[1]")}");

        var off = await agent.RunAsync($"<context>\n[1] {Passages[1]}\n</context>\n<question>ვინ დაწერა „ვეფხისტყაოსანი\"?</question>");
        Console.WriteLine($"Out-of-context question: {off.Text}");
        return 0;
    }

    static double Cosine(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < a.Length; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    // ---------------------------------------------------------------- pdf
    public static int Pdf(string dir)
    {
        var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.pdf") : [];
        if (files.Length == 0)
        {
            Console.WriteLine($"No PDFs in {Path.GetFullPath(dir)}. Copy 3 real lecture PDFs there (git-ignored).");
            return 1;
        }
        foreach (var file in files)
        {
            using var doc = PdfDocument.Open(file);
            int georgian = 0, latin = 0, pua = 0, replacement = 0, mtavruli = 0, total = 0, emptyPages = 0;
            foreach (var page in doc.GetPages())
            {
                var text = page.Text;
                if (string.IsNullOrWhiteSpace(text)) { emptyPages++; }
                foreach (var ch in text)
                {
                    total++;
                    if (ch is >= 'ა' and <= 'ჿ') georgian++;
                    else if (ch is >= 'Ა' and <= 'Ჿ') mtavruli++;
                    else if (ch is >= 'A' and <= 'z') latin++;
                    else if (ch is >= '' and <= '') pua++;
                    else if (ch == '�') replacement++;
                }
            }
            Console.WriteLine($"{Path.GetFileName(file)}: pages={doc.NumberOfPages} empty={emptyPages} chars={total} georgian={georgian} mtavruli={mtavruli} latin={latin} privateUse={pua} replacement={replacement}");
            var p1 = doc.GetPage(Math.Min(2, doc.NumberOfPages));
            var words = string.Join(' ', p1.GetWords().Select(w => w.Text));
            Console.WriteLine($"  page {p1.Number} words: {words[..Math.Min(300, words.Length)]}");
            Console.WriteLine($"  NFC-normalized equal: {words == words.Normalize(NormalizationForm.FormC)}");
        }
        return 0;
    }
}
