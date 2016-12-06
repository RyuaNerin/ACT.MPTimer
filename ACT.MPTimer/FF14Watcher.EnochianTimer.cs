namespace ACT.MPTimer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using ACT.MPTimer.Properties;
    using ACT.MPTimer.Utility;
    using Advanced_Combat_Tracker;

    /// <summary>
    /// FF14を監視する エノキアンタイマ
    /// </summary>
    public partial class FF14Watcher
    {
        /// <summary>
        /// エノキアンの効果期間
        /// </summary>
        public const double EnochianDuration = 30.0d;

        /// <summary>
        /// エノキアンの延長時の効果期間の劣化量
        /// </summary>
        public const double EnochianDegradationSecondsExtending = 5.0d;

        /// <summary>
        /// エノキアンOFF後にエノキアンの更新を受付ける猶予期間（ms）
        /// </summary>
        public const int GraceToUpdateEnochian = 1700;

        /// <summary>
        /// エノキアン効果中か？
        /// </summary>
        private bool inEnochian;

        /// <summary>
        /// アンブラルアイス中か？
        /// </summary>
        private bool inUmbralIce;

        /// <summary>
        /// ログキュー
        /// </summary>
        private Queue<string> logQueue = new Queue<string>();

        /// <summary>
        /// エノキアンタイマータスク
        /// </summary>
        private Task enochianTimerTask;

        /// <summary>
        /// エノキアンタイマー停止フラグ
        /// </summary>
        private bool enochianTimerStop;

        private string _playerName;
        /// <summary>
        /// プレイヤーの名前
        /// </summary>
        private string playerName
        {
            get
            {
                return _playerName;
            }
            set
            {
                this._playerName = value;

                int i = 0;
                this.machingTextToEnochianOn = new string[EnochianOn.Length];
                for (i = 0; i < EnochianOn.Length; ++i)
                    this.machingTextToEnochianOn[i] = string.Format(EnochianOn[i], value);

                this.machingTextToEnochianOff = new string[EnochianOff.Length];
                for (i = 0; i < EnochianOff.Length; ++i)
                    this.machingTextToEnochianOff[i] = string.Format(EnochianOff[i], value);

                this.machingTextToUmbralIceOn = new string[UmbralIceOn.Length];
                for (i = 0; i < UmbralIceOn.Length; ++i)
                    this.machingTextToUmbralIceOn[i] = string.Format(UmbralIceOn[i], value);

                this.machingTextToUmbralIceOff = new string[UmbralIceOff.Length];
                for (i = 0; i < UmbralIceOff.Length; ++i)
                    this.machingTextToUmbralIceOff[i] = string.Format(UmbralIceOff[i], value);
            }
        }

        /// <summary>
        /// エノキアンの更新猶予期間
        /// </summary>
        private bool inGraceToUpdate;

        /// <summary>
        /// 猶予期間中に更新されたか？
        /// </summary>
        private bool updatedDuringGrace;

        /// <summary>
        /// 最後のエノキアンの残り時間イベント
        /// </summary>
        private string lastRemainingTimeOfEnochian;

        /// <summary>
        /// エノキアンタイマーを開始する
        /// </summary>
        private void StartEnochianTimer()
        {
            ActGlobals.oFormActMain.OnLogLineRead += this.OnLoglineRead;
            this.playerName = string.Empty;
            this.lastRemainingTimeOfEnochian = string.Empty;
            this.logQueue.Clear();
            this.enochianTimerStop = false;
            this.inGraceToUpdate = false;
            this.updatedDuringGrace = false;
            this.enochianTimerTask = TaskUtil.StartSTATask(this.AnalyseLogLinesToEnochian);
        }

        /// <summary>
        /// エノキアンタイマーを終了する
        /// </summary>
        private void EndEnochianTimer()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= this.OnLoglineRead;

            if (this.enochianTimerTask != null)
            {
                this.enochianTimerStop = true;
                this.enochianTimerTask.Wait();
                this.enochianTimerTask.Dispose();
                this.enochianTimerTask = null;
            }
        }

        /// <summary>
        ///  Logline Read
        /// </summary>
        /// <param name="isImport">インポートログか？</param>
        /// <param name="logInfo">発生したログ情報</param>
        private void OnLoglineRead(
            bool isImport,
            LogLineEventArgs logInfo)
        {
            if (isImport)
            {
                return;
            }

            // エノキアンタイマーが有効ならば・・・
            if (Settings.Default.EnabledEnochianTimer &&
                this.EnabledByJobFilter)
            {
                // ログをキューに貯める
                lock (this.logQueue)
                {
                    this.logQueue.Enqueue(logInfo.logLine);
                }
            }
        }

        /// <summary>
        /// エノキアンタイマー向けにログを分析する
        /// </summary>
        private void AnalyseLogLinesToEnochian()
        {
            while (true)
            {
                try
                {
                    if (this.enochianTimerStop)
                    {
                        break;
                    }

                    // エノキアンタイマーViewModelを参照する
                    var vm = EnochianTimerWindow.Default.ViewModel;

                    // プレイヤー名を保存する
                    if (this.LastPlayerInfo != null)
                    {
                        if (this.playerName != this.LastPlayerInfo.Name)
                        {
                            this.playerName = this.LastPlayerInfo.Name;
                            Trace.WriteLine("Player name is " + this.playerName);
                        }
                    }

                    // エノキアンタイマーが無効？
                    if (!Settings.Default.EnabledEnochianTimer ||
                        !this.EnabledByJobFilter)
                    {
                        vm.Visible = false;
                        Thread.Sleep(Settings.Default.ParameterRefreshRate);
                        continue;
                    }

                    // ログを解析する
                    if (!string.IsNullOrWhiteSpace(this.playerName))
                    {
                        var log = string.Empty;

                        while (true)
                        {
                            lock (this.logQueue)
                            {
                                if (this.logQueue.Count > 0)
                                {
                                    log = this.logQueue.Dequeue();
                                }
                                else
                                {
                                    break;
                                }
                            }

                            this.AnalyzeLogLineToEnochian(log);
                            Thread.Sleep(1);
                        }
                    }

                    // エノキアンの残り秒数をログとして発生させる
                    /*
                    if (vm.EndScheduledDateTime >= DateTime.MinValue)
                    {
                        var remainSeconds = (vm.EndScheduledDateTime - DateTime.Now).TotalSeconds;
                        if (remainSeconds >= 0)
                        {
                            var notice = "Remaining time of Enochian. " + remainSeconds.ToString("N0");
                            if (this.lastRemainingTimeOfEnochian != notice)
                            {
                                this.lastRemainingTimeOfEnochian = notice;
                                ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, notice);
                            }
                        }
                    }
                    */

                    Thread.Sleep(Settings.Default.ParameterRefreshRate / 2);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        "Enochian Timer Error!" + Environment.NewLine +
                        ex.ToString());

                    Thread.Sleep(5 * 1000);
                }
            }
        }
        
        private static readonly string[] EnochianOn =
        {
            "{0} gains the effect of 천사의 언어 from {0} for ([0-9\\.]+) Seconds",
            "{0} gains the effect of Enochian from {0} for ([0-9\\.]+) Seconds"
        };

        private static readonly string[] EnochianOff =
        {
            "{0} loses the effect of 천사의 언어",
            "{0} loses the effect of Enochian"
        };
        private static readonly string[] UmbralIceOn =
        {
            "{0} gains the effect of 저승의 냉기",
            "{0} gains the effect of Umbral Ice",
        };
        private static readonly string[] UmbralIceOff =
        {
            "{0} loses the effect of 저승의 냉기",
            "{0} loses the effect of Umbral Ice",
        };

        private string[] machingTextToEnochianOn;
        private string[] machingTextToEnochianOff;
        private string[] machingTextToUmbralIceOn;
        private string[] machingTextToUmbralIceOff;

        /// <summary>
        /// エノキアンタイマー向けにログを分析する
        /// </summary>
        /// <param name="log">ログ</param>
        private void AnalyzeLogLineToEnochian(
            string log)
        {
            if (string.IsNullOrWhiteSpace(log))
            {
                return;
            }

            if (log.Contains("Welcome to") ||
                log.Contains("Willkommen auf"))
            {
                // プレイヤ情報を取得する
                var player = FF14PluginHelper.GetCombatantPlayer();
                if (player != null)
                {
                    this.playerName = player.Name;
                    Trace.WriteLine("Player name is " + this.playerName);
                }
            }

            if (string.IsNullOrWhiteSpace(this.playerName))
            {
                Trace.WriteLine("Player name is empty.");
                return;
            }

            // エノキアンON？
            Match m;
            foreach (var text in machingTextToEnochianOn)
            {
                m = Regex.Match(log, text);
                if (m.Success)
                {
                    this.inEnochian = true;
                    this.UpdateEnochian(double.Parse(m.Groups[1].Value), log);
                    this.lastRemainingTimeOfEnochian = string.Empty;

                    Trace.WriteLine("Enochian On. -> " + log);
                    return;
                }
            }

            // エノキアンOFF？
            foreach (var text in machingTextToEnochianOff)
            {
                if (log.Contains(text))
                {
                    // エノキアンの更新猶予期間をセットする
                    this.inGraceToUpdate = true;
                    this.updatedDuringGrace = false;

                    Task.Run(() =>
                    {
                        Thread.Sleep(GraceToUpdateEnochian + Settings.Default.ParameterRefreshRate);

                        // 更新猶予期間中？
                        if (this.inGraceToUpdate)
                        {
                            // 期間中に更新されていない？
                            if (!this.updatedDuringGrace)
                            {
                                this.inEnochian = false;
                                Trace.WriteLine("Enochian Off. -> " + log);
                            }

                            this.inGraceToUpdate = false;
                            return;
                        }

                        this.inEnochian = false;
                        Trace.WriteLine("Enochian Off. -> " + log);
                    });

                    return;
                }
            }

            // アンブラルアイスON？
            foreach (var text in machingTextToUmbralIceOn)
            {
                if (log.Contains(text))
                {
                    this.inUmbralIce = true;

                    Trace.WriteLine("Umbral Ice On. -> " + log);
                    return;
                }
            }

            // アンブラルアイスOFF？
            foreach (var text in machingTextToUmbralIceOff)
            {
                if (log.Contains(text))
                {
                    Task.Run(() =>
                    {
                        Thread.Sleep(GraceToUpdateEnochian + Settings.Default.ParameterRefreshRate);

                        this.inUmbralIce = false;
                        Trace.WriteLine("Umbral Ice Off. -> " + log);
                    });

                    return;
                }
            }
        }

        /// <summary>
        /// エノキアンの効果時間を更新する
        /// </summary>
        private void UpdateEnochian(
            double duration,
            string log)
        {
            var vm = EnochianTimerWindow.Default.ViewModel;

            vm.StartDateTime = DateTime.Now;

            vm.EndScheduledDateTime = vm.StartDateTime.AddSeconds(duration);

            // ACTにログを発生させる
            ActGlobals.oFormActMain.ParseRawLogLine(false, DateTime.Now, "Updated Enochian.");

            Trace.WriteLine("Enochian Update, +" + duration.ToString() + "s. -> " + log);
        }
    }
}
