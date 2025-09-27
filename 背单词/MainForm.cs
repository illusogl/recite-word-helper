using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using WordItems;

namespace 背单词
{
    class MainForm : Text
    {       
        private WordManager _wordManager;
        private NotificationManager _notificationManager;
        private System.Windows.Forms.Timer _reviewTimer;
        // 界面控件
        private Button _startButton;
        private Button _stopButton;
        private NumericUpDown _wordCountSelector;
        private Label _statusLabel;
        private Label _statsLabel;
        private Label _vocabularyInfoLabel;
        private ComboBox _intervalComboBox;
        private ComboBox _vocabularyComboBox;
        // 学习状态
        private bool _isReviewing = false;
        private int _wordsToReview;
        private int _wordsReviewed = 0;

        private Dictionary<string, int> _intervalOptions = new Dictionary<string, int>
        {
            {"立刻", 0},
            {"1分钟", 60000},
            {"3分钟", 180000}
        };
        public MainForm()
        {
            InitializeComponent();
            SetupUI();
            InitializeApplication();
        }

        private void SetupUI()
        {
            // 设置窗体属性
            this.Text = "单词背诵助手";
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            // 创建控件
            _startButton = new Button
            {
                Text = "开始背诵",
                Location = new Point(50, 30),
                Size = new Size(100, 30)
            };
            _startButton.Click += StartButton_Click;

            _stopButton = new Button
            {
                Text = "停止",
                Location = new Point(170, 30),
                Size = new Size(100, 30),
                Enabled = false
            };
            _stopButton.Click += StopButton_Click;
            _vocabularyComboBox = new ComboBox
            {
                Location = new Point(50, 120),
                Size = new Size(150, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            //_vocabularyComboBox.SelectedIndexChanged += VocabularyComboBox_SelectedIndexChanged;

            Label vocabularyLabel = new Label
            {
                Text = "选择词库:",
                Location = new Point(50, 100),
                Size = new Size(120, 20)
            };

            _vocabularyInfoLabel = new Label
            {
                Text = "词库信息加载中...",
                Location = new Point(50, 160),
                Size = new Size(250, 20),
                ForeColor = Color.Gray
            };

            _wordCountSelector = new NumericUpDown
            {
                Location = new Point(50, 80),
                Size = new Size(100, 20),
                Minimum = 1,
                Maximum = 50,
                Value = 10
            };

            Label wordCountLabel = new Label
            {
                Text = "背诵单词数量:",
                Location = new Point(50, 60),
                Size = new Size(120, 20)
            };

            _intervalComboBox = new ComboBox
            {
                Location = new Point(170, 80),
                Size = new Size(100, 20),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            // 添加间隔选项
            foreach (var option in _intervalOptions.Keys)
            {
                _intervalComboBox.Items.Add(option);
            }
            _intervalComboBox.SelectedIndex = 1; // 默认选择1分钟

            Label intervalLabel = new Label
            {
                Text = "显示间隔:",
                Location = new Point(170, 60),
                Size = new Size(120, 20)
            };

            _statusLabel = new Label
            {
                Text = "准备就绪",
                Location = new Point(50, 180),
                Size = new Size(300, 20)
            };

            _statsLabel = new Label
            {
                Text = "今日已背诵: 0 个单词",
                Location = new Point(50, 200),
                Size = new Size(300, 20)
            };

            // 添加到窗体
            this.Controls.Add(_startButton);
            this.Controls.Add(_stopButton);
            this.Controls.Add(_vocabularyComboBox);
            this.Controls.Add(vocabularyLabel);
            this.Controls.Add(_vocabularyInfoLabel);
            this.Controls.Add(_wordCountSelector);
            this.Controls.Add(_intervalComboBox);
            this.Controls.Add(intervalLabel);
            this.Controls.Add(wordCountLabel);
            this.Controls.Add(_statusLabel);
            this.Controls.Add(_statsLabel);
        }
        private void InitializeApplication()
        {
            // 创建WordManager实例（假设词库文件在应用程序目录下）
            _wordManager = new WordManager("Vocabulary.json");

            // 创建NotificationManager实例
            _notificationManager = new NotificationManager(_wordManager);

            // 设置事件处理
            _notificationManager.OnUserResponse += NotificationManager_OnUserResponse;

            // 设置词库选择框
            SetupVocabularyComboBox();

            // 设置定时器检查需要复习的单词
            SetupReviewTimer();

            // 更新统计信息
            UpdateStats();
        }
        private void SetupVocabularyComboBox()
        {
            // 清空现有项
            _vocabularyComboBox.Items.Clear();

            // 获取可用的词库并添加到下拉框
            var availableVocabularies = _wordManager.GetAvailableVocabularies();
            foreach (var vocab in availableVocabularies)
            {
                _vocabularyComboBox.Items.Add(vocab);
            }

            // 选择当前词库
            string currentVocab = _wordManager.GetCurrentVocabularyName();
            if (!string.IsNullOrEmpty(currentVocab))
            {
                _vocabularyComboBox.SelectedItem = currentVocab;
            }
            else if (_vocabularyComboBox.Items.Count > 0)
            {
                _vocabularyComboBox.SelectedIndex = 0;
            }

            // 更新词库信息
            UpdateVocabularyInfo();
        }

        private void UpdateVocabularyInfo()
        {
            int totalWords = _wordManager.GetAllWords().Count;
            int dueWords = _wordManager.GetDueReviewWords().Count;
            string currentVocab = _wordManager.GetCurrentVocabularyName();

            _vocabularyInfoLabel.Text = $"{currentVocab} | 总计: {totalWords} | 待复习: {dueWords}";
        }

        private void VocabularyComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_vocabularyComboBox.SelectedItem != null && !_isReviewing)
            {
                string selectedVocabulary = _vocabularyComboBox.SelectedItem.ToString();
                bool success = _wordManager.SwitchVocabulary(selectedVocabulary);

                if (success)
                {
                    UpdateVocabularyInfo();
                    _statusLabel.Text = $"已切换到词库: {selectedVocabulary}";
                }
                else
                {
                    _statusLabel.Text = $"切换词库失败: {selectedVocabulary}";
                }

                // 重置学习统计
                _wordsReviewed = 0;
                UpdateStats();
            }
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            // 重新加载当前词库
            _wordManager.LoadWordsFromFile();
            UpdateVocabularyInfo();
            _statusLabel.Text = "词库已刷新";
        }
        private void SetupReviewTimer()
        {
                _reviewTimer = new System.Windows.Forms.Timer();
                _reviewTimer.Interval = 60000; // 每分钟检查一次（60000毫秒）
                _reviewTimer.Tick += ReviewTimer_Tick;
        }

        private void ReviewTimer_Tick(object sender, EventArgs e)
        {
        if (_isReviewing && _wordsReviewed < _wordsToReview)
        {
            CheckForReviewWords();
        }
        else if (_wordsReviewed >= _wordsToReview)
        {
            StopReviewing();
            MessageBox.Show($"已完成 {_wordsToReview} 个单词的复习！", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        }

        // 添加事件处理方法
        private void NotificationManager_OnUserResponse(string wordSpelling, bool isKnown)
        {
            // 在UI线程上更新界面
            this.Invoke((MethodInvoker)delegate {
                _wordsReviewed++;
                UpdateStats();

                // 如果选择的是"立刻"，并且用户已经响应了通知，则立即显示下一个单词
                if (_isReviewing &&
                    _wordsReviewed < _wordsToReview &&
                    _intervalComboBox.SelectedItem?.ToString() == "立刻")
                {
                    // 短暂延迟，确保通知完全消失
                    System.Threading.Thread.Sleep(500);
                    CheckForReviewWords();
                }
            });
        }
        private void StartButton_Click(object sender, EventArgs e)
        {
            StartReviewing();
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            StopReviewing();
        }
        private void CheckForReviewWords()
        {
            // 获取需要复习的单词
            var dueWord = _wordManager.GetRandomDueWord();

            if (dueWord != null)
            {
                // 显示通知
                _notificationManager.ShowWordNotification(dueWord);
                UpdateStats();                  
            }
            else
            {
                _statusLabel.Text = "没有需要复习的单词了！";
                _reviewTimer.Stop();
                _isReviewing = false;
                UpdateButtonStates();
            }
        }

        private void StartReviewing()
        {
            if(_vocabularyComboBox.SelectedItem != null)
            {
                string selectedVocabulary = _vocabularyComboBox.SelectedItem.ToString();
                bool success = _wordManager.SwitchVocabulary(selectedVocabulary);
                if (success)
                {
                    UpdateVocabularyInfo();
                    _statusLabel.Text = $"已切换到词库: {selectedVocabulary}";
                }
                else
                {
                    _statusLabel.Text = $"切换词库失败: {selectedVocabulary}";
                }
                // 重置学习统计
                _wordsReviewed = 0;
                UpdateStats();
            }

            _wordsToReview = (int)_wordCountSelector.Value;
            _wordsReviewed = 0;
            _isReviewing = true;

            _statusLabel.Text = $"正在复习单词... ({_wordsReviewed}/{_wordsToReview})";
            // 根据用户选择的间隔设置定时器
            string selectedInterval = _intervalComboBox.SelectedItem?.ToString() ?? "1分钟";
            int intervalMs = _intervalOptions[selectedInterval];

            if (intervalMs > 0)
            {
                _reviewTimer.Interval = intervalMs;
                _reviewTimer.Start();
            }

            _statusLabel.Text = $"正在复习单词... ({_wordsReviewed}/{_wordsToReview})";

            // 无论选择什么间隔，都立即显示第一个单词
            CheckForReviewWords();

            UpdateButtonStates();
        }

    
        private void StopReviewing()
        {
            _isReviewing = false;
            _reviewTimer.Stop();
            _statusLabel.Text = "已停止复习";

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            _startButton.Enabled = !_isReviewing;
            _stopButton.Enabled = _isReviewing;
            _wordCountSelector.Enabled = !_isReviewing;
            _intervalComboBox.Enabled = !_isReviewing;
            _vocabularyComboBox.Enabled = !_isReviewing; // 学习时禁用词库切换
        }

        private void ExitApplication(object sender, EventArgs e)
        {
            _reviewTimer.Stop();
            Application.Exit();
        }
        private void UpdateStats()
        {
            _statusLabel.Text = _isReviewing ?
                $"正在复习单词... ({_wordsReviewed}/{_wordsToReview})" :
                "准备就绪";

            _statsLabel.Text = $"今日已背诵: {_wordsReviewed} 个单词";
        }
        // 可选：重写OnLoad方法以启动时最小化
        //protected override void OnLoad(EventArgs e)
        //    {
        //        base.OnLoad(e);
        //        // 启动时最小化到托盘
        //        this.WindowState = FormWindowState.Minimized;
        //        this.Hide();
        //    }

    }
}

