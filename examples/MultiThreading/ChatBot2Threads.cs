using System;
using System.Threading;
using System.Collections.Generic;

namespace examples.MultiThreading
{
    public class ChatBotGraphic
    {
        private static readonly ChatBotGraphic INSTANCE = new ChatBotGraphic();

        private ChatBotGraphic()
        {
        }

        public static ChatBotGraphic GetInstance()
        {
            return INSTANCE;
        }

        public void Render(string value)
        {
            var currentThreadName = Thread.CurrentThread.Name;
            if (currentThreadName != EventLoop.MAIN_THREAD_NAME)
            {
                throw new Exception("can not render in background thread");
            }
            Console.WriteLine(value);
        }
    }

    public class ChatBot
    {
        public static void Main(string[] args)
        {
            var eventLoop = new EventLoop();
            eventLoop.OnStoped(() =>
            {
                Environment.Exit(0);
            });

            var inputThread = NewInputThread();
            inputThread.Start();

            eventLoop.Start();
        }

        private static Thread NewInputThread()
        {
            var thread = new Thread(() =>
            {
                while (true)
                {
                    //ChatBotGraphic.GetInstance().Render("Please input your question:");
                    NonBlockingQueue.GetInstance().AddTask(() =>
                    {
                        ChatBotGraphic.GetInstance().Render("Please input your question:");
                    });
                    var question = Console.ReadLine();
                    Bot.GetInstance().Answer(question);
                };

            })
            {
                Name = "input"
            };
            return thread;
        }
    }

    public class Bot
    {
        private readonly IDictionary<string, string> QAs = new Dictionary<string, string>() {
            {"hi", "Hello"},
            {"how are you?", "I'm fine, thank you, how about you?"},
            {"i'm fine", "Look good, how do you feel?"},
            {"bad", "Do you want to drink some think?"},
            {"yes, drink", "Beer or tear?"},
            {"quit", "Bye"}
        };

        private static readonly Bot INSTANCE = new Bot();

        private Bot()
        {
        }

        public static Bot GetInstance()
        {
            return INSTANCE;
        }

        public void Answer(string question)
        {
            string answer = null;
            var questionLowercase = question.ToLower();
            if (QAs.ContainsKey(questionLowercase))
            {
                answer = QAs[questionLowercase];
            }
            if(answer == null)
            {
                foreach(string q in QAs.Keys)
                {
                    if(q.Contains(questionLowercase))
                    {
                        answer = QAs[q];
                    }
                }
            }
            if(answer == null)
            {
                answer = "Sorry, I don't know, can you ask me something else?";
            }

            //ChatBotGraphic.GetInstance().Render(answer);
            NonBlockingQueue.GetInstance().AddTask(() =>
            {
                ChatBotGraphic.GetInstance().Render(answer);
            });
            if (questionLowercase == "quit")
            {
                NonBlockingQueue.GetInstance().AddTask(EventLoop.POISON);
            }
        }
    }
}
