namespace BlackDesertRu.Bot
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            if(!File.Exists(Path.Join(Directory.GetCurrentDirectory(), "token.txt")))
                throw  new ArgumentNullException(
                "token.txt",
                "token.txt was not found"
            );

            var bot = new Data.Bot("MTA5NzA3NDk4MDkxMjYzMTgyOA.GuOq8k.26-DAybnUVGwGR84DM2qWvYe_x6oeASvnK65jk");

            await bot.Init();

            if(args.Any(a=>!Enumerable.Range(0, (int)Data.UpdateRegion.ResendMessage+1).ToList().Contains(int.Parse(a))))
                throw new IndexOutOfRangeException(nameof(args));

            await bot.Update(Array.ConvertAll(args,Enum.Parse<Data.UpdateRegion>));
        }
    }
}