namespace ACT.MPTimer
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    using ACT.MPTimer.Properties;
    using ACT.MPTimer.Utility;
    using Advanced_Combat_Tracker;

    /// <summary>
    /// Active Combat Tacker v3 MPTimer Plugin
    /// </summary>
    public partial class MPTimerPlugin : IActPluginV1
    {
        /// <summary>
        /// プラグインステータス表示ラベル
        /// </summary>
        private Label PluginStatusLabel
        {
            get;
            set;
        }

        /// <summary>
        /// プラグインを初期化する
        /// </summary>
        /// <param name="pluginScreenSpace"></param>
        /// <param name="pluginStatusText"></param>
        void IActPluginV1.InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            try
            {
                TraceUtility.Initialize();
                Trace.WriteLine("InitPlugin begin.");

                pluginScreenSpace.Text = "MPTimer";

                // 設定Panelを追加する
                pluginScreenSpace.Controls.Add(ConfigPanel.Default);

                // アップデートを確認する
                Task.Run(() =>
                {
                    this.Update();
                });

                // オーバーレイを表示する
                MPTimerWindow.Default.Show();
                EnochianTimerWindow.Default.Show();

                // FF14監視スレッドを開始する
                FF14Watcher.Initialize();

                this.PluginStatusLabel = pluginStatusText;
                this.PluginStatusLabel.Text = "Plugin Started";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "ACT.MPTimer 플러그인 초기화 도중 에러가 발생했습니다." + Environment.NewLine +
                    ex.ToString());
            }
            finally
            {
                Trace.WriteLine("InitPlugin end.");
            }
        }

        /// <summary>
        /// プラグインを後片付けする
        /// </summary>
        void IActPluginV1.DeInitPlugin()
        {
            try
            {
                Trace.WriteLine("DeInitPlugin begin.");

                // Windowの位置を保存する
                Settings.Default.OverlayTop = (int)MPTimerWindow.Default.Top;
                Settings.Default.OverlayLeft = (int)MPTimerWindow.Default.Left;
                Settings.Default.EnochianOverlayTop = (int)EnochianTimerWindow.Default.Top;
                Settings.Default.EnochianOverlayLeft = (int)EnochianTimerWindow.Default.Left;
                Settings.Default.Save();

                FF14Watcher.Deinitialize();
                MPTimerWindow.Default.Close();
                EnochianTimerWindow.Default.Close();

                this.PluginStatusLabel.Text = "Plugin Exited";
            }
            catch (Exception ex)
            {
                Trace.WriteLine(
                    "ACT.MPTimer 플러그인 종료 도중 에러가 발생했습니다." + Environment.NewLine +
                    ex.ToString());
            }
            finally
            {
                Trace.WriteLine("DeInitPlugin end.");
                Trace.Flush();
            }
        }

        /// <summary>
        /// アップデートを行う
        /// </summary>
        private void Update()
        {
            if ((DateTime.Now - Settings.Default.LastUpdateDatetime).TotalHours >= 6d)
            {
                var message = UpdateChecker.Update();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    ActGlobals.oFormActMain.WriteExceptionLog(
                        new Exception(),
                        message);
                }

                Settings.Default.LastUpdateDatetime = DateTime.Now;
                Settings.Default.Save();
            }
        }
    }
}
