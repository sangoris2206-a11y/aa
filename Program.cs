using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace SocialMediaBot;

class Program
{
    // ЗАМЕНИ НА СВОЙ ТОКЕН ОТ @BotFather
    private const string BOT_TOKEN = "8980163341:AAGQx-dVyGbS6maLNQjR7bomSdyM0oJtiMk";

    private static ITelegramBotClient _botClient;
    private static readonly Dictionary<long, QuizState> _quizStates = new();
    private static readonly Random _random = new();

    // Факты о соцсетях
    private static readonly List<string> Facts = new()
    {
        "📱 Средний пользователь тратит на соцсети 2-3 часа в день. Это ~10 лет за жизнь!",
        "🧠 Соцсети могут вызывать FOMO (страх упустить выгоду) и усиливать тревожность.",
        "❤️ Дофаминовые петли: лайки и уведомления создают зависимость, похожую на игровую.",
        "🕒 Бесконечная прокрутка (infinite scroll) специально разработана, чтобы вы забывали о времени.",
        "📸 Идеальные картинки в Instagram часто вызывают сравнение и снижают самооценку.",
        "💬 Онлайн-общение не заменяет живое: снижается эмпатия и глубина разговоров.",
        "⏰ Чрезмерное использование соцсетей связано с прокрастинацией и нарушением сна.",
        "🧘‍♀️ Цифровой детокс даже на один день улучшает настроение и концентрацию.",
        "📊 Алгоритмы соцсетей создают 'информационные пузыри', отрезая от другого мнения.",
        "👨‍👩‍👧 Соцсети могут отдалять членов семьи, даже если они рядом физически."
    };

    // Советы
    private static readonly List<string> Tips = new()
    {
        "⏲️ Ставьте таймер на 30 минут для соцсетей в день.",
        "🔕 Отключите push-уведомления – они крадут внимание.",
        "📵 Не пользуйтесь телефоном за час до сна.",
        "👀 Следите не за 'идеальными' блогерами, а за вдохновляющими профилями.",
        "🧘 Делайте цифровой выходной раз в неделю.",
        "✍️ Ведите дневник реальных встреч с друзьями.",
        "🌿 Перед заходом в соцсеть спросите себя: 'Зачем я это делаю?'"
    };

    // Вопросы викторины
    private static readonly List<QuizQuestion> QuizQuestions = new()
    {
        new QuizQuestion("Сколько в среднем времени в день люди тратят на соцсети?",
            new[] { "30 минут", "1-1.5 часа", "2-3 часа", "Более 5 часов" }, 2),
        new QuizQuestion("Что такое 'FOMO' в контексте соцсетей?",
            new[] { "Новый вид мемов", "Страх пропустить что-то интересное", "Техника фотофильтров", "Алгоритм TikTok" }, 1),
        new QuizQuestion("Что помогает снизить негативное влияние соцсетей?",
            new[] { "Ещё больше времени в сети", "Цифровой детокс", "Отключение всех уведомлений навсегда", "Просмотр stories всю ночь" }, 1),
        new QuizQuestion("Как соцсети влияют на сон?",
            new[] { "Улучшают его", "Никак", "Синий свет экранов мешает выработке мелатонина", "Помогают быстрее заснуть" }, 2)
    };

    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════╗");
        Console.WriteLine("║     Telegram Bot - HAPP VPN Support    ║");
        Console.WriteLine("╚════════════════════════════════════════╝");
        Console.WriteLine();

        Console.WriteLine();
        Console.WriteLine("🚀 Запуск бота...");

        try
        {
            // Создаем клиент Telegram
            _botClient = new TelegramBotClient(BOT_TOKEN);

            // Проверяем подключение
            Console.WriteLine("📡 Подключение к Telegram API...");

            var me = await _botClient.GetMeAsync();
            Console.WriteLine();
            Console.WriteLine($"✅ УСПЕХ! Бот @{me.Username} успешно запущен!");
            Console.WriteLine($"   ID бота: {me.Id}");
            Console.WriteLine();

            var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message, UpdateType.CallbackQuery }
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cts.Token
            );

            Console.WriteLine("🤖 Бот работает!");
            Console.WriteLine("📝 Команды:");
            Console.WriteLine("   /start - приветствие");
            Console.WriteLine("   /fact - случайный факт о соцсетях");
            Console.WriteLine("   /quiz - викторина");
            Console.WriteLine("   /tip - полезный совет");
            Console.WriteLine();
            Console.WriteLine("⏹️ Нажмите Enter для выхода...");
            Console.ReadLine();
            cts.Cancel();
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("timed out") || ex.Message.Contains("connection"))
        {
            Console.WriteLine();
            Console.WriteLine("❌ ОШИБКА ПОДКЛЮЧЕНИЯ");
            Console.WriteLine();
            Console.WriteLine("HAPP VPN включён, но .NET не может подключиться.");
            Console.WriteLine();
            Console.WriteLine("🔧 ПРОВЕРЬТЕ НАСТРОЙКИ HAPP VPN:");
            Console.WriteLine();
            Console.WriteLine("1. Откройте HAPP VPN");
            Console.WriteLine("2. Найдите настройки маршрутизации (Routing)");
            Console.WriteLine("3. Убедитесь, что включён режим 'Глобальный прокси'");
            Console.WriteLine("4. Или добавьте правило для api.telegram.org");
            Console.WriteLine();
            Console.WriteLine("📌 Альтернативное решение (работает 100%):");
            Console.WriteLine("   Напишите 'Cloudflare Worker' - я дам код,");
            Console.WriteLine("   который работает вообще без VPN");
            Console.WriteLine();
            Console.WriteLine($"Детали ошибки: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("Нажмите Enter для выхода...");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ ОШИБКА: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"   → {ex.InnerException.Message}");
            Console.WriteLine("\nПроверьте токен бота и интернет-соединение");
            Console.WriteLine("\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            // Обработка кнопок викторины
            if (update.CallbackQuery != null)
            {
                await HandleCallbackQuery(botClient, update.CallbackQuery, cancellationToken);
                return;
            }

            // Обработка сообщений
            if (update.Message is not { } message)
                return;

            var chatId = message.Chat.Id;
            var text = message.Text;

            if (string.IsNullOrEmpty(text))
                return;

            // Команды
            if (text.StartsWith("/"))
            {
                switch (text.ToLower())
                {
                    case "/start":
                        await SendStartMessage(chatId);
                        break;
                    case "/fact":
                        await SendRandomFact(chatId);
                        break;
                    case "/tip":
                        await SendRandomTip(chatId);
                        break;
                    case "/quiz":
                        await StartQuiz(chatId);
                        break;
                    default:
                        await botClient.SendTextMessageAsync(chatId, "❓ Неизвестная команда. Используйте /start");
                        break;
                }
                return;
            }

            // Свободный текст
            await HandleFreeText(chatId, text);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка в HandleUpdateAsync: {ex.Message}");
        }
    }

    private static async Task SendStartMessage(long chatId)
    {
        var text = "🧠 *Привет! Я бот об осознанном использовании социальных сетей.*\n\n" +
                   "Они влияют на настроение, продуктивность и отношения. Давай разберёмся!\n\n" +
                   "📌 *Что я умею:*\n" +
                   "🔹 /fact – случайный факт о влиянии соцсетей\n" +
                   "🔹 /quiz – викторина\n" +
                   "🔹 /tip – полезный совет для цифрового баланса\n\n" +
                   "Начни с /fact или /quiz!";

        await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown);
    }

    private static async Task SendRandomFact(long chatId)
    {
        var fact = Facts[_random.Next(Facts.Count)];
        await _botClient.SendTextMessageAsync(chatId, $"📖 *Знаете ли вы?*\n{fact}", parseMode: ParseMode.Markdown);
    }

    private static async Task SendRandomTip(long chatId)
    {
        var tip = Tips[_random.Next(Tips.Count)];
        await _botClient.SendTextMessageAsync(chatId, $"💡 *Совет дня:*\n{tip}", parseMode: ParseMode.Markdown);
    }

    private static async Task StartQuiz(long chatId)
    {
        var state = new QuizState { CurrentStep = 0, Score = 0 };
        _quizStates[chatId] = state;
        await SendQuizQuestion(chatId, 0);
    }

    private static async Task SendQuizQuestion(long chatId, int step)
    {
        if (step >= QuizQuestions.Count)
        {
            if (_quizStates.TryGetValue(chatId, out var state))
            {
                var total = QuizQuestions.Count;
                var percent = (state.Score * 100) / total;

                var resultText = $"🏆 *Викторина окончена!*\n\n" +
                                $"Правильных ответов: {state.Score} из {total}\n" +
                                $"Результат: {percent}%\n\n" +
                                $"Помните: осознанность в соцсетях — это навык, который развивается!";

                await _botClient.SendTextMessageAsync(chatId, resultText, parseMode: ParseMode.Markdown);
                _quizStates.Remove(chatId);
            }
            return;
        }

        var question = QuizQuestions[step];
        var text = $"❓ *Вопрос {step + 1}/{QuizQuestions.Count}*\n\n{question.Text}";

        var buttons = question.Options.Select((opt, idx) =>
            InlineKeyboardButton.WithCallbackData(opt, $"quiz_{step}_{idx}")
        ).ToArray();

        var keyboard = new InlineKeyboardMarkup(buttons.Select(b => new[] { b }));

        await _botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
    }

    private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (callbackQuery.Data == null || !callbackQuery.Data.StartsWith("quiz_"))
            return;

        var chatId = callbackQuery.Message.Chat.Id;
        var parts = callbackQuery.Data.Split('_');

        if (parts.Length != 3)
            return;

        var step = int.Parse(parts[1]);
        var answerIdx = int.Parse(parts[2]);

        if (!_quizStates.TryGetValue(chatId, out var state) || state.CurrentStep != step)
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Викторина не активна. Начните заново: /quiz");
            return;
        }

        var question = QuizQuestions[step];
        var isCorrect = (answerIdx == question.CorrectIndex);

        if (isCorrect)
        {
            state.Score++;
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Верно!");
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id,
                $"❌ Неверно. Правильно: {question.Options[question.CorrectIndex]}");
        }

        state.CurrentStep++;

        try
        {
            await botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
        }
        catch { }

        await SendQuizQuestion(chatId, state.CurrentStep);
    }

    private static async Task HandleFreeText(long chatId, string text)
    {
        var lowerText = text.ToLower();
        string response;

        if (lowerText.Contains("завишу") || lowerText.Contains("много сижу") ||
            lowerText.Contains("много времени") || lowerText.Contains("не могу оторваться"))
        {
            response = "😟 Похоже, вы проводите в соцсетях слишком много времени.\n\n" +
                      "Попробуйте начать с малого: поставьте лимит 15 минут в день.\n" +
                      "Хотите совет? Напишите /tip";
        }
        else if (lowerText.Contains("плохо") || lowerText.Contains("грустно") ||
                 lowerText.Contains("депрессия") || lowerText.Contains("завидую"))
        {
            response = "🧡 Соцсети показывают только лучшие моменты.\n\n" +
                      "Помните: за красивой картинкой могут стоять обычные проблемы.\n" +
                      "Попробуйте отписаться от аккаунтов, вызывающих зависть.";
        }
        else if (lowerText.Contains("полезно") || lowerText.Contains("развитие") ||
                 lowerText.Contains("учусь") || lowerText.Contains("познавательно"))
        {
            response = "📚 Да, соцсети могут быть полезными!\n\n" +
                      "Главное — контролировать ленту и выделять время только на осознанное чтение.";
        }
        else if (lowerText.Contains("нет друзей") || lowerText.Contains("одиночество") || lowerText.Contains("общение"))
        {
            response = "💬 Соцсети создают иллюзию общения.\n\n" +
                      "Настоящая связь рождается в реальных разговорах.\n" +
                      "Попробуйте позвонить старому другу или сходить на офлайн-встречу.";
        }
        else
        {
            response = "🤖 Интересная мысль!\n\n" +
                      "Если хотите узнать факт - /fact\n" +
                      "Пройти викторину - /quiz\n" +
                      "Получить совет - /tip\n\n" +
                      "Поделитесь своим опытом влияния соцсетей — я слушаю!";
        }

        await _botClient.SendTextMessageAsync(chatId, response);
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"❌ Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}

// Класс для состояния викторины
public class QuizState
{
    public int CurrentStep { get; set; }
    public int Score { get; set; }
}

// Класс для вопроса викторины
public class QuizQuestion
{
    public string Text { get; set; }
    public string[] Options { get; set; }
    public int CorrectIndex { get; set; }

    public QuizQuestion(string text, string[] options, int correctIndex)
    {
        Text = text;
        Options = options;
        CorrectIndex = correctIndex;
    }
}