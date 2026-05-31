using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using LiveSplit.Model;
using LiveSplit.TimeAttackPause.IO;
using LiveSplit.TimeAttackPause.UI.Components;

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
            if (!string.IsNullOrEmpty(cachedAutoSaveFileName)) return;

            try
            {
                // Build a descriptive autosave filename: [Game Name] - [Category Name] - [timestamp]
                string gameName = "Unknown Game";
                string categoryName = "Unknown Category";
                try
                {
                    if (CurrentState?.Run != null)
                    {
                        gameName = string.IsNullOrEmpty(CurrentState.Run.GameName) ? gameName : CurrentState.Run.GameName;
                        categoryName = string.IsNullOrEmpty(CurrentState.Run.CategoryName) ? categoryName : CurrentState.Run.CategoryName;
                    }
                }
                catch { }

                DateTime startTime = DateTime.Now;
                if (runStartTime.HasValue)
                {
                    startTime = runStartTime.Value;
                }
                else
                {
                    try
                    {
                        object candidate = null;
                        var state = CurrentState;
                        if (state != null)
                        {
                            var t = state.GetType();
                            var prop = t.GetProperty("AdjustedStartTime");
                            if (prop != null)
                                candidate = prop.GetValue(state);
                            if (candidate == null)
                            {
                                prop = t.GetProperty("AttemptStarted");
                                if (prop != null)
                                    candidate = prop.GetValue(state);
                            }
                            if (candidate == null)
                            {
                                prop = t.GetProperty("AttemptStartedTime");
                                if (prop != null)
                                    candidate = prop.GetValue(state);
                            }
                        }

                        if (candidate != null)
                        {
                            if (candidate is DateTime dt)
                            {
                                startTime = dt;
                            }
                            else if (candidate is DateTimeOffset dto)
                            {
                                startTime = dto.LocalDateTime;
                            }
                            else
                            {
                                var candType = candidate.GetType();
                                var toDate = candType.GetMethod("ToDateTime", Type.EmptyTypes);
                                if (toDate != null)
                                {
                                    var res = toDate.Invoke(candidate, null);
                                    if (res is DateTime dt2) startTime = dt2;
                                }
                                else
                                {
                                    var dateProp = candType.GetProperty("DateTime") ?? candType.GetProperty("LocalDateTime") ?? candType.GetProperty("Value");
                                    if (dateProp != null)
                                    {
                                        var res = dateProp.GetValue(candidate);
                                        if (res is DateTime dt3) startTime = dt3;
                                        else if (res is DateTimeOffset dto2) startTime = dto2.LocalDateTime;
                                    }
                                }
                            }
                        }
                        else
                        {
                            var tm = CurrentState.CurrentTimingMethod;
                            var elapsed = CurrentState.CurrentTime[tm] ?? TimeSpan.Zero;
                            startTime = DateTime.Now - elapsed;
                        }
                    }
                    catch { }
                }

                string timestamp = startTime.ToString("yyyy-MM-dd-HH.mm.ss");
                string fileName = $"{gameName} - {categoryName} - {timestamp}.json";
                foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                {
                    fileName = fileName.Replace(c, '_');
                }

                cachedAutoSaveFileName = fileName;
            }
            catch { }
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

        // Auto-saves the current run state to guard against PC crashes
        private void AutoSaveRun()
        {
            if (ImportContext.IsImporting)
            {
                System.Diagnostics.Debug.WriteLine("Import in progress, Auto-Saves paused");
                return;
            }
            try
            {

                // If settings exists and autosave is disabled, skip autosave
                if (Settings != null && Settings.EnableAutosave == false)
                {
                    System.Diagnostics.Debug.WriteLine("TimeAttackPause autosave skipped because EnableAutosave is false.");
                    return;
                }

                // Build candidate directories in order of preference
                var candidates = new List<string>();
                try
                {
                    var settings = Settings;
                    if (settings != null && !string.IsNullOrEmpty(settings.DefaultSavePath))
                        candidates.Add(settings.DefaultSavePath);
                }
                catch
                {
                    // ignore
                }

                string processRoot = null;
                try
                {
                    processRoot = Application.StartupPath;
                }
                catch
                {
                    processRoot = AppDomain.CurrentDomain.BaseDirectory;
                }
                candidates.Add(System.IO.Path.Combine(processRoot, "TimeAttackPauseAutosaves"));

                candidates.Add(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LiveSplit", "TimeAttackPauseAutosaves"));

                candidates.Add(System.IO.Path.GetTempPath());

                bool saved = false;
                foreach (var dir in candidates)
                {
                    try
                    {
                        var autoSaveDir = dir;
                        if (string.IsNullOrEmpty(autoSaveDir)) continue;
                        System.IO.Directory.CreateDirectory(autoSaveDir);
                        // Build a descriptive autosave filename: [Game Name] - [Category Name] - [Date] - [Time the run Started]
                        string gameName = "Unknown Game";
                        string categoryName = "Unknown Category";
                        try
                        {
                            // LiveSplit's Run typically exposes GameName and CategoryName
                            if (CurrentState?.Run != null)
                            {
                                gameName = string.IsNullOrEmpty(CurrentState.Run.GameName) ? gameName : CurrentState.Run.GameName;
                                categoryName = string.IsNullOrEmpty(CurrentState.Run.CategoryName) ? categoryName : CurrentState.Run.CategoryName;
                            }
                        }
                        catch { }

                        // Prefer the system time the run started (if LiveSplit exposes it),
                        // otherwise estimate from the current timer time.
                        DateTime startTime = DateTime.Now;
                        try
                        {
                            object candidate = null;
                            var state = CurrentState;
                            if (state != null)
                            {
                                var t = state.GetType();
                                // Try common property names that may represent the system start time
                                var prop = t.GetProperty("AdjustedStartTime");
                                if (prop != null)
                                    candidate = prop.GetValue(state);
                                if (candidate == null)
                                {
                                    prop = t.GetProperty("AttemptStarted");
                                    if (prop != null)
                                        candidate = prop.GetValue(state);
                                }
                                if (candidate == null)
                                {
                                    prop = t.GetProperty("AttemptStartedTime");
                                    if (prop != null)
                                        candidate = prop.GetValue(state);
                                }
                            }

                            if (candidate != null)
                            {
                                if (candidate is DateTime dt)
                                {
                                    startTime = dt;
                                }
                                else if (candidate is DateTimeOffset dto)
                                {
                                    startTime = dto.LocalDateTime;
                                }
                                else
                                {
                                    var candType = candidate.GetType();
                                    var toDate = candType.GetMethod("ToDateTime", Type.EmptyTypes);
                                    if (toDate != null)
                                    {
                                        var res = toDate.Invoke(candidate, null);
                                        if (res is DateTime dt2) startTime = dt2;
                                    }
                                    else
                                    {
                                        var dateProp = candType.GetProperty("DateTime") ?? candType.GetProperty("LocalDateTime") ?? candType.GetProperty("Value");
                                        if (dateProp != null)
                                        {
                                            var res = dateProp.GetValue(candidate);
                                            if (res is DateTime dt3) startTime = dt3;
                                            else if (res is DateTimeOffset dto2) startTime = dto2.LocalDateTime;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                // Fallback: estimate the start time from CurrentTime
                                var tm = CurrentState.CurrentTimingMethod;
                                var elapsed = CurrentState.CurrentTime[tm] ?? TimeSpan.Zero;
                                startTime = DateTime.Now - elapsed;
                            }
                        }
                        catch { }

                        string timestamp = startTime.ToString("yyyy-MM-dd HH.mm.ss");
                        string fileName = $"{gameName} - {categoryName} - {timestamp}.json";
                        // Remove or replace invalid filename chars
                        foreach (var c in System.IO.Path.GetInvalidFileNameChars())
                        {
                            fileName = fileName.Replace(c, '_');
                        }

                        // Ensure the cached filename exists; if the run start has been captured earlier
                        // it will be used. Otherwise compute it now (best effort).
                        EnsureCachedAutoSaveFileName();
                        string filePath = System.IO.Path.Combine(autoSaveDir, cachedAutoSaveFileName ?? fileName);
                        SplitsStateWriter.SaveSplitsState(CurrentState, filePath);
                        System.Diagnostics.Debug.WriteLine($"TimeAttackPause autosave saved to: {filePath}");
                        saved = true;
                        break;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"TimeAttackPause autosave attempt failed for '{dir}': {ex.Message}");
                    }
                }

                if (!saved)
                {
                    System.Diagnostics.Debug.WriteLine("TimeAttackPause autosave failed: no writable directory found.");
                }
            }
            catch (Exception ex)
            {
                // Silently fail on autosave to not interrupt the run
                System.Diagnostics.Debug.WriteLine($"TimeAttackPause autosave failed: {ex.Message}");
            }
        }

        // I do not know what this is for.
        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}