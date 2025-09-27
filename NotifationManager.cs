using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WordItems;

namespace 背单词
{
    class NotificationManager
    {
        private WordManager wordManager;
        public event Action<string, bool> OnUserResponse; // 新增事件

        public NotificationManager(WordManager wordManager)
        {
            this.wordManager = wordManager;

            // 订阅Toast通知激活事件
            ToastNotificationManagerCompat.OnActivated += ToastNotificationManagerCompat_OnActivated;
        }
        public void ShowWordNotification(WordItem word)
        {
            if (word == null)
                return;

            // 创建Toast通知内容
            new ToastContentBuilder()
                .AddArgument("action", "viewWord") // 添加参数，用于识别通知类型
                .AddArgument("word", word.Spelling) // 添加单词参数，用于后续处理
                .AddText($" 单词: {word.Spelling}")
                .AddText($" 释义: {word.Definition}")

                // 添加按钮供用户选择熟悉程度
                .AddButton(new ToastButton()
                    .SetContent("认识")
                    .AddArgument("action", "remember")
                    .AddArgument("word", word.Spelling)
                    .SetBackgroundActivation())

                .AddButton(new ToastButton()
                    .SetContent("模糊")
                    .AddArgument("action", "vague")
                    .AddArgument("word", word.Spelling)
                    .SetBackgroundActivation())

                .AddButton(new ToastButton()
                    .SetContent("忘记")
                    .AddArgument("action", "forget")
                    .AddArgument("word", word.Spelling)
                    .SetBackgroundActivation())

                // 显示通知
                .Show();
        }
        private void ToastNotificationManagerCompat_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            // 解析参数
            var args = ToastArguments.Parse(e.Argument);
            string action = args["action"];
            string wordSpelling = args["word"];
            bool isKnown = false;
            // 根据动作类型处理
            switch (action)
            {
                case "remember":
                    isKnown = true;
                    HandleUserResponse(wordSpelling, true);
                    break;
                case "vague":
                    isKnown = false;
                    HandleUserResponse(wordSpelling, false); // 或者可以有不同的处理
                    break;
                case "forget":
                    isKnown = false;
                    HandleUserResponse(wordSpelling, false);
                    break;
            }
            // 触发事件，通知主窗体
            OnUserResponse?.Invoke(wordSpelling, isKnown);
        }
        private void HandleUserResponse(string wordSpelling, bool isKnown)
        {
            // 从WordManager中查找对应的单词
            var word = wordManager.GetAllWords().FirstOrDefault(w => w.Spelling == wordSpelling);
            if (word != null)
            {
                // 更新单词的记忆等级
                word.UpdateReview(isKnown);

                // 保存更改到文件
                wordManager.SaveWordsToFile();

                Console.WriteLine($"已更新单词 '{wordSpelling}' 的状态: {(isKnown ? "认识" : "需要复习")}");
            }
        }
    }
}
