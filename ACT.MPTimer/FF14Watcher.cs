namespace ACT.MPTimer
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    using ACT.MPTimer.Properties;
    using ACT.MPTimer.Utility;
    using Advanced_Combat_Tracker;

    /// <summary>
    /// FF14を監視する
    /// </summary>
    public partial class FF14Watcher
    {
        private static double FFXIVProcessCheckInterval = 10.0d;

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        private static FF14Watcher instance;

        /// <summary>
        /// 監視タスク
        /// </summary>
        private Task watchTask;

        /// <summary>
        /// 処理中か？
        /// </summary>
        private bool isWorking;

        /// <summary>
        /// FFXIVプロセスの所在を最後にチェックした日時
        /// </summary>
        private DateTime lastCheckDateTime;

        /// <summary>
        /// 最後にディスパッチャーを再起動した日時
        /// </summary>
        private DateTime lastRebootDispatcherDateTime = DateTime.MinValue;

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static FF14Watcher Default
        {
            get
            {
                FF14Watcher.Initialize();
                return instance;
            }
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        public static void Initialize()
        {
            if (instance == null)
            {
                instance = new FF14Watcher()
                {
                    PreviousMP = -1
                };

                // 監視タスクを開始する
                instance.isWorking = true;
                instance.watchTask = TaskUtil.StartSTATask(instance.WatchCore);

                // エノキアンタイマーを開始する
                instance.StartEnochianTimer();
            }
        }

        /// <summary>
        /// 後片付けをする
        /// </summary>
        public static void Deinitialize()
        {
            if (instance != null)
            {
                // エノキアンタイマーを終了する
                instance.EndEnochianTimer();

                // 監視タスクを終了する
                if (instance.watchTask != null)
                {
                    instance.isWorking = false;
                    instance.watchTask.Wait();
                    instance.watchTask.Dispose();
                    instance.watchTask = null;
                }

                instance = null;
            }
        }

        /// <summary>
        /// 監視の中核
        /// </summary>
        private void WatchCore()
        {
            while (this.isWorking)
            {
                try
                {
                    // ACTが表示されていなければ何もしない
                    if (!ActGlobals.oFormActMain.Visible)
                    {
                        Thread.Sleep(5 * 1000);
                        continue;
                    }

                    // FF14Processがなければ何もしない
                    if ((DateTime.Now - this.lastCheckDateTime).TotalSeconds >= FFXIVProcessCheckInterval)
                    {
                        if (FF14PluginHelper.GetFFXIVProcess == null)
                        {
#if !DEBUG
                            Thread.Sleep(5 * 1000);
                            continue;
#endif
                        }

                        this.lastCheckDateTime = DateTime.Now;
                    }

                    // MP回復を監視する
                    this.WacthMPRecovery();

                    Thread.Sleep(Settings.Default.ParameterRefreshRate);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(
                        "MP Timer Error!" + Environment.NewLine +
                        ex.ToString());

                    Thread.Sleep(5 * 1000);
                }
            }
        }
    }
}
