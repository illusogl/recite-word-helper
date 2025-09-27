using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordItems
{
    class WordManager
    {
        public List<WordItem> needWords;
        private string filePath;
        private Dictionary<string, string> _vocabularyFiles;
        public WordManager(string filePath)
        {
            needWords = new List<WordItem>();
            _vocabularyFiles = new Dictionary<string, string>
            {
                {"四级词汇", "cet4.json"},
                {"六级词汇", "cet6.json"},
                {"考研词汇", "kaoyan.json"}
            };
            // 默认加载第一个词库
            if (_vocabularyFiles.Count > 0)
            {
                var firstVocab = _vocabularyFiles.First();
                SwitchVocabulary(firstVocab.Key);
            }
        }
        /// <summary>
        /// 切换到指定的词库
        /// </summary>
        /// <param name="vocabularyName">词库名称</param>
        /// <returns>是否成功切换</returns>
        public bool SwitchVocabulary(string vocabularyName)
        {
            if (_vocabularyFiles.ContainsKey(vocabularyName))
            {
                filePath = _vocabularyFiles[vocabularyName];
                LoadWordsFromFile();
                return true;
            }
            return false;
        }
        /// <summary>
        /// 获取所有可用的词库名称
        /// </summary>
        /// <returns>词库名称列表</returns>
        public List<string> GetAvailableVocabularies()
        {
            return _vocabularyFiles.Keys.ToList();
        }
        /// <summary>
        /// 获取当前使用的词库名称
        /// </summary>
        /// <returns>当前词库名称</returns>
        public string GetCurrentVocabularyName()
        {
            return _vocabularyFiles.FirstOrDefault(x => x.Value == filePath).Key;
        }
        public void LoadWordsFromFile()
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("文件不存在: " + filePath);
                needWords = new List<WordItem>();
                return;
            }
            try
            {
                string json = File.ReadAllText(filePath);

                // 使用JsonConvert将JSON字符串反序列化为List<WordItem>对象
                // 这是Newtonsoft.Json库最核心、最常用的方法之一
                needWords = JsonConvert.DeserializeObject<List<WordItem>>(json);

                Console.WriteLine($"Successfully loaded {needWords?.Count ?? 0} words from {filePath}.");
            }
            catch (Exception ex)
            {
                // 如果过程中出现任何错误（如文件格式错误、权限问题等），捕获异常
                Console.WriteLine($"Error loading words from file: {ex.Message}");
                // 为了避免程序崩溃，我们初始化一个空列表
                needWords = new List<WordItem>();
            }
        }
        public void SaveWordsToFile()
        {
            try
            {
                // 使用JsonConvert将List<WordItem>对象序列化为格式化的JSON字符串
                // Formatting.Indented 参数让生成的JSON字符串有缩进，更易于阅读
                string json = JsonConvert.SerializeObject(needWords, Formatting.Indented);

                // 将JSON字符串写入文件，会覆盖原有文件内容
                File.WriteAllText(filePath, json);

                Console.WriteLine($"Successfully saved {needWords.Count} words to {filePath}.");
            }
            catch (Exception ex)
            {
                // 处理可能出现的错误，如路径无效、磁盘已满等
                Console.WriteLine($"Error saving words to file: {ex.Message}");
            }
        }
        /// <summary>
        /// 获取所有到期需要复习的单词
        /// </summary>
        /// <returns>需要复习的单词列表</returns>
        public List<WordItem> GetDueReviewWords()
        {
            // 使用LINQ查询语法，从_allWords中筛选出NextReviewTime小于或等于当前时间的单词
            // ToList() 将结果转换为一个新的List
            return needWords.Where(word => word.NextReviewTime <= DateTime.Now).ToList();
        }

        /// <summary>
        /// 从到期复习的单词中随机获取一个
        /// </summary>
        /// <returns>一个随机选中的到期单词,优先选复习等级高的，如果没有则返回null</returns>
        public WordItem GetRandomDueWord()
        {
            // 获取所有到期的单词
            var dueWords = GetDueReviewWords();

            if (dueWords == null || dueWords.Count == 0)
            {                
                return null;
            }
            // 按记忆等级降序排序，优先选择记忆等级高的单词
            var sortedWords = dueWords.OrderByDescending(word => word.MemoryLevel).ToList();

            // 如果最高记忆等级的单词有多个，从中随机选择一个
            int maxLevel = sortedWords[0].MemoryLevel;
            var highestLevelWords = sortedWords.Where(word => word.MemoryLevel == maxLevel).ToList();

            Random random = new Random();
            int randomIndex = random.Next(0, highestLevelWords.Count);
            return highestLevelWords[randomIndex];
        }
        public List<WordItem> GetAllWords()
        {
            // 返回_allWords的副本，避免外部代码直接修改内部列表
            return new List<WordItem>(needWords);
        }

        // (可选) 添加新单词
        //public void AddWord(WordItem newWord)
        //{
        //    needWords.Add(newWord);
        //    // 添加后可以选择自动保存
        //    SaveWordsToFile();
        //}
    }
}
