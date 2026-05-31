#nullable enable
using System;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using LiveSplit.Model;
using Newtonsoft.Json;
using Run = LiveSplit.TimeAttackPause.DTO.Run;

namespace LiveSplit.TimeAttackPause.IO
{
    public static class SplitStateImporter
    {
        public static void ImportState(string filepath, LiveSplitState state, ITimerModel timerModel)
        {
            // read all the text from the file as string
            string jsonString = File.ReadAllText(filepath);
            Run? runToImport = JsonConvert.DeserializeObject<Run>(jsonString);
            if (runToImport == null)
            {
                return;
            }
            
            try
            {
                timerModel.Start();

                for (var i = 0; i < runToImport.CurrentSplitIndex; i++)
                {
                    timerModel.Split();
                    Application.DoEvents();
                    Thread.Sleep(50);
                }

                var splitIndex = 0;
                foreach (var segment in state.Run)
                {
                    segment.SplitTime = new Time(runToImport.TimingMethod, runToImport.Splits[splitIndex].Time);
                    splitIndex += 1;
                }


                state.AdjustedStartTime = TimeStamp.Now - runToImport.CurrentTime.GetValueOrDefault(TimeSpan.Zero);
                state.IsGameTimeInitialized = true;

                timerModel.Pause();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SplitStateImporter.ImportState encountered an error while applying run data: {ex.Message}");
            }
        }
    }
}