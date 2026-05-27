import asyncio
import random
import logging
from typing import Dict, Optional
from dataclasses import dataclass
from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup
from telegram.ext import (
    Application,
    CommandHandler,
    MessageHandler,
    CallbackQueryHandler,
    filters,
    ContextTypes
)

# Настройка логирования
logging.basicConfig(
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    level=logging.INFO
)
logger = logging.getLogger(__name__)

# ЗАМЕНИ НА СВОЙ ТОКЕН ОТ @BotFather
BOT_TOKEN = "8980163341:AAGQx-dVyGbS6maLNQjR7bomSdyM0oJtiMk"

# Состояния викторины
quiz_states: Dict[int, 'QuizState'] = {}

# Факты о соцсетях
FACTS = [
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
]

# Советы
TIPS = [
    "⏲️ Ставьте таймер на 30 минут для соцсетей в день.",
    "🔕 Отключите push-уведомления – они крадут внимание.",
    "📵 Не пользуйтесь телефоном за час до сна.",
    "👀 Следите не за 'идеальными' блогерами, а за вдохновляющими профилями.",
    "🧘 Делайте цифровой выходной раз в неделю.",
    "✍️ Ведите дневник реальных встреч с друзьями.",
    "🌿 Перед заходом в соцсеть спросите себя: 'Зачем я это делаю?'"
]

# Вопросы викторины
class QuizQuestion:
    def __init__(self, text: str, options: list, correct_index: int):
        self.text = text
        self.options = options
        self.correct_index = correct_index

QUIZ_QUESTIONS = [
    QuizQuestion(
        "Сколько в среднем времени в день люди тратят на соцсети?",
        ["30 минут", "1-1.5 часа", "2-3 часа", "Более 5 часов"],
        2
    ),
    QuizQuestion(
        "Что такое 'FOMO' в контексте соцсетей?",
        ["Новый вид мемов", "Страх пропустить что-то интересное", "Техника фотофильтров", "Алгоритм TikTok"],
        1
    ),
    QuizQuestion(
        "Что помогает снизить негативное влияние соцсетей?",
        ["Ещё больше времени в сети", "Цифровой детокс", "Отключение всех уведомлений навсегда", "Просмотр stories всю ночь"],
        1
    ),
    QuizQuestion(
        "Как соцсети влияют на сон?",
        ["Улучшают его", "Никак", "Синий свет экранов мешает выработке мелатонина", "Помогают быстрее заснуть"],
        2
    )
]

@dataclass
class QuizState:
    current_step: int = 0
    score: int = 0


async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработчик команды /start"""
    text = (
        "🧠 *Привет! Я бот об осознанном использовании социальных сетей.*\n\n"
        "Они влияют на настроение, продуктивность и отношения. Давай разберёмся!\n\n"
        "📌 *Что я умею:*\n"
        "🔹 /fact – случайный факт о влиянии соцсетей\n"
        "🔹 /quiz – викторина\n"
        "🔹 /tip – полезный совет для цифрового баланса\n\n"
        "Начни с /fact или /quiz!"
    )
    await update.message.reply_text(text, parse_mode='Markdown')


async def random_fact(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Отправляет случайный факт"""
    fact = random.choice(FACTS)
    await update.message.reply_text(f"📖 *Знаете ли вы?*\n{fact}", parse_mode='Markdown')


async def random_tip(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Отправляет случайный совет"""
    tip = random.choice(TIPS)
    await update.message.reply_text(f"💡 *Совет дня:*\n{tip}", parse_mode='Markdown')


async def start_quiz(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Начинает викторину"""
    chat_id = update.effective_chat.id
    quiz_states[chat_id] = QuizState()
    await send_quiz_question(update, chat_id, 0)


async def send_quiz_question(update: Update, chat_id: int, step: int):
    """Отправляет вопрос викторины"""
    if step >= len(QUIZ_QUESTIONS):
        if chat_id in quiz_states:
            state = quiz_states[chat_id]
            total = len(QUIZ_QUESTIONS)
            percent = (state.score * 100) // total
            
            result_text = (
                f"🏆 *Викторина окончена!*\n\n"
                f"Правильных ответов: {state.score} из {total}\n"
                f"Результат: {percent}%\n\n"
                f"Помните: осознанность в соцсетях — это навык, который развивается!"
            )
            await update.callback_query.edit_message_text(result_text, parse_mode='Markdown')
            del quiz_states[chat_id]
        return
    
    question = QUIZ_QUESTIONS[step]
    text = f"❓ *Вопрос {step + 1}/{len(QUIZ_QUESTIONS)}*\n\n{question.text}"
    
    keyboard = []
    for idx, option in enumerate(question.options):
        keyboard.append([InlineKeyboardButton(option, callback_data=f"quiz_{step}_{idx}")])
    
    reply_markup = InlineKeyboardMarkup(keyboard)
    
    # Если это первый вопрос (step=0) - отправляем новое сообщение
    if step == 0:
        await update.message.reply_text(text, parse_mode='Markdown', reply_markup=reply_markup)
    else:
        await update.callback_query.edit_message_text(text, parse_mode='Markdown', reply_markup=reply_markup)


async def handle_quiz_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обрабатывает ответы на вопросы викторины"""
    query = update.callback_query
    await query.answer()
    
    if not query.data or not query.data.startswith("quiz_"):
        return
    
    parts = query.data.split('_')
    if len(parts) != 3:
        return
    
    step = int(parts[1])
    answer_idx = int(parts[2])
    chat_id = update.effective_chat.id
    
    if chat_id not in quiz_states:
        await query.edit_message_text("❌ Викторина не активна. Начните заново: /quiz")
        return
    
    state = quiz_states[chat_id]
    if state.current_step != step:
        await query.edit_message_text("❌ Викторина не активна. Начните заново: /quiz")
        return
    
    question = QUIZ_QUESTIONS[step]
    is_correct = (answer_idx == question.correct_index)
    
    if is_correct:
        state.score += 1
        await query.answer("✅ Верно!", show_alert=False)
    else:
        correct_answer = question.options[question.correct_index]
        await query.answer(f"❌ Неверно. Правильно: {correct_answer}", show_alert=False)
    
    state.current_step += 1
    await send_quiz_question(update, chat_id, state.current_step)


async def handle_free_text(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обрабатывает свободный текст от пользователя"""
    text = update.message.text.lower()
    chat_id = update.effective_chat.id
    
    if any(word in text for word in ["завишу", "много сижу", "много времени", "не могу оторваться"]):
        response = (
            "😟 Похоже, вы проводите в соцсетях слишком много времени.\n\n"
            "Попробуйте начать с малого: поставьте лимит 15 минут в день.\n"
            "Хотите совет? Напишите /tip"
        )
    elif any(word in text for word in ["плохо", "грустно", "депрессия", "завидую"]):
        response = (
            "🧡 Соцсети показывают только лучшие моменты.\n\n"
            "Помните: за красивой картинкой могут стоять обычные проблемы.\n"
            "Попробуйте отписаться от аккаунтов, вызывающих зависть."
        )
    elif any(word in text for word in ["полезно", "развитие", "учусь", "познавательно"]):
        response = (
            "📚 Да, соцсети могут быть полезными!\n\n"
            "Главное — контролировать ленту и выделять время только на осознанное чтение."
        )
    elif any(word in text for word in ["нет друзей", "одиночество", "общение"]):
        response = (
            "💬 Соцсети создают иллюзию общения.\n\n"
            "Настоящая связь рождается в реальных разговорах.\n"
            "Попробуйте позвонить старому другу или сходить на офлайн-встречу."
        )
    else:
        response = (
            "🤖 Интересная мысль!\n\n"
            "Если хотите узнать факт - /fact\n"
            "Пройти викторину - /quiz\n"
            "Получить совет - /tip\n\n"
            "Поделитесь своим опытом влияния соцсетей — я слушаю!"
        )
    
    await update.message.reply_text(response)


async def error_handler(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Обработчик ошибок"""
    logger.error(f"Ошибка: {context.error}")


async def main():
    """Главная функция запуска бота"""
    print("╔════════════════════════════════════════╗")
    print("║     Telegram Bot - HAPP VPN Support    ║")
    print("╚════════════════════════════════════════╝")
    print()
    print("🚀 Запуск бота...")
    
    # Создаём приложение
    application = Application.builder().token(BOT_TOKEN).build()
    
    # Добавляем обработчики команд
    application.add_handler(CommandHandler("start", start))
    application.add_handler(CommandHandler("fact", random_fact))
    application.add_handler(CommandHandler("tip", random_tip))
    application.add_handler(CommandHandler("quiz", start_quiz))
    
    # Добавляем обработчик callback-запросов (кнопки викторины)
    application.add_handler(CallbackQueryHandler(handle_quiz_callback, pattern="^quiz_"))
    
    # Добавляем обработчик свободного текста
    application.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, handle_free_text))
    
    # Добавляем обработчик ошибок
    application.add_error_handler(error_handler)
    
    # Запускаем бота
    print("📡 Подключение к Telegram API...")
    
    try:
        me = await application.bot.get_me()
        print()
        print(f"✅ УСПЕХ! Бот @{me.username} успешно запущен!")
        print(f"   ID бота: {me.id}")
        print()
        print("🤖 Бот работает!")
        print("📝 Команды:")
        print("   /start - приветствие")
        print("   /fact - случайный факт о соцсетях")
        print("   /quiz - викторина")
        print("   /tip - полезный совет")
        print()
        print("⏹️ Нажмите Ctrl+C для выхода...")
        print()
        
        # Запускаем поллинг
        await application.run_polling()
        
    except Exception as e:
        print()
        print("❌ ОШИБКА ПОДКЛЮЧЕНИЯ")
        print()
        print(f"Детали ошибки: {e}")
        print()
        print("🔧 Возможные решения:")
        print("1. Проверьте токен бота")
        print("2. Проверьте интернет-соединение")
        print("3. Если используете VPN - проверьте настройки")
        print()
        print("Нажмите Enter для выхода...")


if __name__ == "__main__":
    asyncio.run(main())
