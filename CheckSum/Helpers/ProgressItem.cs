using System;
using System.Globalization;
using CheckSum.Res;

namespace CheckSum.Helpers
{
    public class ProgressItem
    {
        internal ProgressItem(string id)
        {
            Id = id;
        }

        private long _current;

        public string Id { get; private set; }
        public string Message { get; private set; }
        public long? Total { get; private set; }

        public long Current
        {
            get { return _current; }
            set
            {
                if (!IsComplete)
                {
                    if (!StartTime.HasValue) StartTime = DateTime.UtcNow;
                    _current = value;
                    LastUpdateTime = DateTime.UtcNow;
                }
            }
        }

        public decimal Progress { get { return Total.HasValue ? 100.0m*Current/Total.Value : Current; } }
        public DateTime? StartTime { get; private set; }
        public DateTime LastUpdateTime { get; private set; }
        public TimeSpan Duration { get { return LastUpdateTime.Subtract(StartTime.GetValueOrDefault()); } }
        public TimeSpan DisplayThresholdDuration { get; private set; }

        public TimeSpan? RemainingTime
        {
            get
            {
                if (Total.HasValue && Current != 0)
                {
                    double d = LastUpdateTime.Subtract(StartTime.GetValueOrDefault()).Ticks;
                    return TimeSpan.FromTicks((long)((d/Current)*(Total.Value-Current)));
                }
                return null;
            }
        }

        public bool IsComplete { get; private set; }

        object _lock=new object();

        public ProgressItem Complete()
        {
            lock (_lock)
            {
                Total = Current;
                IsComplete = true;
                LastUpdateTime = DateTime.UtcNow;
                if (!StartTime.HasValue) StartTime = LastUpdateTime;
            }

            if (ProgressUpdated != null)
                ProgressUpdated(this);
            
            return this;
        }

        public bool DeleteFlag { get; set; }
        public ProgressItem Delete()
        {
            DeleteFlag = true;
            return Complete();
        }

        public ProgressItem Reset()
        {
            lock (_lock)
            {
                Total = null;
                Current = 0;
                Message = null;
                IsComplete = false;
                LastUpdateTime = default(DateTime);
                LastRenderTime = null;
                StartTime = null;
                DisplayThresholdDuration = TimeSpan.Zero;
            }
            return this;
        }

        public ProgressItem SetDisplayThresholdDuration(TimeSpan duration)
        {
            DisplayThresholdDuration = duration;
            return this;
        }

        public ProgressItem SetMessage(string msg)
        {
            Message = msg;
            return this;
        }

        public ProgressItem SetProgress(long current, long total)
        {
            lock (_lock)
            {
                Total = total;
                Current = current;
                LastUpdateTime = DateTime.UtcNow;
            }
            if (ProgressUpdated != null)
                ProgressUpdated(this);

            return this;
        }

        public ProgressItem SetProgress(long current)
        {
            lock (_lock)
            {
                Current = current;
                if (Total != null && Current >= Total)
                    Total = Current;
                LastUpdateTime = DateTime.UtcNow;
            }

            if (ProgressUpdated != null)
                ProgressUpdated(this);

            return this;
        }

        public ProgressItem IncrementProgress()
        {
            return IncrementProgress(1);
        }

        public ProgressItem IncrementProgress(long increment)
        {
            lock (_lock)
            {
                Current += increment;

                if (Total != null && Current >= Total)
                    Total = Current;

                LastUpdateTime = DateTime.UtcNow;
            }

            if (ProgressUpdated != null)
                ProgressUpdated(this);

            return this;
        }

        public event ProgressUpdatedHandler ProgressUpdated;

        public bool Rendered { get; set; }
        public DateTime? LastRenderTime { get; set; }
        public DateTime? GlobalLastRenderTime { get; set; }
        public int LineIndex { get; set; }

        public string GetProgressMessage()
        {
            string wholeMessage;
            if (DeleteFlag)
            {
                wholeMessage = "";
            }
            else if (IsComplete)
            {
                wholeMessage = string.Format(CultureInfo.InvariantCulture,
                    Resource.GetProgressMessage_CompletedTask,
                    Id,
                    RenderIndex(Current),
                    RenderIndex(Total),
                    Duration.TotalSeconds,
                    Message == null ? "" : ("(" + Message.Replace('\n', ' ') + ")"));
            }
            else if (Total.HasValue && RemainingTime.HasValue)
            {
                wholeMessage = string.Format(CultureInfo.InvariantCulture,
                    Resource.GetProgressMessage_BoundedTask,
                    Id,
                    RenderIndex(Current),
                    RenderIndex(Total),
                    RemainingTime.Value.TotalSeconds,
                    Progress,
                    Message == null ? "" : ("(" + Message.Replace('\n', ' ') + ")")
                    );
            }
            else
            {
                wholeMessage = string.Format(CultureInfo.InvariantCulture,
                    Resource.GetProgressMessage_UnboundedTask,
                    Id,
                    RenderIndex(Current),
                    Duration.TotalSeconds,
                    Message == null ? "" : ("(" + Message + ")"),
                    RenderIndexAsHumanReadableSize ? "" : " "+(Current > 1 ? Resource.GetProgressMessage_Items : Resource.GetProgressMessage_Item)
                    );
            }
            return wholeMessage;
        }

        public bool RenderIndexAsHumanReadableSize { get; private set; }

        public ProgressItem SetRenderIndexAsHumanReadableSize(bool flag=true)
        {
            RenderIndexAsHumanReadableSize = flag;
            return this;
        }

        public string RenderIndex(long? idx)
        {
            if (idx == null)
                return "";
            if (RenderIndexAsHumanReadableSize)
            {
                if (idx < 4096)
                    return string.Format(CultureInfo.InvariantCulture, "{0} b", idx);

                if (idx < 4096*1024)
                    return string.Format(CultureInfo.InvariantCulture, "{0} kb", idx/1024);

                if (idx < 4096L*1024*1024)
                    return string.Format(CultureInfo.InvariantCulture, "{0} Mb", idx/(1024L*1024));

                return string.Format(CultureInfo.InvariantCulture, "{0:0.0} Gb", idx*1.0/(1024L*1024*1024));
            }
            else
            {
                return idx.Value.ToString(CultureInfo.InvariantCulture);
            }
        }
    }

    public delegate void ProgressUpdatedHandler(ProgressItem item);

}