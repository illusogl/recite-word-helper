using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordItems
{
    class WordItem
    {
        public string Spelling { get; set; }
        public string Definition { get; set; }
        public byte[]? AudioData { get; set; } // 音频数据，可能为空
        public DateTime NextReviewTime { get; set; }
        public int MemoryLevel { get; set; }
        public WordItem() 
        {

        }
        public WordItem(string spelling, string meaning, byte[]? audioData = null)
        {
            Spelling = spelling;
            Definition = meaning;
            AudioData = audioData;
            NextReviewTime = DateTime.Now;
            MemoryLevel = 0;
        }

        //更新记忆水平和下次复习时间，需要后期重写
        public void UpdateReview(bool remembered)
        {
            if (remembered)
            {
                MemoryLevel++;
            }
            else
            {
                MemoryLevel = Math.Max(0, MemoryLevel - 1);
            }
            // 根据记忆水平调整下次复习时间
            if (MemoryLevel <= 5)
                NextReviewTime = DateTime.Now.AddDays(Math.Pow(2, MemoryLevel));
            else
                NextReviewTime = DateTime.Now.AddDays(10000);
        }
    }
}
