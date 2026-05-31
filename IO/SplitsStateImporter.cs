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
    public static class ImportContext
    {
        public static bool IsImporting { get; private set; }

        public static IDisposable BeginImport()
        {
            IsImporting = true;
            return new EndImportScope();
        }

        private sealed class EndImportScope : IDisposable
        {
            public void Dispose()
            {
                IsImporting = false;
            }
        }
    }
    public static class SplitStateImporter
    {
        public static void ImportState(string filepath, LiveSplitState state, ITimerModel timerModel)
        {
            using (ImportContext.BeginImport())
            {
                try
                {
                    // read all the text from the file as string
                    string jsonString = File.ReadAllText(filepath);
                    Run? runToImport = JsonConvert.DeserializeObject<Run>(jsonString);
                    if (runToImport == null)
                    {
                        return;
                    }


                    // 1. Ensure clean state
                    if (state.CurrentPhase != TimerPhase.NotRunning)
                    {
                        timerModel.Reset();
                    }

                    timerModel.Start();

                    // Some autosplitters keep track of properties that we're modifying here, so to maximise compatibility, we should mutate the values in a way similar to 
                    // how they would be mutated during a normal run, rather than just setting them directly.
                    // This is especially important for the split times, as some autosplitters may have logic that triggers on split events.

                    for (var splitIndex = 0; splitIndex < runToImport.CurrentSplitIndex; splitIndex++)
                    {
                        var importCurrentSplitTime = runToImport.Splits[splitIndex].Time;

                        System.Diagnostics.Debug.WriteLine($"SplitIndex {splitIndex}, importCurrentSplitTime {importCurrentSplitTime}");
                        if (importCurrentSplitTime == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Skipping split...");
                            timerModel.SkipSplit();
                        }
                        else
                        {

                            state.AdjustedStartTime = TimeStamp.Now - importCurrentSplitTime.GetValueOrDefault(TimeSpan.Zero);
                            System.Diagnostics.Debug.WriteLine($"Global timer set to {importCurrentSplitTime}");
                            System.Diagnostics.Debug.WriteLine($"Splitting...");
                            timerModel.Split();

                            state.Run[splitIndex].SplitTime = new Time(runToImport.TimingMethod, importCurrentSplitTime);

                            Application.DoEvents();
                            Thread.Sleep(50);
                        }
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
}