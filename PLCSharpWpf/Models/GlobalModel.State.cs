using PLCSharp.VVMs.ModeState;
using System.ComponentModel;
using System.Threading;
using System.Windows.Media;

namespace PLCSharp.Models
{
    /// <summary>
    /// 全局模型 - 模式与状态管理
    /// </summary>
    public partial class GlobalModel
    {
        #region 模式与状态
        private ModeState _ModeState = new();
        /// <summary>
        /// ModeState
        /// </summary>
        public ModeState ModeState
        {
            get { return _ModeState; }
            set { SetProperty(ref _ModeState, value); }
        }

        private readonly BackgroundWorker bkgWorker;

        private void BackgroundWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                try
                {
                    if (ModeState.Sc.Reset)
                    {
                        TitleState.Background = Brushes.Gray;
                    }
                    ModeState.Run();


                }
                catch (Exception ex)
                {
                    SendErr(ex.Message);
                    goto sleep;
                }

            sleep:

                Thread.Sleep(1);
            }
        }
        #endregion
    }
}
