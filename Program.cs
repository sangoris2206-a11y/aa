using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

const string BOT_TOKEN = "8980163341:AAGQx-dVyGbS6maLNQjR7bomSdyM0oJtiMk";
var botClient = new TelegramBotClient(BOT_TOKEN);

// ========== ВАШИ ДАННЫЕ ==========
var _quizStates = new Dictionary<long, QuizState>();
var _random = new Random();

var Facts = new List<string>
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

var Tips = new List<string>
{
    "⏲️ Ставьте таймер на 30 минут для соцсетей в день.",
    "🔕 Отключите push-уведомления – они крадут внимание.",
    "📵 Не пользуйтесь телефоном за час до сна.",
    "👀 Следите не за 'идеальными' блогерами, а за вдохновляющими профилями.",
    "🧘 Делайте цифровой выходной раз в неделю.",
    "✍️ Ведите дневник реальных встреч с друзьями.",
    "🌿 Перед заходом в соцсеть спросите себя: 'Зачем я это делаю?'"
};

var QuizQuestions = new List<QuizQuestion>
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

// ========== ГЛАВНЫЙ ЭНДПОИНТ (ЭТО САМОЕ ВАЖНОЕ) ==========
app.MapPost("/webhook", async (HttpContext context) =>
{
    Console.WriteLine("➡️ Получен запрос на /webhook"); // Лог для проверки
    
    var update = await context.Request.ReadFromJsonAsync<Update>();
    if (update == null) 
    {
        Console.WriteLine("⚠️ Пустой update");
        return Results.BadRequest();
    }

    Console.WriteLine($"✅ Получен update типа: {(update.Message != null ? "Message" : "Callback")}");

    try
    {
        if (update.CallbackQuery != null)
            await HandleCallbackQuery(botClient, update.CallbackQuery);
        else if (update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
            await HandleMessage(botClient, update.Message);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Ошибка: {ex.Message}");
    }

    return Results.Ok();
});

// Проверочный эндпоинт
app.MapGet("/", () => "Bot is running!");

// ========== УСТАНОВКА WEBHOOK ==========
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var domain = Environment.GetEnvironmentVariable("RAILWAY_PUBLIC_DOMAIN") ?? "bot.up.railway.app";
var webhookUrl = $"https://{domain}/webhook";

Console.WriteLine($"🌐 Устанавливаю webhook: {webhookUrl}");

try
{
    await botClient.SetWebhookAsync(webhookUrl);
    Console.WriteLine("✅ Webhook успешно установлен!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Ошибка установки webhook: {ex.Message}");
}

// ========== ЗАПУСК СЕРВЕРА ==========
app.Run($"http://*:{port}");

// ========== ВСЕ ВАШИ ФУНКЦИИ ОБРАБОТКИ (БЕЗ ИЗМЕНЕНИЙ) ==========
async Task HandleMessage(ITelegramBotClient client, Message message)
{
    var chatId = message.Chat.Id;
    var text = message.Text;

    if (string.IsNullOrEmpty(text)) return;

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
                await client.SendTextMessageAsync(chatId, "❓ Неизвестная команда. Используйте /start");
                break;
        }
        return;
    }

    await HandleFreeText(chatId, text);
}

async Task SendStartMessage(long chatId)
{
    var text = "🧠 *Привет! Я бот об осознанном использовании социальных сетей.*\n\n" +
               "Они влияют на настроение, продуктивность и отношения. Давай разберёмся!\n\n" +
               "📌 *Что я умею:*\n" +
               "🔹 /fact – случайный факт о влиянии соцсетей\n" +
               "🔹 /quiz – викторина\n" +
               "🔹 /tip – полезный совет для цифрового баланса\n\n" +
               "Начни с /fact или /quiz!";

    await botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown);
}

async Task SendRandomFact(long chatId)
{
    var fact = Facts[_random.Next(Facts.Count)];
    await botClient.SendTextMessageAsync(chatId, $"📖 *Знаете ли вы?*\n{fact}", parseMode: ParseMode.Markdown);
}

async Task SendRandomTip(long chatId)
{
    var tip = Tips[_random.Next(Tips.Count)];
    await botClient.SendTextMessageAsync(chatId, $"💡 *Совет дня:*\n{tip}", parseMode: ParseMode.Markdown);
}

async Task StartQuiz(long chatId)
{
    var state = new QuizState { CurrentStep = 0, Score = 0 };
    _quizStates[chatId] = state;
    await SendQuizQuestion(chatId, 0);
}

async Task SendQuizQuestion(long chatId, int step)
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

            await botClient.SendTextMessageAsync(chatId, resultText, parseMode: ParseMode.Markdown);
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

    await botClient.SendTextMessageAsync(chatId, text, parseMode: ParseMode.Markdown, replyMarkup: keyboard);
}

async Task HandleCallbackQuery(ITelegramBotClient client, CallbackQuery callbackQuery)
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
        await client.AnswerCallbackQueryAsync(callbackQuery.Id, "❌ Викторина не активна. Начните заново: /quiz");
        return;
    }

    var question = QuizQuestions[step];
    var isCorrect = (answerIdx == question.CorrectIndex);

    if (isCorrect)
    {
        state.Score++;
        await client.AnswerCallbackQueryAsync(callbackQuery.Id, "✅ Верно!");
    }
    else
    {
        await client.AnswerCallbackQueryAsync(callbackQuery.Id,
            $"❌ Неверно. Правильно: {question.Options[question.CorrectIndex]}");
    }

    state.CurrentStep++;

    try
    {
        await client.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
    }
    catch { }

    await SendQuizQuestion(chatId, state.CurrentStep);
}

async Task HandleFreeText(long chatId, string text)
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

    await botClient.SendTextMessageAsync(chatId, response);
}

// ========== КЛАССЫ ==========
public class QuizState
{
    public int CurrentStep { get; set; }
    public int Score { get; set; }
}

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
