using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace BlackDesertRu.Bot.Data;

public class Bot
{
    private readonly DiscordSocketClient _discord = new(new DiscordSocketConfig()
		{
			GatewayIntents = GatewayIntents.Guilds
            | GatewayIntents.GuildMessages
            | GatewayIntents.MessageContent
		});

    private readonly string _token;

    private readonly MessageInfo _messageInfo;

    private readonly string[] _downloadUrls;

    private readonly bool _createMessage = false;

    private bool _isReady = false;

    private Logger _log = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .Enrich.FromLogContext()
            .WriteTo.File("log.log", rollingInterval: RollingInterval.Day )
            .WriteTo.Console()
            .CreateLogger();

    public Bot(string token)
    {
        _token = token;

        //Включаю логирование событий и реагирование на событие Ready(оно срабатывает когда бот подключается и готов к работе)
        _discord.Log += LogAsync;
        _discord.Ready += Ready;


        var files = Directory.GetFiles(Directory.GetCurrentDirectory()).ToList();


        //Проверка на наличие необходимых файлов
        if (!files.Any(f => f.Contains("messageId.txt")))
            throw  new ArgumentNullException(
                "message.txt",
                "message.txt was not found"
            );
        if(!files.Any(f => f.Contains("urls.txt")))
            throw  new ArgumentNullException(
                "urls.txt",
                "urls.txt was not found"
            );


        //Читаем все необходимые нам файлики
        var messageInfo = File.ReadLines(Path.Join(Directory.GetCurrentDirectory(), "messageId.txt")).ToArray();
        _downloadUrls = File.ReadLines(Path.Join(Directory.GetCurrentDirectory(), "urls.txt")).ToArray();


        //Если количество ссылок меньше, чем указано регионов в UpdateRegion, то выпадает ошибка
        if(_downloadUrls.Length<Enum.GetNames(typeof(UpdateRegion)).Length-1)
            throw new ArgumentException(nameof(_downloadUrls));


        //Здесь 2 - количество дополнительных полей, которые не могут иметь ссылки на скачивание
        if(messageInfo.Length>3||messageInfo.Length<2)
            throw new ArgumentException(nameof(messageInfo));

        else if(messageInfo.Length==2)
        {
            _createMessage = true;
            _messageInfo = new(ulong.Parse(messageInfo[0]),ulong.Parse(messageInfo[1]), null);
            return;
        }
        
        _messageInfo = new(ulong.Parse(messageInfo[0]),ulong.Parse(messageInfo[1]),ulong.Parse(messageInfo[2]));
    }

    public async Task Update(UpdateRegion[] regions)
    {
        //Это нужно для ожидания, пока бот запустится. Так, на всякий
        for(int i = 0;!_isReady;i++)
        {
            if(i>25)
                throw new Exception("It took 25 seconds, but the bot did not start. Interrupted.");
            
            await Task.Delay(1000);
        }
        _log.Information("[MessageUpdater] Starts message update");

        EmbedBuilder infoEmbed = new()
        {
            Title = Format.Bold("ВНИМАНИЕ"),
            Author = new()
            {
                IconUrl = _discord.CurrentUser.GetAvatarUrl(),
                Name = _discord.CurrentUser.Username
            },
            Color = new Color(0,255,255),
            Description = 
$@"Для новой локализации теперь не нужен никакой скрипт и если у вас уже стоит старый русификатор, то выполните следующие действия

1️⃣ Удалите файлы принадлежащие старому русификатору(папки `ads` и `resource`, а также файлы `!resource_ru.cmd` и `lang_en_ru.txt`).
2️⃣ Удалите `Resource.ini`
3️⃣ Закиньте Resource.ini согласно вашему региону.
        Скачать: {Format.Url("Resource","https://drive.google.com/file/d/1fVHo7uHhYH0UFtRlx4mG3YG2B3-BOBSc/view?usp=sharing")}
4️⃣ Распакуйте папку ads из новой локализации в корень игры

Не скачивает с Google Диска? Ответ 👉 https://discord.com/channels/335183492696768522/447756895491719170/1206908101668896778",
        };
        EmbedBuilder uninstallEmbed = new()
        {
            Title = "Удаление локализации",
            Color = Color.Red,
            Description =
@"1️⃣ Откройте папку с игрой

2️⃣ Удалите файлы `ads_files` и `ads_version`

3️⃣ Запустите лаунчер игры и он сам обновит файлы локализации на оригинальные

4️⃣ Играйте",
            Timestamp = DateTimeOffset.Now
        };


        var channel = (SocketTextChannel)_discord.GetGuild(_messageInfo.GuildId).GetChannel(_messageInfo.ChannelId);

        if(channel==null)
            throw new ArgumentNullException(nameof(channel), "An error occurred while getting the channel");


        //Если не было указан id сообщения(а он изначально не должен быть указан), то создаём сообщение
        if(_createMessage)
        {
            _log.Information("[MessageUpdater] Starts creating of new message");

            EmbedBuilder installEmbed = new()
            {
                Title = "Установка локализации текста",
                Color = Color.Green,
                Description=
$@"0️⃣ При первой установки локализации скачайте и распакуйте архив со шрифтом в папку игры:
Скачать: {Format.Url("font","https://drive.google.com/file/d/1211cPdoZ-9ppJWNMYFA9sCtkdY12hgrm/view?usp=sharing")}

1️⃣ Скачайте текст для вашего региона (ссылки будут перечислены после инструкции).

2️⃣ Распакуйте архив в папку игры с заменой файлов.

3️⃣ Наслаждайтесь игрой!",
                Fields = {
                    new()
                    {
                        Name = "Россия",
                        Value = $"Обновление от ???"
                    },
                    new()
                    {
                        Name = "Европа",
                        Value = $"Скачать: {Format.Url("Google Drive",_downloadUrls[1])} - Обновление от ???"
                    },
                    new()
                    {
                        Name = "Тайвань",
                        Value = $"Скачать: {Format.Url("Google Drive",_downloadUrls[2])} - Обновление от ???"
                    },
                    new()
                    {
                        Name = "Турция",
                        Value = $"Скачать: {Format.Url("Google Drive",_downloadUrls[3])} - Обновление от ???"
                    },
                    new()
                    {
                        Name = "Южная Америка",
                        Value = $"Скачать: {Format.Url("Google Drive",_downloadUrls[4])} - Обновление от ???"
                    },
                    new()
                    {
                        Name = "Япония",
                        Value = $"Скачать: {Format.Url("Google Drive",_downloadUrls[5])} - Обновление от ???"
                    },
                    new()
                    {
                        Name = "Тест сервер",
                        Value = $"Скачать: {Format.Url("Google Drive",_downloadUrls[6])} - Обновление от ???"
                    }
                }
            };

            var sendedMessage = await channel.SendMessageAsync(embeds:[infoEmbed.Build(),installEmbed.Build(),uninstallEmbed.Build()]);
            _log.Information("[MessageUpdater] Message sended");

            //Сохраняем id сообщения в файлик
            File.AppendAllText(Path.Join(Directory.GetCurrentDirectory(), "messageId.txt"),"\n"+sendedMessage.Id.ToString());

            await _discord.LogoutAsync();
            await _discord.StopAsync();
            return;
        }

        _log.Information("[MessageUpdater] Starts editing of existing message");

        var message = (IUserMessage)await channel.GetMessageAsync(id: _messageInfo.MessageId??0);

        if(message == null)
            throw new ArgumentNullException(nameof(message), "An error occurred while getting the message");

        //Поиск необходимого тела по его названию
        var embedBuilder = message.Embeds.FirstOrDefault(embed=>embed.Title=="Установка локализации текста").ToEmbedBuilder();


        //Реагирование на пересылку сообщения
        if(regions.Contains(UpdateRegion.ResendMessage))
        {
            _log.Information($"[MessageUpdater] Received {nameof(UpdateRegion.ResendMessage)}");

            //Обновляем все поля
            for(int i = 0; i<embedBuilder.Fields.Count; i++)
            {
                embedBuilder.Fields[i].Value = //Если ссылка присутствует, то добавляем её.
                        (_downloadUrls[i]!=string.Empty?$"Скачать: {Format.Url("Google Drive",_downloadUrls[i])} - ":"")
                        +$"Обновление от <t:{DateTimeOffset.Now.ToUnixTimeSeconds()}>";
            }

            await message.DeleteAsync();
            var newMessage = await channel.SendMessageAsync(embeds:[infoEmbed.Build(),embedBuilder.Build(),uninstallEmbed.Build()]);

            //Обновляем файлик с информацией о сообщении
            File.WriteAllLines(Path.Join(Directory.GetCurrentDirectory(), "messageId.txt"), [channel.Guild.Id.ToString(), channel.Id.ToString(), newMessage.Id.ToString()]);

            _log.Information("[MessageUpdater] Message updated");
            await _discord.LogoutAsync();
            await _discord.StopAsync();
            return;
        }


        for(int i = 0; i < regions.Length; i++)
        {
            _log.Information($"[MessageUpdater] Received {regions[i]}");
            embedBuilder.Fields[((int)regions[i])].Value = //Если ссылка присутствует, то добавляем её.
                        (_downloadUrls[(int)regions[i]]!=string.Empty?$"Скачать: {Format.Url("Google Drive",_downloadUrls[(int)regions[i]])} - ":"")
                        +$"Обновление от <t:{DateTimeOffset.Now.ToUnixTimeSeconds()}>";
        }

        await message.ModifyAsync(msg=>{
            msg.Embeds = new Optional<Embed[]>([infoEmbed.Build(),embedBuilder.Build(),uninstallEmbed.Build()]);
        });

        _log.Information("[MessageUpdater] Message updated");
        await _discord.LogoutAsync();
        await _discord.StopAsync();
    }

    public async Task Init()
    {
        await _discord.LoginAsync(TokenType.Bot, _token);
        await _discord.StartAsync();
    }

    private async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };
        
        _log.Write(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);
        await Task.CompletedTask;
    }

    private Task Ready()
    {
        _isReady = true;
        return Task.CompletedTask;
    }
    
}

public class MessageInfo(ulong guildId, ulong channelId, ulong? messageId)
{
    public ulong GuildId {get;} = guildId;

    public ulong ChannelId {get;} = channelId;

    public ulong? MessageId {get;set;} = messageId;

}

public enum UpdateRegion
{
    Russia,
    Europe,
    Taiwan,
    Turkey,
    SouthAmerica,
    Japan,
    TestServer,
    ResendMessage
}