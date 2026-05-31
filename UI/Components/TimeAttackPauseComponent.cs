using LiveSplit.Model;
using LiveSplit.TimeAttackPause.IO;
using LiveSplit.TimeAttackPause.UI.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public class TimeAttackPauseComponent : ControlComponent
    {
        private TimeAttackPauseSettings Settings { get; set; }

        private ITimerModel Model { get; set; }

        // This object contains all of the current information about the splits, the timer, etc.
        private LiveSplitState CurrentState { get; set; }

        // Track the last seen split index to detect when the run's split index changes
        private int lastSeenSplitIndex = -1;
        // Cached autosave filename for the active run so multiple autosaves don't overwrite different files
        private string cachedAutoSaveFileName = null;

        // Builds and caches the autosave filename. If runStartTime is provided, it will be used
        // as the timestamp for the filename. Otherwise the method will attempt to discover
        // the run start time via LiveSplitState properties or fall back to estimating from CurrentTime.
        private void EnsureCachedAutoSaveFileName(DateTime? runStartTime = null)
        {
            if (!string.IsNullOrEmpty(cachedAutoSaveFileName))
            {
                return;
            }

            var gameName = GetValueOrDefault(CurrentState?.Run?.GameName, "Unknown Game");
            var categoryName = GetValueOrDefault(CurrentState?.Run?.CategoryName, "Unknown Category");
            var startTime = runStartTime ?? GetRunStartTime();

            var timestamp = startTime.ToString("yyyy-MM-dd-HH.mm.ss");
            var fileName = $"{gameName} - {categoryName} - {timestamp}.json";

            cachedAutoSaveFileName = SanitiseFileName(fileName);
        }

        private static string GetValueOrDefault(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private DateTime GetRunStartTime()
        {
            var timingMethod = CurrentState.CurrentTimingMethod;
            var elapsed = CurrentState.CurrentTime[timingMethod] ?? TimeSpan.Zero;

            return DateTime.Now - elapsed;
        }

        private static string SanitiseFileName(string fileName)
        {
            foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalidCharacter, '_');
            }

            return fileName;
        }

        public override string ComponentName => "TimeAttackPause";
        public override float HorizontalWidth => 0;
        public override float MinimumWidth => 0;
        public override float VerticalHeight => 0;
        public override float MinimumHeight => 0;

        // This function is called when LiveSplit creates your component. This happens when the
        // component is added to the layout, or when LiveSplit opens a layout with this component
        // already added.
        public TimeAttackPauseComponent(LiveSplitState state) : this(state, CreateFormControl())
        {
            ContextMenuControls = new Dictionary<string, Action>();
            ContextMenuControls.Add("Export current run", ExportCurrentRun);
            ContextMenuControls.Add("Import run", ImportRun);
        }

        private static Control CreateFormControl()
        {
            // not used anymore
            return new FlowLayoutPanel
            {
                Size = new Size(0, 0),
                Location = new Point(0, 0),
            };
        }

        private TimeAttackPauseComponent(LiveSplitState state, Control formControl) : base(state, formControl,
            ex => ErrorCallback(state.Form, ex))
        {
            Settings = new TimeAttackPauseSettings();
            Model = new TimerModel() { CurrentState = state };

            CurrentState = state;
        }

        private void ExportCurrentRun()
        {
            if (CurrentState.CurrentPhase == TimerPhase.Running)
            {
                Model.Pause();
            }

            // Displays a SaveFileDialog so the user can save the Run as json file
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            saveFileDialog.Title = "Save Your Run";
            saveFileDialog.ShowDialog();

            if (saveFileDialog.FileName == "") return;

            SplitsStateWriter.SaveSplitsState(CurrentState, saveFileDialog.FileName);
        }

        private void ImportRun()
        {
            // Displays a OpenFileDialog so the user can save the Run as json file
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "json files (*.json)|*.json|All files (*.*)|*.*";
            openFileDialog.Title = "Open Your Run";
            openFileDialog.ShowDialog();

            if (openFileDialog.FileName == "") return;

            if (CurrentState.CurrentPhase != TimerPhase.NotRunning)
            {
                Model.Reset();
            }

            SplitStateImporter.ImportState(openFileDialog.FileName, CurrentState, Model);

            lastSeenSplitIndex = CurrentState.CurrentSplitIndex;
            cachedAutoSaveFileName = null;
        }

        static void ErrorCallback(Form form, Exception ex)
        {
            string requiredBits = Environment.Is64BitProcess ? "64" : "32";
            MessageBox.Show(form, "Error appeared: " + ex.Message, "TimeAttackPause Component Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public override Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        // This is the function where we decide what needs to be displayed at this moment in time,
        // and tell the internal component to display it. This function is called hundreds to
        // thousands of times per second.
        public override void Update(IInvalidator invalidator, LiveSplitState state, float width, float height,
            LayoutMode mode)
        {
            CurrentState = state;

            // Detect split index changes (increase = new split, decrease = reset/undo)
            if (CurrentState.CurrentSplitIndex < lastSeenSplitIndex)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"TimeAttackPause: detected split index decrease from {lastSeenSplitIndex} to {CurrentState.CurrentSplitIndex}, syncing tracker.");
                }
                catch { }
                lastSeenSplitIndex = CurrentState.CurrentSplitIndex;
                // Clear cached filename only when the run is reset to the very start (index == 0)
                if (CurrentState.CurrentSplitIndex == 0)
                {
                    cachedAutoSaveFileName = null;
                }
            }
            else if (CurrentState.CurrentSplitIndex > lastSeenSplitIndex)
            {
                // If the run just started (0 -> 1) capture the system start time and cache the filename
                if (lastSeenSplitIndex == 0 && CurrentState.CurrentSplitIndex == 1)
                {
                    EnsureCachedAutoSaveFileName(DateTime.Now);
                }

                // Auto-save when the split index increases (but not on the initial start at index 0)
                if (CurrentState.CurrentSplitIndex > 0)
                {
                    AutoSaveRun();
                }

                lastSeenSplitIndex = CurrentState.CurrentSplitIndex;
            }
        }

        // Auto-saves the current run state
        private void AutoSaveRun()
        {
            if (ShouldSkipAutoSave())
            {
                return;
            }

            foreach (var directory in GetAutoSaveDirectoryCandidates())
            {
                if (TryAutoSaveToDirectory(directory))
                {
                    return;
                }
            }

            Debug.WriteLine("TimeAttackPause autosave failed: no writable directory found.");
        }

        private bool ShouldSkipAutoSave()
        {
            if (ImportContext.IsImporting)
            {
                Debug.WriteLine("Import in progress, Auto-Saves paused");
                return true;
            }

            if (Settings?.EnableAutosave == false)
            {
                Debug.WriteLine("TimeAttackPause autosave skipped because EnableAutosave is false.");
                return true;
            }

            return false;
        }

        private IEnumerable<string> GetAutoSaveDirectoryCandidates()
        {
            if (!string.IsNullOrWhiteSpace(Settings?.DefaultSavePath))
            {
                yield return Settings.DefaultSavePath;
            }

            yield return Path.Combine(GetApplicationRoot(), "TimeAttackPauseAutosaves");

            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LiveSplit",
                "TimeAttackPauseAutosaves"
            );

            yield return Path.GetTempPath();
        }

        private string GetApplicationRoot()
        {
            try
            {
                return Application.StartupPath;
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private bool TryAutoSaveToDirectory(string directory)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(directory))
                {
                    return false;
                }

                Directory.CreateDirectory(directory);

                EnsureCachedAutoSaveFileName();

                var filePath = Path.Combine(directory, cachedAutoSaveFileName);
                SplitsStateWriter.SaveSplitsState(CurrentState, filePath);

                Debug.WriteLine($"TimeAttackPause autosave saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TimeAttackPause autosave attempt failed for '{directory}': {ex.Message}");
                return false;
            }
        }

        // I do not know what this is for.
        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}